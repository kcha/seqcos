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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Bio;
using Bio.IO;
using Bio.IO.FastQ;
using Bio.IO.FastA;
using SeqcosApp.Analyzer;
using SeqcosApp.Plot;
using SeqcosApp.Properties;
using System.ComponentModel;
using System.Reflection;

namespace SeqcosApp
{
    /// <summary>
    /// SeqcosApp is the main class of the quality control (QC) application. The console and GUI apps will invoke
    /// this class to execute the QC procedure.
    /// </summary>
    public class Seqcos
    {
        #region Member Variables

        /// <summary>
        /// Store list of output filenames (used for saving image plots).
        /// </summary>
        private Filenames myFilenames;

        /// <summary>
        /// Keep track of the initial working directory, in case it changes
        /// during the progress of the application
        /// </summary>
        private string InitialWorkingDirectory;

        /// <summary>
        /// Store the name of the output directory
        /// </summary>
        public string OutputDirectory { get; private set; }

        /// <summary>
        /// Returns the aboslute path of the output directory
        /// </summary>
        public string FullOutputDirectory
        {
            get
            {
                return InitialWorkingDirectory + "\\" + OutputDirectory;
            }
        }

        /// <summary>
        /// Holds the Parser object for reading in each sequence record from the input file.
        /// </summary>
        public ISequenceParser SelectedParser { get; private set; }

        /// <summary>
        /// Handles sequence-level QC analysis.
        /// </summary>
        public SequenceAnalyzer SequenceQc { get; private set; }

        /// <summary>
        /// Handles quality score-level QC analysis.
        /// </summary>
        public QualityScoreAnalyzer QualityScoreQc { get; private set; }

        /// <summary>
        /// Handles the BLAST component of the application.
        /// </summary>
        public SequenceContaminationFinder ContaminationFinder { get; private set; }

        /// <summary>
        /// Determines whether sequence level plots were generated for this instance class.
        /// This helps us decide whether we should include the corresponding hyperlinks in the 
        /// CSV file.
        /// </summary>
        public bool HasPlottedSequenceStats { get; private set; }

        /// <summary>
        /// Determines whether quality leve plots were generated for this instance class.
        /// </summary>
        public bool HasPlottedQualityScoreStats { get; private set; }

        /// <summary>
        /// Return a list of filenames of the plots that get generated.
        /// </summary>
        public Filenames FileList
        {
            get
            {
                return this.myFilenames;
            }
        }

