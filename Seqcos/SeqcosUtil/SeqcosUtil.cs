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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bio;
using Bio.IO.FastA;
using Bio.Util.ArgumentParser;
using SeqcosApp;
using SeqcosApp.Analyzer.NCBI;
using SeqcosApp.Properties;

namespace SeqcosUtil
{
    /// <summary>
    /// Console application for executing the QC application
    /// </summary>
    public class SeqcosUtil
    {
        #region Command line options

        /// <summary>
        /// Class to establish default command line options
        /// </summary>
        internal class CommandLineOptions
        {
            public bool help = false;
            public bool silent = false;
            public string InputFile;
            public string OutputDirectory;  // -o
            public bool ExecuteBlast;       // -B
            public string BlastDbPrefix;    // -D
            public int BlastSize;
            public bool ExecuteSequenceQc = true;
            public bool ExecuteQualityScoreQc = true;
            public string FastqFormat;

            // Control whether Excel-formatted hyperlinks are used in the output file
            public bool UseExcelHyperlinks;        // -e

            /// <summary>
            /// Constructor - set default parameters
            /// </summary>
            public CommandLineOptions()
            {
                InputFile = null;
                OutputDirectory = null;
                ExecuteBlast = false;
                BlastDbPrefix = Resource.BLAST_DB_DEFAULT;
                UseExcelHyperlinks = false;
                BlastSize = Convert.ToInt32(Resource.BLAST_MAX_SEQUENCES_DEFAULT);
                FastqFormat = null;
            }

            /// <summary>
            /// Write text to standard output if verbose is turned on
            /// </summary>
            /// <param name="text"></param>
            public void WriteLine(string text)
            {
                if (!silent)
                    Console.WriteLine(text);
            }

            /// <summary>
            /// Write text to standard error if verbose is turned on
            /// </summary>
            /// <param name="text"></param>
            public void ErrorWriteLine(string text)
            {
                if (!silent)
                    Console.Error.WriteLine(text);
            }
        }

        #endregion

        #region Main

        private Seqcos qcm;

        /// <summary>
        /// Execute QC program in console
        /// </summary>
        /// <param name="args">Arguments entered in console mode</param>
        static void Main(string[] args)
        {
            Console.Error.WriteLine(SplashString());

            // Check arguments
            if (args.Length == 0)
            {
                throw new ArgumentException("Missing arguments");
            }
            else
            {
                CommandLineOptions myArgs = ProcessCommandLine(args);

                SeqcosUtil p = new SeqcosUtil();
                p.Run(myArgs);
            }
        }

        #endregion

