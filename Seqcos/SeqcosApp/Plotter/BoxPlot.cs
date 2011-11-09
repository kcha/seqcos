// *********************************************************************
// 
//     Copyright (c) Microsoft, 2011. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *********************************************************************
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using ShoNS.Array;
using ShoNS.Visualization;

namespace SeqcosApp.Plot
{
    /// <summary>
    /// Create boxplots using Sho libraries. 
    /// This code is a port of the Python code in Sho.
    /// </summary>
    public class BoxPlot
    {
        #region Members

        /// <summary>
        /// The chart object where the boxplot will be populated in
        /// </summary>
        private ShoChart chart;

        /// <summary>
        /// The maximum read length
        /// </summary>
        private readonly int readLength;

        /// <summary>
        /// THe total number of reads
        /// </summary>
        private readonly long count;

        /// <summary>
        /// Background worker thread for GUI
        /// </summary>
        private readonly BackgroundWorker worker;

        /// <summary>
        /// Background worker arguments for GUI
        /// </summary>
        private readonly DoWorkEventArgs workerArgs;

        /// <summary>
        /// Cap the maximum number of reads to be processed when building boxplots
        /// </summary>
        public const int MaxNumberOfProcessedReads = 2000000;

        /// <summary>
        /// Total number reads that is processed by boxplot.
        /// </summary>
        public readonly int NumberOfProcessedReads;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a BoxPlot object that sets all the required parameters
        /// </summary>
        /// <param name="chart">ShoChart to be used for plotting</param>
        /// <param name="length">Maximum length of the read</param>
        /// <param name="c">Number of reads</param>
        /// <param name="worker">Background working thread (used by GUI)</param>
        /// <param name="e">Worker event arguments</param>
        public BoxPlot(ShoChart chart, int length, long c, Stream s = null)
        {
            if (chart == null)
                throw new ArgumentNullException("chart");

            this.chart = chart;
            this.readLength = length;
            this.count = c;
            //this.NumberOfProcessedReads = Math.Min(MaxNumberOfProcessedReads, (int)this.count);
            this.NumberOfProcessedReads = (int)this.count;
        }