        /// <summary>
        /// Gets the total number of reads in the input file, if known.
        /// </summary>
        public long Count
        {
            get
            {
                if (SequenceQc != null)
                {
                    return SequenceQc.Count;
                }
                else if (QualityScoreQc != null)
                {
                    return QualityScoreQc.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets the maximum read length in the input file, if known.
        /// </summary>
        public long ReadLengthMax
        {
            get
            {
                if (SequenceQc != null)
                {
                    return SequenceQc.ReadLengthMax;
                }
                else if (QualityScoreQc != null)
                {
                    return QualityScoreQc.ReadLengthMax;
                }
                return 0;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Controls execution of QC steps
        /// </summary>
        /// <param name="parser">ISequenceParser object holding the input sequence data</param>
        /// <param name="filename">Input filename</param>
        /// <param name="runSequenceQc">Indicates whether the sequence-level QC module should be initialized</param>
        /// <param name="runQualityScoreQc">Indicates whether the quality score-level QC module should be initialized</param>
        /// <param name="runBlast">Indicates whether the sequence contamination finder module should be initialized</param>
        /// <param name="format">FastQ Format Type, if applicable. Otherwise use 'null'.</param>
        /// <param name="dir">Output directory</param>
        public Seqcos(ISequenceParser parser, string filename, bool runSequenceQc, bool runQualityScoreQc, bool runBlast, string format, string dir = null)
        {
            if (parser == null)
                throw new ArgumentNullException("parser");

            if (filename == null)
                throw new ArgumentNullException("filename");

            // (deprecated) Register AssemblyResolve event handler - for dealing with Sho libaries that are located
            // externally from this application's install folder
            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolveEventHandler);

            this.myFilenames = new Filenames(filename, Resource.ChartFormat);
            this.SelectedParser = parser;

            this.OutputDirectory = (dir == null) ? myFilenames.Prefix : dir;
            this.InitialWorkingDirectory = Path.GetDirectoryName(filename);
            Directory.SetCurrentDirectory(this.InitialWorkingDirectory);

            if (!Directory.Exists(this.OutputDirectory))
                Directory.CreateDirectory(this.OutputDirectory);
                
            // Initialize SequenceAnalyzer
            this.SequenceQc = runSequenceQc ? new SequenceAnalyzer(this.SelectedParser, myFilenames.FileName) : null;
   
            // Initialize QualityScoreAnalyzer
            if (runQualityScoreQc && !(parser is FastAParser))
            {
                if (format == null)
                    throw new ArgumentNullException("format");

                FastQFormatType myFormat = BioHelper.GetQualityFormatType(format);

                if (runSequenceQc && this.SequenceQc != null)
                {
                    this.QualityScoreQc = new QualityScoreAnalyzer(this.SelectedParser, this.SequenceQc.ReadLengthMax, this.SequenceQc.Count, myFormat, myFilenames.FileName);
                }
                else
                {
                    this.QualityScoreQc = new QualityScoreAnalyzer(this.SelectedParser, myFormat, myFilenames.FileName);
                }
            }
            else
            {
                this.QualityScoreQc = null;
            }

            // Initialize ContaminationFinder
            this.ContaminationFinder = runBlast ? new SequenceContaminationFinder(this.SelectedParser) : null;

            this.HasPlottedSequenceStats = false;
            this.HasPlottedQualityScoreStats = false;
        }

        public Seqcos(ISequenceParser parser, string filename, bool runSequenceQc, bool runQualityScoreQc, bool runBlast, string format, BackgroundWorker worker, DoWorkEventArgs e, string dir = null)
            : this(parser, filename, runSequenceQc, runQualityScoreQc, runBlast, format, dir)
        {
            if (this.SequenceQc != null)
            {
                this.SequenceQc.Worker = worker;
                this.SequenceQc.WorkerArgs = e;
            }

            if (this.QualityScoreQc != null)
            {
                this.QualityScoreQc.Worker = worker;
                this.QualityScoreQc.WorkerArgs = e;
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Plot the statistics at the sequence-level
        /// </summary>
        public void PlotSequenceLevelStats()
        {
            string tempDir = this.FullOutputDirectory + @"\";
            SequencePlotter sp = new SequencePlotter(SequenceQc);
            sp.PlotSymbolCountByPosition(tempDir + myFilenames.SymbolCountByPosition,
                    width: ShoHelper.GetAutoPlotWidth(this.SequenceQc.ReadLengthMax));
            sp.PlotGCContentBySequence(tempDir + myFilenames.GCContentBySequence);
            sp.PlotSequenceLengthDistribution(tempDir + myFilenames.SequenceLengths);

            HasPlottedSequenceStats = true;
        }

        /// <summary>
        /// Plot the statistics at the quality score-level
        /// </summary>
        /// <param name="worker">Background working thread (used by GUI)</param>
        /// <param name="e">Worker event arguments</param>
        public void PlotQualityScoreLevelStats()
        {
            string tempDir = this.FullOutputDirectory + "/";
            QualityScorePlotter qp = new QualityScorePlotter(QualityScoreQc, QualityScoreQc.Worker, QualityScoreQc.WorkerArgs);
            qp.PlotQualityScoreCountByPosition(tempDir + myFilenames.QualityScoreByPosition,
                    width: ShoHelper.GetAutoPlotWidth(QualityScoreQc.ReadLengthMax));
            qp.PlotQualityScoreBySequence(tempDir + myFilenames.QualityScoreBySequence);

            HasPlottedQualityScoreStats = true;
        }

        /// <summary>
        /// After Qc analysis is complete, this method writes all of the statistics to a text file
        /// </summary>
        /// <param name="excelFormat">True if Excel-formatted hyperlinks are desired. False, othrewise.</param>
        public void WriteInputStatistics(bool excelFormat)
        {
            string csvFilename = FullOutputDirectory + "/" + myFilenames.CSV;

            using (StreamWriter csvOut = new StreamWriter(csvFilename))
            {
                InputStatistics stats = new InputStatistics(this);

                if (SequenceQc != null && SequenceQc.IsReady)
                {
                    csvOut.WriteLine(stats.WriteSequenceLevelStats().ToString());
                }

                if (QualityScoreQc != null && QualityScoreQc.IsReady)
                {
                    csvOut.WriteLine(stats.WriteQualityScoreLevelStats().ToString());
                }

                if (HasPlottedSequenceStats)
                {
                    csvOut.WriteLine(stats.WriteSequenceLevelHyperlinks(excelFormat).ToString());
                }

                if (HasPlottedQualityScoreStats)
                {
                    csvOut.WriteLine(stats.WriteQualityScoreLevelHyperlinks(excelFormat).ToString());
                }
            }
        }

        /// <summary>
        /// Return the filename 
        /// </summary>
        /// <returns></returns>
        public string GetPrefix()
        {
            return myFilenames.Prefix;
        }

        /// <summary>
        /// Nullifies SequenceQc to free up memory
        /// </summary>
        public void FinishSequenceQc()
        {
            SequenceQc = null;
        }

        /// <summary>
        /// Nullifies QualityScoreQc and performs clean up tasks of related files
        /// </summary>
        public void FinishQualityScoreQC()
        {
            //File.Delete(QualityScoreQc.StreamFileName);
            QualityScoreQc = null;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Event handler for dealing with unresolved assemblies at runtime. 
        /// This has been added to deal with Sho libraries that are located outside of this application's
        /// program folder. 
        /// The code below has been copied from source (1).
        /// 
        /// Update: This method has been deprecated.
        /// 
        /// Sources:
        /// 1. http://support.microsoft.com/kb/837908
        /// 2. http://msdn.microsoft.com/en-us/library/system.appdomain.assemblyresolve.aspx
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /**
        private Assembly OnAssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            Assembly MyAssembly, objExecutingAssemblies;
            string strTempAssmbPath = "";

            objExecutingAssemblies = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(",")))
                {
                    //Build the path of the assembly from where it has to be loaded.
                    string shoDir = Environment.GetEnvironmentVariable("SHODIR");
                    strTempAssmbPath = shoDir + @"bin\" + args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                    break;
                }

            }
            //Load the assembly from the specified path. 					
            MyAssembly = Assembly.LoadFrom(strTempAssmbPath);

            //Return the loaded assembly.
            return MyAssembly;
        }
         **/

        #endregion

    }
}