        /// <summary>
        /// Run the QC application module
        /// </summary>
        /// <param name="file"></param>
        private void Run(CommandLineOptions myArgs)
        {
            #region Determine parser
            // Determine parser type
            InputSubmission input = new InputSubmission(myArgs.InputFile);
            input.DetermineParserUtil();

            if (input.Parser is Bio.IO.FastQ.FastQParser &&
                    myArgs.FastqFormat == null)
            {
                myArgs.ErrorWriteLine("For FASTQ input, please provide a valid FASTQ format: [Sanger, Solexa, Illumina]");
            }
            #endregion

            myArgs.WriteLine("Processing the file " + myArgs.InputFile +
                        "...this may take a while depending on the input size. Please be patient!");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            #region Run QC analysis
            // Run QC analysis
            try
            {
                qcm = new Seqcos(input.Parser, myArgs.InputFile, myArgs.ExecuteSequenceQc, myArgs.ExecuteQualityScoreQc, myArgs.ExecuteBlast, myArgs.FastqFormat, dir: myArgs.OutputDirectory);
            }
            catch (ArgumentNullException e)
            {
                myArgs.ErrorWriteLine(e.Message);
            }

            myArgs.WriteLine("Performing sequence-level QC...");
            qcm.SequenceQc.Process();
            var time = ElapsedSeconds(sw);
            Console.WriteLine(time.ToString() + " s");

            if (!(input.Parser is FastAParser))
            {
                myArgs.WriteLine("Performing quality score-level QC...");
                qcm.QualityScoreQc.Process();
                time = ElapsedSeconds(sw, (long)time);
                Console.WriteLine(time.ToString() + " s");
            }

            #endregion

            #region Display statistics to console
            if (!myArgs.silent)
            {
                qcm.WriteInputStatistics(myArgs.UseExcelHyperlinks);
                DisplayInputStatistics();
            }
            #endregion

            #region Generate plots
            myArgs.WriteLine("Generating plots and saving them to: " + qcm.FullOutputDirectory);

            // Do these last, so that after plotting each section you can't free up memory
            qcm.PlotSequenceLevelStats();
            qcm.FinishSequenceQc();      // free up some memory, since we won't be using this anymore

            if (!(input.Parser is FastAParser))
            {
                qcm.PlotQualityScoreLevelStats();
                qcm.FinishQualityScoreQC();
            }

            #endregion

            #region Carry out BLAST analysis

            if (myArgs.ExecuteBlast)
            {
                myArgs.WriteLine("Searching for contaminants...");
                // Convert FASTQ to FASTA
                string targetFasta = qcm.OutputDirectory + "/" + qcm.GetPrefix() + ".fa";
                BioHelper.ConvertToFASTA(qcm.ContaminationFinder.TargetSequences, targetFasta, myArgs.BlastSize, overwrite: true);

                // Run local NCBI BLAST
                qcm.ContaminationFinder.RunLocalBlast(myArgs.BlastDbPrefix, targetFasta);
                time = ElapsedSeconds(sw, (long)time);
                Console.WriteLine(time.ToString() + " s");
                File.Delete(targetFasta);

                if (!myArgs.silent)
                {
                    HighlightText("NCBI BLAST results from searching against " + myArgs.BlastDbPrefix + " database:\n");
                    DisplayBlastResults();
                }
            }

            #endregion


            input.Parser.Close();

            sw.Stop();
            myArgs.WriteLine("\nTotal time: " + ToSeconds(sw.ElapsedMilliseconds) + " s");
        }

        #region BLAST-related methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ftp"></param>
        /// <param name="dbFilename"></param>
        /// <param name="overwrite"></param>
        private void GetAndPrepareBlastDB(string ftp, string dbFilename, bool overwrite)
        {
            Console.Write("Downloading {0} from {0}...", dbFilename, ftp);
            var status = BlastLocalHandler.DownloadFasta(ftp, dbFilename, qcm.ContaminationFinder.Blaster.BlastDbPath, overwrite);
            Console.WriteLine("status {0}", status);

            // FastqFormat FASTA to BLAST database
            string fastaFile = qcm.ContaminationFinder.Blaster.BlastDbPath + "/" + dbFilename;
            string resultStdOut, resultStdErr;
            BlastLocalHandler.MakeBlastDb(fastaFile, 
                qcm.ContaminationFinder.Blaster.BlastDbPath, "nucl", out resultStdOut, out resultStdErr);
            Console.Write(resultStdOut);
            Console.Error.Write(resultStdErr);
        }

        #endregion

        #region Display Methods

