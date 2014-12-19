// *********************************************************************
// 
//     Copyright (c) 2011 Microsoft. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using Bio;
using ShoNS.Array;
using ShoNS.Visualization;
using ShoNS.MathFunc;
using System.ComponentModel;
using SeqcosApp.Analyzer;
using SeqcosApp.Plot;

namespace SeqcosApp.Plot
{
    /// <summary>
    /// Helper class to plot results from QualityScoreAnalyzer
    /// </summary>
    public class QualityScorePlotter
    {
        #region Members

        /// <summary>
        /// An instance of QualityScoreAnalyzer
        /// </summary>
        public readonly QualityScoreAnalyzer Analyzer;

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
        /// Consructor for creating plots from QualityScoreAnalyzer data
        /// </summary>
        /// <param name="analyzer">QualityScoreAnalyzer object that has been processed</param>
        public QualityScorePlotter(QualityScoreAnalyzer analyzer)
        {
            if (analyzer == null)
                throw new ArgumentNullException("analyzer");

            if (!analyzer.HasRunContentByPosition)
                throw new ArgumentException("QualityScoreAnalyzer object has not processed input data yet.");

            this.Analyzer = analyzer;
            this.titleBanner = MakeTitleBanner(Path.GetFileName(this.Analyzer.FileName), this.Analyzer.Count);
        }

        /// <summary>
        /// Constructor that is called by the GUI. Provides BackgroundWorker and DoWorkEventArgs parameters that
        /// are used to halt an operation if a Cancel event is fired.
        /// </summary>
        /// <param name="analyzer">QualityScoreAnalyzer object that has been processed</param>
        /// <param name="worker">Background working thread (used by GUI)</param>
        /// <param name="e">Worker event arguments</param>
        public QualityScorePlotter(QualityScoreAnalyzer analyzer, BackgroundWorker worker, DoWorkEventArgs e)
            : this(analyzer)
        {
            this.worker = worker;
            this.workerArgs = e;
        }

        #endregion

        #region Public Plotting Methods

        /// <summary>
        /// Generate a plot of quality score by position
        /// </summary>
        /// <param name="filename">Filename of the output image file</param>
        /// <param name="height">Height of the plot in pixels</param>
        /// <param name="width">Width of the plot in pixels</param>
        public void PlotQualityScoreCountByPosition(string filename, int height = ShoHelper.HeightDefault, int width = ShoHelper.WidthDefault)
        {
            if (!this.Analyzer.HasRunContentByPosition)
                throw new ArgumentException("Unable to plot. Need to process quality score data first.");

            ShoChart f = ShoHelper.CreateBasicShoChart(
                                    false,
                                    height,
                                    width,
                                    "Phred quality score (" + this.Analyzer.FormatType.ToString() + ")",
                                    "Read position (bp)",
                                    ""
                                    );
            // Set x- and y-axis ranges
            //f.SetXRange(1, this.Analyzer.ReadLengthMax);
            //f.SetYRange(Convert.ToDouble(QualitativeSequence.GetMinQualScore(this.Analyzer.FormatType)),
              //          Convert.ToDouble(QualitativeSequence.GetMaxQualScore(this.Analyzer.FormatType)));

            var xLabels = ShoHelper.CreateBins(1, (int)this.Analyzer.ReadLengthMax);
            f.SetXLabels(xLabels, null);          

            // Create a BoxPlot by reading data previously saved to file via FileStream.
            BoxPlot bp = new BoxPlot(f, (int)this.Analyzer.ReadLengthMax, this.Analyzer.Count, this.worker, this.workerArgs);

            // Update chart title
            f.Title = string.Format("Base quality score by position {0}", MakeTitleBanner(Path.GetFileName(this.Analyzer.FileName), bp.NumberOfProcessedReads));

            // Build boxplot
            bp.BuildBoxPlot(xLabels, this.Analyzer.MemStream);

            f.SaveImage(filename);
        }

        /// <summary>
        /// Plot mean quality scores at the sequence-level
        /// </summary>
        /// <param name="filename">Filename of the output image file</param>
        /// <param name="height">Height of the plot in pixels</param>
        /// <param name="width">Width of the plot in pixels</param>
        public void PlotQualityScoreBySequence(string filename, int height = ShoHelper.HeightDefault, int width = ShoHelper.WidthDefault)
        {
            if (!this.Analyzer.HasRunContentBySequence)
                throw new ArgumentException("Unable to plot. Need to process quality score data first.");

            var bins = ShoHelper.CreateBins((int)this.Analyzer.BaseQualityScoreMin, (int)this.Analyzer.BaseQualityScoreMax, 2);

            var hist = new Histogram(this.Analyzer.QualityScoreBySequenceMeans, bins);

            ShoChart f = ShoHelper.CreateBasicShoChart(
                                    false,
                                    height,
                                    width,
                                    "Number of reads",
                                    "Mean quality score (" + this.Analyzer.FormatType.ToString() + ")",
                                    "Mean quality score by sequence " + titleBanner
                                    );

            // set y-axis labels
            //f.SetYLabels(ShoHelper.GetIntegerAxisLabels(0, this.Analyzer.Count), null);
           
            f.Bar(bins, hist.Count);
            f.SaveImage(filename);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private string MakeTitleBanner(string name, long c)
        {
            return string.Format("(File = {0}, n = {1})", name, c.ToString());
        }

        #endregion
    }
}
