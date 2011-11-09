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
using System.Windows.Forms.DataVisualization.Charting;
using System.Linq;
using System.IO;
using Bio;
using SeqcosApp.Analyzer;
using ShoNS.Array;
using ShoNS.Visualization;
using ShoNS.MathFunc;
using System.ComponentModel;
using System.Reflection;

namespace SeqcosApp.Plot
{
    /// <summary>
    /// Helper class to plot results from SequenceAnalyzer
    /// </summary>
    public class SequencePlotter 
    {
        #region Members

        /// <summary>
        /// Array of colors that will be used for plotting
        /// </summary>
        private char[] seriesColors = new char[] { 'r', 'g', 'b', 'y', 'c', 'm' };

        /// <summary>
        /// An instance of SequenceAnalyzer
        /// </summary>
        public readonly SequenceAnalyzer Analyzer;

        /// <summary>
        /// This holds the standard title text for plots
        /// </summary>
        private readonly string titleBanner;

        /// <summary>
        /// Background worker thread for GUI
        /// </summary>
        private readonly BackgroundWorker worker;

        /// <summary>
        /// Background worker arguments for GUI
        /// </summary>
        private readonly DoWorkEventArgs workerArgs;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for creating plots from SequenceAnalyzer data
        /// </summary>
        /// <param name="analyzer">SequenceAnalyzer object that has been processed</param>
        public SequencePlotter(SequenceAnalyzer analyzer)
        {
            //AssemblyName assemblyName = new AssemblyName(Environment.GetEnvironmentVariable("SHODIR") + @"bin\ShoViz.dll");
            //Assembly assem = Assembly.Load(assemblyName);


            if (analyzer == null)
                throw new ArgumentNullException("analyzer");

            // check if this analyzer has processed the input data yet
            // i.e. did it calculate the stats yet?
            if (! (analyzer.HasRunContentByPosition && analyzer.HasRunContentBySequence) )
                throw new Exception("SequenceAnalyzer object has not processed input data yet.");

            this.Analyzer = analyzer;
            this.titleBanner = "(File = " + Path.GetFileName(this.Analyzer.FileName) + ", n = " + this.Analyzer.Count + ")";
        }

        /// <summary>
        /// Constructor that is called by the GUI. Provides BackgroundWorker and DoWorkEventArgs parameters that
        /// are used to halt an operation if a Cancel event is fired.
        /// </summary>
        /// <param name="analyzer">SequenceAnalyzer object that has been processed</param>
        /// <param name="worker">Background working thread (used by GUI)</param>
        /// <param name="e">Worker event arguments</param>
        public SequencePlotter(SequenceAnalyzer analyzer, BackgroundWorker worker, DoWorkEventArgs e)
            : this(analyzer)
        {
            this.worker = worker;
            this.workerArgs = e;
        }

        #endregion

        #region Public Plotting Methods