        /// <summary>
        /// Print summary of base content by position to console
        /// </summary>
        /// <param name="qcm">Main QC application</param>
        private void PrintContentByPosition()
        {
            List<byte> symbols = qcm.SequenceQc.SymbolCountByPositionTable.Keys.ToList();

            foreach (var symbol in symbols)
            {
                Console.Write((char)symbol + ": ");

                for (int i = 0; i < qcm.SequenceQc.ReadLengthMax; i++)
                {
                    Console.Write(qcm.SequenceQc.SymbolCountByPositionTable[symbol][i] + "  ");
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Print summary of base content by sequence to console
        /// </summary>
        private void PrintContentBySequence()
        {
            foreach (var stats in qcm.SequenceQc.SymbolCountBySequenceArray)
            {
                Console.WriteLine(stats.ToString());
            }
        }

        /// <summary>
        /// Show input statistics of the input data
        /// </summary>
        /// <param name="qcm">Main QC application</param>
        private void DisplayInputStatistics()
        {
            if (!qcm.SequenceQc.HasRunContentByPosition)
                throw new ArgumentException("Sequence-level QC has not been run.");

            HighlightText("\n--- INPUT SUMMARY STATISTICS ---\n");

            HighlightText("Total # of reads: ");
            Console.WriteLine(qcm.SequenceQc.Count);

            HighlightText("Min/Max/Average read length: ");
            Console.WriteLine(qcm.SequenceQc.ReadLengths.Min() + " / " +
                            qcm.SequenceQc.ReadLengths.Max() + " / " +
                                qcm.SequenceQc.ReadLengths.Average());

            if (!(qcm.SelectedParser is FastAParser))
            {
                if (!qcm.QualityScoreQc.HasRunContentByPosition)
                    throw new ArgumentException("Quality score-level QC has not been run.");

                HighlightText("Min/Max base quality score: ");
                Console.WriteLine(qcm.QualityScoreQc.BaseQualityScoreMin + " / " + qcm.QualityScoreQc.BaseQualityScoreMax);

                HighlightText("Min/Max/Average read quality score: ");
                Console.WriteLine(Math.Round(qcm.QualityScoreQc.ReadQualityScoreMin, 1) + " / " +
                                    Math.Round(qcm.QualityScoreQc.ReadQualityScoreMax, 1) + " / " +
                                    Math.Round(qcm.QualityScoreQc.ReadQualityScoreMean, 1));

                HighlightText("Encoding Scheme: ");
                Console.WriteLine(qcm.QualityScoreQc.FormatType.ToString());
            }
        }

        /// <summary>
        /// Display results from NCBI BLAST local alignment. For each match to the 
        /// reference database (i.e. UniVec), keep track of the total number of hits.
        /// </summary>
        private void DisplayBlastResults()
        {
            if (qcm.ContaminationFinder.BlastHspCounter.Count == 0)
            {
                Console.WriteLine("No matches found.");
            }
            else
            {
                // Print number of query reads that have a HSP with the subject
                foreach (KeyValuePair<string, List<string>> match in qcm.ContaminationFinder.BlastHspCounter)
                {
                    Console.WriteLine(match.Key + ": " + match.Value.Count.ToString() + " hits");
                }
            }
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Parse command line arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static CommandLineOptions ProcessCommandLine(string[] args)
        {
            CommandLineOptions myArgs = new CommandLineOptions();
            CommandLineArguments parser = new CommandLineArguments();

            AddParameters(parser);

            try
            {
                parser.Parse(args, myArgs);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("\nException while processing command line arguments [{0}]", e.Message);
                Environment.Exit(-1);
            }

            if (myArgs.help)
            {
                string[] validFastqFormats = BioHelper.QueryValidFastqFormats();

                string helpString = "Usage: SeqcosUtil.exe [options] <input file>\n"
                                    + "\nDescription: Evaluates the quality of sequencing reads and summarizes\n"
                                    + " the results in the form of text and plots. BLAST may be optionally performed\n"
                                    + " against a custom database (i.e. when looking for sequence contamination)."
                                    + "\n\n/help (/h)\n  Show this help information"
                                    + "\n\n/silent (/s)\n  Show less details displayed in the console"
                                    + "\n\n/FastQFormat (/q)\n  Required for FASTQ input files. Choose from [" + string.Join(",", validFastqFormats) + "]"
                                    + "\n\n/OutputDirectory:<string> (/o)\n  Directory where all output files will be saved"
                                    + "\n\n/UseExcelHyperlinks (/e)\n  Outputs Excel-formatted hyperlinks in the csv file"
                                    + "\n\n\n*** BLAST Options ***\n"
                                    + "\n\n/ExecuteBlast (/B)\n  Perform a BLAST of the input sequences against a custom database. Windows NCBI BLAST must be installed first"
                                    + "\n\n/BlastDbPrefix:<string> (/D)\n  Database to use for BLAST. The default is UniVec (http://www.ncbi.nlm.nih.gov/VecScreen/UniVec.html)"
                                    + "\n\n/BlastSize:<int> (/S)\n  Limit the number of sequences to be searched by BLAST. Default is " + Resource.BLAST_MAX_SEQUENCES_DEFAULT + "."
                                    ;
                Console.WriteLine(helpString);
                Environment.Exit(-1);
            }

            // Process all the arguments for correctness
            if (!File.Exists(myArgs.InputFile))
            {
                Console.Error.WriteLine("The file {0} could not be found.", myArgs.InputFile);
                Environment.Exit(-1);
            }

            if (myArgs.OutputDirectory == null)
            {
                myArgs.OutputDirectory = Path.GetFileNameWithoutExtension(myArgs.InputFile);
            }
            if (!Directory.Exists(myArgs.OutputDirectory))
            {
                Directory.CreateDirectory(myArgs.OutputDirectory);
            }
            
            if (myArgs.ExecuteBlast)
            {
                // check to make sure Windows BLAST is installed
                if (!BlastLocalHandler.IsLocalBLASTInstalled(BlastLocalHandler.BlastVersion))
                {
                    Console.Error.WriteLine("Unable to find {0} in your PATH environment variable. Please check that BLAST is correctly installed.");
                    Environment.Exit(-1);
                }

                if (myArgs.BlastSize < 0)
                {
                    Console.Error.WriteLine("/BlastSize must be greater than 0.");
                    Environment.Exit(-1);
                }
            }

            return myArgs;
        }

        /// <summary>
        /// Splash text when the exe is called.
        /// </summary>
        /// <returns></returns>
        static string SplashString()
        {
            string splashString = "\n\nSeQCoS command-line utility, Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3)
                                    + "\n\nCopyright (c) Microsoft, 2011. All rights reserved.\n"
                                    //+ "<url>"
                                    ;
            return splashString;
        }

        private static void AddParameters(CommandLineArguments parser)
        {
            parser.Parameter(ArgumentType.Optional, "help", ArgumentValueType.Bool, "h", "Show this help information.");
            parser.Parameter(ArgumentType.Optional, "silent", ArgumentValueType.Bool, "s", "Suppress console messages.");
            parser.Parameter(ArgumentType.Optional, "OutputDirectory", ArgumentValueType.String, "o", "Directory where all output files will be saved in.");
            parser.Parameter(ArgumentType.Optional, "FastqFormat", ArgumentValueType.String, "q", "Fastq format type");
            parser.Parameter(ArgumentType.Optional, "ExecuteBlast", ArgumentValueType.Bool, "B", "Perform a BLAST of the input sequences against a custom database. Windows NCBI BLAST must be installed first.");
            parser.Parameter(ArgumentType.Optional, "BlastDbPrefix", ArgumentValueType.String, "D", "Database to use for BLAST. The default is UniVec (http://www.ncbi.nlm.nih.gov/VecScreen/UniVec.html).");
            parser.Parameter(ArgumentType.Optional, "BlastSize", ArgumentValueType.Int, "S", "Limit the number of sequences to BLAST.");
            parser.Parameter(ArgumentType.Optional, "UseExcelHyperlinks", ArgumentValueType.Bool, "e", "Control whether Excel-formatted hyperlinks are used in the output file");
            parser.Parameter(ArgumentType.DefaultArgument, "inputFile", ArgumentValueType.String, "", "Input file for processing.");
        }

        #endregion

        #region Other private methods

        /// <summary>
        /// Helper method for changing the color of the output text and writing it to Console.
        /// </summary>
        /// <param name="text"></param>
        private void HighlightText(string text)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(text);
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Convert milliseconds to seconds
        /// </summary>
        /// <param name="ms">Time in milliseconds</param>
        /// <returns>Time in seconds</returns>
        private double ToSeconds(long ms)
        {
            return Math.Round(ms / 1000.0, 3);
        }

        /// <summary>
        /// Return elapsed time at the point this function is called.
        /// </summary>
        /// <param name="sw">Stopwatch object that is counting the time.</param>
        /// <param name="previousElapsedTime">Optional parameter to include to count from.</param>
        /// <returns></returns>
        private double ElapsedSeconds(Stopwatch sw, long previousElapsedTime = 0)
        {
            if (previousElapsedTime < 0)
                throw new ArgumentException("Previous elapsed time must be 0 or greater.");

            if (sw.IsRunning)
            {
                var elapsedTime = sw.ElapsedMilliseconds;

                return ToSeconds(elapsedTime - previousElapsedTime);
            }

            return -1;
        }

        #endregion
    }
}