        public BoxPlot(ShoChart chart,int length, long c, BackgroundWorker worker, DoWorkEventArgs e)
            : this(chart, length, c)
        {
            this.worker = worker;
            this.workerArgs = e;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculate boxplot values and set them in a ShoChart object.
        /// </summary>
        /// <param name="xLabelPositions">X-axis labels</param>
        /// <returns>ShoChart with boxplot parameters set</returns>
        public ShoChart BuildBoxPlot(List<int> xLabels, Stream myStream)
        {
            // Boxplot parameters
            // 0 - lower whisker - could be various things, but here we'll use 1.5 IQR of lower quartile
            // 1 - upper whisker - 1.5 IQR of upper quartile
            // 2 - lower box - lower quartile
            // 3 - upper box - upper quartile
            // 4 - average and mean
            // 5 - median
            // >= 6 - other unusual points (e.g. outlier)s
            DoubleArray whiskerLows = new DoubleArray(this.readLength);
            DoubleArray whiskerHighs = new DoubleArray(this.readLength);
            DoubleArray boxLows = new DoubleArray(this.readLength);
            DoubleArray boxHighs = new DoubleArray(this.readLength);
            DoubleArray meanList = new DoubleArray(this.readLength);
            DoubleArray medianList = new DoubleArray(this.readLength);
            DoubleArray[] outliers = new DoubleArray[this.readLength];

            // Open Stream
            //using (Stream myStream = new FileStream(this.streamFileName, FileMode.Open))
            using (myStream)
            {
                if (myStream.CanRead)
                {
                    if (myStream.Length != this.count * this.readLength)
                    {
                        throw new ArgumentOutOfRangeException("Stream size does not match expected length.");
                    }

                    // iterate through each position of the read
                    for (int i = 0; i < this.readLength; i++)
                    //Parallel.For(0, this.readLength, i =>
                    {
                        if (worker != null && workerArgs != null && worker.CancellationPending)
                        {
                            workerArgs.Cancel = true;
                            break;
                        }

                        byte[] qualityBytes = new byte[NumberOfProcessedReads];
                        int bytesRead;
                        double whiskerLow, whiskerHi, boxLow, boxHi, meanVal, medianVal;
                        List<double> outlierLows, outlierHighs;

                        lock (myStream)
                        {
                            myStream.Position = i * this.count;

                            // Read from the filestream
                            bytesRead = 0;
                            bytesRead += myStream.Read(qualityBytes, bytesRead, qualityBytes.Length);
                        }

                        // Convert the byte array to a DoubleArray.
                        // Also, if the byte array contains zeroes, they need to be removed.
                        // These represent unallocated space by reads that do not have 
                        // any bases at this position (i.e. there are variable read lengths
                        // in the dataset).
                        DoubleArray values = validatedConvertToDoubleArray(qualityBytes);

                        // calculate boxplot parameters
                        GetBoxPlotStats(values, out whiskerLow, out whiskerHi, out boxLow, out boxHi, out meanVal, out medianVal, out outlierLows, out outlierHighs);

                        // append values
                        whiskerLows[i] = whiskerLow;
                        whiskerHighs[i] = whiskerHi;
                        boxLows[i] = boxLow;
                        boxHighs[i] = boxHi;
                        meanList[i] = meanVal;
                        medianList[i] = medianVal;

                        // combine low and high outliers and append
                        outlierLows.AddRange(outlierHighs);
                        outliers[i] = DoubleArray.From(outlierLows);
                    }
                    //);
                }
                else
                {
                    throw new NotSupportedException("Unable to read FileStream");
                }
            }

            this.chart.Bar(xLabels, whiskerLows);
            this.chart.DundasChart.Series[0].ChartType = SeriesChartType.BoxPlot;
            SetYRowValues(this.chart.DundasChart.Series[0], whiskerLows, 0);
            SetYRowValues(this.chart.DundasChart.Series[0], whiskerHighs, 1);
            SetYRowValues(this.chart.DundasChart.Series[0], boxLows, 2);
            SetYRowValues(this.chart.DundasChart.Series[0], boxHighs, 3);
            SetYRowValues(this.chart.DundasChart.Series[0], meanList, 4);
            SetYRowValues(this.chart.DundasChart.Series[0], medianList, 5);
            SetYOtherValues(this.chart.DundasChart.Series[0], outliers, 6);

            this.chart.DundasChart.Series[0]["BoxPlotShowUnusualValues"] = "true";
            this.chart.DundasChart.Series[0]["BoxPlotShowAverage"] = "false";

            //ShoNS.Visualization.EllipseAnnotation ellipse = new ShoNS.Visualization.EllipseAnnotation(2, 2, 1, 1);

            return this.chart;
        }


        /// <summary>
        /// Generate parameters to be use for creating a boxplot. This code is mostly ported from
        /// the boxplot() function in Sho (http://research.microsoft.com/en-us/projects/sho/).
        /// </summary>
        /// <param name="values">List of numerical values to be computed</param>
        /// <param name="whiskerLow">Sample minimum</param>
        /// <param name="whiskerHigh">Sample maximum</param>
        /// <param name="boxLow">Lower quartile</param>
        /// <param name="boxHigh">Upper quartile</param>
        /// <param name="meanVal">Mean</param>
        /// <param name="medianVal">Median</param>
        /// <param name="outlierLows">Outliers below sample minimum</param>
        /// <param name="outlierHighs">Outliers above sample minimum</param>
        public void GetBoxPlotStats(DoubleArray values,
            out double whiskerLow, out double whiskerHigh, out double boxLow, out double boxHigh,
            out double meanVal, out double medianVal, out List<double> outlierLows, out List<double> outlierHighs)
        {
            // determine boundaries, median, and mean for the box
            double[] quantileValues = GetQuantiles(values, new double[] { 0.25, 0.5, 0.75 });
            boxLow = quantileValues[0];
            boxHigh = quantileValues[2];
            //medianVal = values.Median(); /// replace with custom code
            medianVal = quantileValues[1];
            meanVal = values.Mean();

            double interQuartileRange = boxHigh - boxLow;

            // According to http://en.wikipedia.org/wiki/Box_plot:
            // Whiskers have alternative forms. Here we are conforming to 1.5 * IQR
            whiskerLow = boxLow - 1.5 * interQuartileRange;
            whiskerHigh = boxHigh + 1.5 * interQuartileRange;

            // refine whisker low and high values to match actual observations in values
            List<double> inlierLows = new List<double>();
            List<double> inlierHighs = new List<double>();
            foreach (var x in values)
            {
                if (x >= whiskerLow)
                {
                    inlierLows.Add(x);

                }
                
                if (x <= whiskerHigh)
                {
                    inlierHighs.Add(x);
                }
            }
            if (inlierLows.Count > 0)
                whiskerLow = inlierLows.Min();
            if (inlierHighs.Count > 0)
                whiskerHigh = inlierHighs.Max();


            // determine outliers
            outlierLows = new List<double>();
            outlierHighs = new List<double>();
            foreach (var x in values)
            {
                if (x < whiskerLow)
                {
                    outlierLows.Add(x);
                }
                if (x > whiskerHigh)
                {
                    outlierHighs.Add(x);
                }
            }
            
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculate values at the given quantiles.
        /// </summary>
        /// <param name="values">List of values</param>
        /// <param name="quantiles">Array of quantiles to consider as a percentage</param>
        /// <returns>A list of values at the given quantiles</returns>
        private double[] GetQuantiles(DoubleArray values, double[] quantiles)
        {
            if (quantiles.Length == 0)
                throw new ArgumentNullException("quantiles");

            // values is already sorted using a more optimized approach
            //values = values.Sort();

            double[] quantileVals = new double[quantiles.Length];
            
            // Go through each quantile and calculate the corresponding value
            //for (int i = 0; i < quantiles.Length; i++)
            Parallel.ForEach(quantiles, (quantile, state, i) =>
            {
                quantileVals[i] = QuantileAt(values, quantiles[i]);
            }
            );

            return quantileVals;
        }

        /// <summary>
        /// Determine the value at given percentile
        /// </summary>
        /// <param name="values">List of values</param>
        /// <param name="percentile">Quantile level as a percentage</param>
        /// <returns>Value at given percentile</returns>
        private double QuantileAt(DoubleArray values, double percentile)
        {
            if (percentile < 0 || percentile > 1)
                throw new ArgumentNullException("Percentile must be between 0 and 1");

            // Get the index position at this percentile
            double position = (values.Length - 1) * percentile;
                                 
            // Store the remainder
            double frac = position - Math.Floor(position);

            int index = (int)Math.Floor(position);
            var value1 = values[index];

            if (index < values.Length - 1)
            {
                var value2 = values[index + 1];
                return value1 + frac * (value2 - value1);
            }
            else
            {
                return value1;
            }
        }

        /// <summary>
        /// Set y values of a particular type (whisker, median, etc.) for all series' in the plot (i.e. every position).
        /// NB: Used for setting key points in a boxplot. For setting outliers, see SetYOtherValues().
        /// See MSDN Box Plot Chart controls: http://msdn.microsoft.com/en-us/library/dd456709.aspx
        /// </summary>
        /// <param name="series">Series object storing data points</param>
        /// <param name="rowValues">Horizontal data points (i.e. across all base positiosn of the sequence)</param>
        /// <param name="yIndex">Index indicating type of value (Lower whisker, upper whisker, etc.)</param>
        private void SetYRowValues(Series series, DoubleArray rowValues, int yIndex)
        {
            if (rowValues != null)
            {
                for (int i = 0; i < Math.Min(rowValues.Length, series.Points.Count); i++)
                {
                    series.Points[i].YValues[yIndex] = rowValues[i];
                }
            }
            else
            {
                for (int i = 0; i < series.Points.Count; i++)
                {
                    series.Points[i].YValues[yIndex] = Double.NaN;
                }
            }
        }

        /// <summary>
        /// Set other outstanding values of a Series object, such as outliers.
        /// </summary>
        /// <param name="series">Series object storing data points</param>
        /// <param name="outliers">DoubleArray[] of all outlier values to be plotted</param>
        /// <param name="yIndex">Index of where outlier should be stored in the series (>=6)</param>
        private void SetYOtherValues(Series series, DoubleArray[] outliers, int yIndex)
        {
            if (yIndex < 6)
                throw new ArgumentException("yIndex must be >=6 for outliers");

            if (outliers != null)
            {
                for (int i = 0; i < Math.Min(outliers.Length, series.Points.Count); i++)
                {
                    if (outliers[i].Length > 0)
                    {
                        // Append outlier values to existing list of points in the series
                        var oldYValues = new List<double>(series.Points[i].YValues);

                        var outlierValues = new List<double>(outliers[i]);

                        oldYValues.AddRange(outlierValues);

                        var newYValues = DoubleArray.From(oldYValues);

                        series.Points[i].YValues = newYValues.ToFlatSystemArray();
                    }
                }
            }
        }

        /// <summary>
        /// Convert byte array to a DoubleArray. Also remove any zeroes
        /// from the array.
        /// </summary>
        /// <param name="qualityBytes">Byte array read from FileStream</param>
        /// <returns></returns>
        private DoubleArray validatedConvertToDoubleArrayOld(byte[] qualityBytes)
        {

            List<double> doubleList = new List<double>();

            foreach (var qual in qualityBytes)
            {
                if (qual != 0)
                    doubleList.Add(Convert.ToDouble(qual));
            }

            return DoubleArray.From(doubleList);
        }

        /// <summary>
        /// Convert a byte array to a sorted Double Array. Also remove any zeroes
        /// from the array because they are uninformative.
        /// </summary>
        /// <param name="qualityBytes"></param>
        /// <returns></returns>
        private DoubleArray validatedConvertToDoubleArray(byte[] qualityBytes)
        {
            // Using a SortedList will created an already sorted list. 
            SortedList<double, int> doubleList = new SortedList<double, int>();

            foreach (var qual in qualityBytes)
            {
                if (qual != 0)
                {
                    double val = Convert.ToDouble(qual);
                    int count;
                    //if (doubleList.ContainsKey(val))
                    if (doubleList.TryGetValue(val, out count))
                    {
                        doubleList[val] = count + 1;
                    }
                    else
                    {
                        doubleList.Add(val, 1);
                    }
                }
            }
            List<double> doubleArray = new List<double>();
            foreach (double item in doubleList.Keys)
            {
                for (int i = 0; i < doubleList[item]; i++)
                {
                    doubleArray.Add(item);
                }
            }

            return DoubleArray.From(doubleArray);
        }

        #endregion
    }
}
