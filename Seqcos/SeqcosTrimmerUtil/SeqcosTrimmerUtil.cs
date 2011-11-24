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
using Bio.IO;
using Bio.IO.FastA;
using Bio.IO.FastQ;
using Bio.Util.ArgumentParser;
using SeqcosApp;
using SeqcosFilterTools.Common;
using SeqcosFilterTools.Trim;

namespace SeqcosTrimmerUtil
{
    #region Command line options

    /// <summary>
    /// Class to establish default command line options
    /// </summary>
    class CommandLineOptions
    {
        public bool Help = false;
        public bool Verbose = false;
        public string[] FileList;
        public string InputFile
        {
            get { return FileList[0]; }
        }
        public string OutputFile
        {
            get { return FileList[1]; }
        }
        public string DiscardedFile;
        public double TrimByLength;       // -l
        public byte TrimByQuality;        // -q
        public string TrimByRegex;        // -r
        public bool Left;                 // -L -- trim from left side of read             


        /// <summary>
        /// Constructor - set default parameters
        /// </summary>
        public CommandLineOptions()
        {
            FileList = null;
            DiscardedFile = null;
            TrimByLength = 0;
            TrimByQuality = 0;
            TrimByRegex = null;
            Left = false;
        }
    }

    #endregion


    /// <summary>
    /// Console application for executing read trimming functions
    /// </summary>
    class SeqcosTrimmerUtil
    {
        /// <summary>
        /// usage: SeqcosTrimmerUtil.exe [options] <input> <output>
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            Console.Error.WriteLine(SplashString());

            CommandLineOptions myArgs = ProcessCommandLine(args);

            #region Trimming
            // Determine parser
            InputSubmission input = new InputSubmission(myArgs.InputFile);
            input.DetermineParserUtil();

            // Create a sequence filteredFormatter object
            ISequenceFormatter filteredFormatter;
            ISequenceFormatter discardedFormatter = null;

            // If the format is FASTA, then output will be FASTA.
            // Everything else (assuming quality scores are available)
            // will be outputted to FASTQ.
            if (input.Parser is FastAParser)
            {
                if (myArgs.TrimByQuality > 0)
                {
                    Console.Error.WriteLine("Cannot trim by quality using a FASTA file.");
                    Environment.Exit(-1);
                }

                if (myArgs.DiscardedFile != null)
                {
                    discardedFormatter = new FastAFormatter(myArgs.DiscardedFile);
                }

                filteredFormatter = new FastAFormatter(myArgs.OutputFile);
            }
            else
            {
                if (myArgs.DiscardedFile != null)
                {
                    discardedFormatter = new FastQFormatter(myArgs.DiscardedFile);
                }

                filteredFormatter = new FastQFormatter(myArgs.OutputFile);
            }

            // Initialize a Trimmer object
            Trimmer myTrimmer = null;

            // By now, we should have sanity checked the command line arguments. So we should be able to
            // figure out what mode is being used simply by checking the properties.
            if (myArgs.TrimByLength > 0)
            {
                Console.Error.WriteLine("Trimming reads to length {0}", myArgs.TrimByLength);
                myTrimmer = new TrimByLength(input.Parser, filteredFormatter, discardedFormatter, myArgs.TrimByLength, myArgs.Left);
            }

            else if (myArgs.TrimByQuality > 0)
            {
                if (!(input.Parser is FastQParser))
                    throw new ArgumentException("Input file must be in FASTQ format.");

                Console.Error.WriteLine("Trimming reads based on quality score {0}", myArgs.TrimByQuality);
                myTrimmer = new TrimByQuality(input.Parser, filteredFormatter, discardedFormatter, (byte)myArgs.TrimByQuality, myArgs.Left, (int)Math.Round(myArgs.TrimByLength));
            }

            else if (myArgs.TrimByRegex != null)
            {
                Console.Error.WriteLine("Trimming reads based on the regular expression pattern {0}", myArgs.TrimByRegex);
                myTrimmer = new TrimByRegex(input.Parser, filteredFormatter, discardedFormatter, myArgs.TrimByRegex);
            }

            else
            {
                // Should never reach this line.
                Console.Error.WriteLine("Invalid trim mode. Use '-l' or '-q'.");
                Environment.Exit(-1);
            }


            myTrimmer.TrimAll();


            #endregion

            if (myArgs.Verbose)
            {
                Console.Error.WriteLine("Trimmed {0}/{1} sequences.", myTrimmer.TrimCount, myTrimmer.Counted);
                Console.Error.WriteLine("Discarded {0}/{1} sequences.", myTrimmer.DiscardCount, myTrimmer.Counted);
                Console.Error.WriteLine("Output saved in {0}.", Path.GetFullPath(myArgs.OutputFile));
                Console.Error.WriteLine("Warning: Output may not be in the same order as the original input.");
            }