        /// <summary>
        /// Generate a plot of symbol count by position using Sho libraries. 
        /// Also include GC count, if applicable.
        /// </summary>
        /// <param name="filename">Filename of the output image file</param>
        /// <param name="height">Height of the plot in pixels</param>
        /// <param name="width">Width of the plot in pixels</param>
        public void PlotSymbolCountByPosition(string filename, int height = ShoHelper.HeightDefault, int width = ShoHelper.WidthDefault)
        {
            if (this.Analyzer.SymbolCountByPositionTable == null)
                throw new ArgumentNullException("SymbolCountByPositionTable");

            ShoChart f = ShoHelper.CreateBasicShoChart(
                                            true,
                                            height,
                                            width,
                                            "Percentage (%)",
                                            "Read position (bp)",
                                            "Base content by position " + titleBanner
                                            );

            // Set additional ShoChart parameters
            f.SetYRange(0, 100);
            f.SetXRange(0, this.Analyzer.ReadLengthMax + 1);

            var xLabels = ShoHelper.CreateBins(1, (int)this.Analyzer.ReadLengthMax);
            f.SetXLabels(xLabels, null);

            // Calculate the total number of reads by position
            int[] totalCountByPosition = this.Analyzer.GetSumByPosition();
            
            // Plot symbol content for each base (i.e. A, C, G, T)
            int seriesCount = 0;
            foreach (KeyValuePair<byte, int[]> pair in this.Analyzer.SymbolCountByPositionTable)
            {
                // convert counts to percentages
                double[] percentages = new double[this.Analyzer.ReadLengthMax];
                for (int i = 0; i < this.Analyzer.ReadLengthMax; i++)
                {
                    int countAtCurrentPosition = pair.Value.Sum();
                    percentages[i] = pair.Value[i] * 100 / totalCountByPosition[i];
                }

                // plot the series
                f.Plot(xLabels, percentages, "-" + seriesColors[seriesCount]);

                f.DundasChart.Series[seriesCount].ChartType = SeriesChartType.StackedArea100;

                // set the series name (for legend display)
                f.SeriesNames[seriesCount] = System.Text.Encoding.UTF8.GetString(new byte[] { pair.Key });
                
                seriesCount++;
            }

            //f.DundasChart.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.StackedColumn100;
            //f.DundasChart.AlignDataPointsByAxisLabel();

            // If applicable, plot GC content by position as well
            if (this.Analyzer.Alphabet == Alphabets.DNA 
                    || this.Analyzer.Alphabet == Alphabets.AmbiguousDNA
                    || this.Analyzer.Alphabet == Alphabets.RNA
                    || this.Analyzer.Alphabet == Alphabets.AmbiguousRNA)
            {
                var gcContentByPosition = this.Analyzer.GCContentByPositionArray;

                f.Plot(xLabels, gcContentByPosition, ":ok");
                f.SeriesNames[seriesCount] = "GC%";
                f.DundasChart.Series[seriesCount].ChartType = SeriesChartType.Spline;
            }

            // save plot to image file
            f.SaveImage(filename);
        }

        /// <summary>
        /// Generate a histogram of GC content over all sequences.
        /// </summary>
        /// <param name="filename">Filename of the output image file</param>
        /// <param name="height">Height of the plot in pixels</param>
        /// <param name="width">Width of the plot in pixels</param>
        public void PlotGCContentBySequence(string filename, int height = ShoHelper.HeightDefault, int width = ShoHelper.WidthDefault)
        {
            if (this.Analyzer.GCContentBySequenceArray == null)
                throw new ArgumentNullException("this.analyzer.SymbolCountByPositionTable");

            int binSize = 5;
            var bins = ShoHelper.CreateBins(0, 100, binSize);

            var hist = new Histogram(this.Analyzer.GCContentBySequenceArray, bins);

            ShoChart f = ShoHelper.CreateBasicShoChart(
                                            false,
                                            height,
                                            width,
                                            "Number of reads",
                                            "GC content (%; bin size = " + binSize + ")",
                                            "GC content by sequence " + titleBanner
                                            );
            var xLabelPositions = ShoHelper.CreateBins(1, 21);
            f.SetXLabels(xLabelPositions, bins);

            f.Bar(bins, hist.Count);

            //f.DundasChart.Series[0].ChartType = SeriesChartType.Spline;
            f.SaveImage(filename);                                            
        }

        /// <summary>
        /// Plot the distribution of sequence lengths
        /// </summary>
        /// <param name="filename">Filename of the output image file</param>
        /// <param name="height">Height of the plot in pixels</param>
        /// <param name="width">Width of the plot in pixels</param>
        public void PlotSequenceLengthDistribution(string filename, int height = ShoHelper.HeightDefault, int width = ShoHelper.WidthDefault)
        {
      
            var hist = new Histogram(this.Analyzer.ReadLengths);

            ShoChart f = ShoHelper.CreateBasicShoChart(
                                            false,
                                            height,
                                            width,
                                            "Number of reads",
                                            "Length (bp)",
                                            "Sequence length distribution " + titleBanner
                                            );
            f.HorizontalMinorGridlinesVisible = true;
            //f.SetYLabels(ShoHelper.CreateBins(0, (int)this.Analyzer.Count), null);
            f.Bar(hist.BinCenter, hist.Count);
            f.SaveImage(filename);
        }

        #endregion
    }
}