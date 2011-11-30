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
using System.IO;
using Bio.IO;
using Bio.IO.FastA;
using Bio.IO.FastQ;
using Bio.Util.ArgumentParser;
using SeqcosApp;
using SeqcosFilterTools.Discard;
using SeqcosFilterTools.Common;

namespace SeqcosDiscarderUtil
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
        public string DiscardedFile;     // -D
        public int DiscardByLength;         // -l
        public int DiscardByQuality;        // -q
        public string DiscardByRegex;       // -r

        /// <summary>
        /// Constructor - set default parameters
        /// </summary>
        public CommandLineOptions()
        {
            FileList = null;
            DiscardedFile = null;
            DiscardByLength = 0;
            DiscardByQuality = 0;
            DiscardByRegex = null;
        }
    }

    #endregion

    /// <summary>
    /// Console application for executing read discarding functions
    /// </summary>
    class SeqcosDiscarderUtil
    {
        static void Main(string[] args)
        {
            Console.Error.WriteLine(SplashString());
            CommandLineOptions myArgs = ProcessCommandLine(args);

            #region Discarding 
            // Determine parser
            InputSubmission input = new InputSubmission(myArgs.FileList[0]);
            input.DetermineParserUtil();

            // Create a sequence formatter object
            ISequenceFormatter filteredFormatter;
            ISequenceFormatter discardedFormatter = null;

            // If the format is FASTA, then output will be FASTA.
            // Everything else (assuming quality scores are available)
            // will be outputted to FASTQ.
            if (input.Parser is FastAParser)
            {
                filteredFormatter = new FastAFormatter(myArgs.FileList[1]);

                if (myArgs.DiscardedFile != null)
                {
                    discardedFormatter = new FastAFormatter(myArgs.DiscardedFile);
                }
            }
            else
            {
                filteredFormatter = new FastQFormatter(myArgs.FileList[1]);

                if (myArgs.DiscardedFile != null)
                {
                    discardedFormatter = new FastQFormatter(myArgs.DiscardedFile);
                }
            }

            // Initialize a Trimmer object
            Discarder myDiscarder = null;

            // By now, we should have sanity checked the command line arguments. So we should be able to
            // figure out what mode is being used simply by checking the properties.
            if (myArgs.DiscardByLength > 0)
            {
                myDiscarder = new DiscardByLength(input.Parser, filteredFormatter, discardedFormatter, myArgs.DiscardByLength);
            }

            else if (myArgs.DiscardByQuality > 0)
            {
                if (!(input.Parser is FastQParser))
                {
                    Console.Error.WriteLine("Input file must be in FASTQ format.");
                    Environment.Exit(-1);
                }

                myDiscarder = new DiscardByMeanQuality(input.Parser, filteredFormatter, discardedFormatter, (byte)myArgs.DiscardByQuality);
            }

            else
            {
                // Should never reach this line.
                Console.Error.WriteLine("Invalid trim mode. Use '-l' or '-q'.");
                Environment.Exit(-1);
            }

            myDiscarder.DiscardReads();

            #endregion

            if (myArgs.Verbose)
            {
                Console.Error.WriteLine("Discarded {0}/{1} sequences.", myDiscarder.DiscardCount, myDiscarder.Counted);
                Console.Error.WriteLine("Non-discarded sequences saved in {0}.", Path.GetFullPath(myArgs.FileList[1]));
                if (myArgs.DiscardedFile != null)
                {
                    Console.Error.WriteLine("Discarded sequences saved in {0}.", Path.GetFullPath(myArgs.DiscardedFile));
                    discardedFormatter.Close();
                }
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
                const string helpString = "Usage: SeqcosDiscarderUtil.exe [options] <input file> <output file>\n"
                                    + "\nDescription: Discard reads based on length (option -l) or quality (-q).\n"
                                    + "\n\n/Help (/h)\n  Show this Help information"
                                    + "\n\n/Verbose (/v)\n  Display more information"
                                    + "\n\n/DiscardedFile:<String> (/D)\n  Filename to store discarded reads [optional]."
                                    + "\n\n*** Discard modes ***"
                                    + "\n\n/DiscardByLength:<INT > 0> (/l)\n  Discard reads with length less than INT."
                                    + "\n\n/DiscardByQuality:<INT > 0> (/q)\n  Discard reads with mean Phred-based quality score less than INT."
                                    + "\n\n/DiscardByRegex:<Pattern> (/r)\n  Discard reads based a .NET Framework regular expression pattern. "
                                        + "For more information on .NET supported regular expressions, please visit: http://msdn.microsoft.com/en-us/library/az24scfc.aspx"
                                    ;
                Console.WriteLine(helpString);
                Environment.Exit(-1);
            }

            // Process all the arguments
            if (!File.Exists(myArgs.FileList[0]))
            {
                Console.Error.WriteLine("Error: The file {0} could not be found.", myArgs.FileList[0]);
                Environment.Exit(-1);
            }

            if ((myArgs.FileList == null) || (myArgs.FileList.Length < 2))
            {
                Console.Error.WriteLine("Error: Must specify an input and output file.");
                Environment.Exit(-1);
            }

            if ((myArgs.DiscardByLength == 0 && myArgs.DiscardByQuality == 0) || (myArgs.DiscardByLength > 0 && myArgs.DiscardByQuality > 0))
            {
                Console.Error.WriteLine("Error: You must choose either -l or -q discard modes.");
                Environment.Exit(-1);
            }

            if (myArgs.DiscardByRegex != null && !RegexTools.IsValidRegexPattern(myArgs.DiscardByRegex))
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
            string splashString = "\nSeQCoS Discarder Tool, Version " + GlobalAssemblyAttributes.GlobalAssemblyAttributes.Version
                                    + "\n" + GlobalAssemblyAttributes.GlobalAssemblyAttributes.Copyright + ". All rights reserved.\n"
                //+ "<url>"
                                    ;
            return splashString;
        }

        private static void AddParameters(CommandLineArguments parser)
        {
            parser.Parameter(ArgumentType.Optional, "Help", ArgumentValueType.Bool, "h", "Show this Help information.");
            parser.Parameter(ArgumentType.Optional, "Verbose", ArgumentValueType.Bool, "v", "Show more information.");
            parser.Parameter(ArgumentType.Optional, "DiscardByLength", ArgumentValueType.Int, "l", "Minimum length threshold for discarding reads.");
            parser.Parameter(ArgumentType.Optional, "DiscardByQuality", ArgumentValueType.Int, "q", "Minimum mean Phred-based quality score for discarding reads.");
            parser.Parameter(ArgumentType.Optional, "DiscardedFile", ArgumentValueType.String, "D", "Discarded filename.");
            parser.Parameter(ArgumentType.Optional, "DiscardByRegex", ArgumentValueType.String, "r", "Regular expression pattern.");
            parser.Parameter(ArgumentType.DefaultArgument, "FileList", ArgumentValueType.MultipleUniqueStrings, "", "Input and output file names.");
        }
        #endregion
    }
}