            input.Parser.Close();
            filteredFormatter.Close();
            if (discardedFormatter != null) { discardedFormatter.Close(); }
        }

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

            if (myArgs.Help)
            {
                const string helpString = "Usage: SeqcosTrimmerUtil.exe [options] <input file> <output file>\n"
                                    + "\nDescription: Trim reads based on length (use option -l) or quality (use option -q and optionally -l).\n"
                                    + "\n/Help (/h)\n  Show this Help information"
                                    + "\n/Verbose (/v)\n  Display more information"
                                    + "\n\n*** Trim modes ***"
                                    + "\n\n/TrimByLength:<INT > 0> (/l)\n  Minimum length that reads will be trimmed to. Reads whose original length is less than INT will not be discarded."
                                    + "\n\n/TrimByQuality:<INT > 0> (/q)\n  Minimum Phred-based quality score that reads will be trimmed to. Trimming is done by finding the subsequence whose "
                                        + "sum of differences between base quality score and cutoff is maximized. If a subsequence cannot be found due to poor quality, the entire read will be "
                                        + "discarded. \n\n  Can be combined with /l option to set a minimum trimming length (e.g. if '/q 15 /l 10', any read that gets trimmed to a length less than "
                                        + "10 will be discarded."
                                    + "\n\n/TrimByRegex:<Pattern> (/r)\n  Trim reads based on a .NET Framework regular expression pattern. Any matches found, will be stripped from the sequence. "
                                        + "For more information on .NET supported regular expressions, please visit: http://msdn.microsoft.com/en-us/library/az24scfc.aspx"
                                    + "\n\n*** Trim options ***\n"
                                    + "\n\n/Left (/L)\n  When combined with /l, trimming occurs from the beginning of the read. When combined with /q, "
                                        + "trimming occurs at both ends of the read. For both modes, this option is OFF (i.e. only trim from right side)."
                                    + "\n\n/DiscardedFilename:<String> (/D)\n  Filename to store discarded reads [optional]."
                                    ;
                Console.WriteLine(helpString);
                Environment.Exit(-1);
            }

            // Process all the arguments
            if (!File.Exists(myArgs.InputFile))
            {
                Console.Error.WriteLine("Error: The file {0} could not be found.", myArgs.InputFile);
                Environment.Exit(-1);
            }
            
            if (myArgs.TrimByLength == 0 && myArgs.TrimByQuality == 0 && myArgs.TrimByRegex != null)
            {
                Console.Error.WriteLine("You must choose a trim mode.");
                Environment.Exit(-1);
            }
            if (myArgs.TrimByLength < 0)
            {
                Console.Error.WriteLine("Trim length must be greater than zero");
                Environment.Exit(-1);
            }
            if (myArgs.TrimByQuality < 0)
            {
                Console.Error.WriteLine("Quality score threshold must be greater than zero");
                Environment.Exit(-1);
            }

            if (myArgs.TrimByRegex != null && !RegexTools.IsValidRegexPattern(myArgs.TrimByRegex))
            {
                Console.Error.WriteLine("Error: unable to verify your regular expression pattern.");
                Environment.Exit(-1);
            }

            return myArgs;
        }

        /// <summary>
        /// Splash text when the exe is called.
        /// </summary>
        /// <returns>Splash text</returns>
        static string SplashString()
        {
            string splashString = "\nSeQCoS Trimmer Tool, Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3)
                                    + "\nCopyright (c) Microsoft, 2011. All rights reserved.\n"
                //+ "<url>"
                                    ;
            return splashString;
        }

        private static void AddParameters(CommandLineArguments parser)
        {
            parser.Parameter(ArgumentType.Optional, "Help", ArgumentValueType.Bool, "h", "Show this Help information.");
            parser.Parameter(ArgumentType.Optional, "Verbose", ArgumentValueType.Bool, "v", "Display more information.");
            parser.Parameter(ArgumentType.Optional, "TrimByLength", ArgumentValueType.Int, "l", "Maximum length that reads will be trimmed to.");
            parser.Parameter(ArgumentType.Optional, "TrimByQuality", ArgumentValueType.Int, "q", "Minimum Phred-based quality score that reads will be trimmed to, based on a moving average.");
            parser.Parameter(ArgumentType.Optional, "TrimByRegex", ArgumentValueType.String, "r", "Regular expression pattern.");
            parser.Parameter(ArgumentType.Optional, "Left", ArgumentValueType.Bool, "L", "Trim from left side of read.");
            parser.Parameter(ArgumentType.Optional, "DiscardedFile", ArgumentValueType.String, "D", "Discarded filename.");
            parser.Parameter(ArgumentType.DefaultArgument, "FileList", ArgumentValueType.MultipleUniqueStrings, "", "Input and output files.");
        }
        #endregion
    }
}
