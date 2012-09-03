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
using Bio.IO;
using Bio.IO.FastA;
using Bio.IO.FastQ;
using SeqcosApp.Properties;
using Bio;

namespace SeqcosApp
{
    /// <summary>
    /// Class for handling input files (i.e. FASTQ)
    /// </summary>
    public class InputSubmission
    {
        #region Member Variables

        /// <summary>
        /// Name of the input file
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Collection of parsers supported by this application
        /// </summary>
        public static List<ISequenceParser> ValidParsers = new List<ISequenceParser> {
                                                                                            new FastQParser(),
                                                                                            new FastAParser()
                                                                                       };

        /// <summary>
        /// The parser object of the input file
        /// </summary>
        public ISequenceParser Parser { get; private set; }

        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for processing the input file
        /// </summary>
        /// <param name="file">Input filename</param>
        public InputSubmission(string file)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            this.Filename = file;
            this.Parser = SequenceParsers.FindParserByFileName(file);
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Parse the input file
        /// </summary>
        /// <returns>
        /// true if parsing was successful
        /// false otherwise (can't determine input format)
        /// </returns>
        public bool CanParseInputByFileName()
        {
            if (this.Parser == null)
                return false;

            return true;
        }

        /// <summary>
        /// Parse the input file with given parser type
        /// </summary>
        /// <param name="p">The parser name (i.e. FastQ)</param>
        /// <returns>
        /// true if parsing was successful
        /// false otherwise
        /// </returns>
        public bool CanParseInputByName(string ps)
        {
            if (string.IsNullOrEmpty(ps))
                return false;

            this.Parser = SequenceParsers.FindParserByName(Filename, ps);

            if (this.Parser == null)
                return false;

            return true;
        }

        /// <summary>
        /// For console application, if the format type cannot be detected automatically
        /// by the parser, prompt the user to manually enter the appropriate format (i.e. fastq, fasta, etc.)
        /// </summary>
        public void DetermineParserUtil()
        {
            if (!this.CanParseInputByFileName())
            {
                Console.WriteLine(Resource.AutoDetectFail);

                // Keeps track of whether a valid format has been found
                var validFormat = false;

                // Get list of valid parser names
                List<string> validParserNames = InputSubmission.QuerySupportedParserNames();

                // Ask the user to manually enter the format
                while (!validFormat)
                {
                    // Need to extend to SAM, BAM, gzip in the future
                    Console.WriteLine(Resource.EnterFormatPrompt);

                    string userFormat = Console.ReadLine();

                    if (userFormat == "exit")
                    {
                        Environment.Exit(0);
                    }
                    else if (!validParserNames.Contains(userFormat))
                    {
                        Console.WriteLine(Resource.InvalidFileFormatMessage);
                        continue;
                    }
                      
                    validFormat = this.CanParseInputByName(userFormat);

                    if (!validFormat)
                    {
                        Console.WriteLine(Resource.InvalidFileFormatMessage);
                    }
                }
            }
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// This method will determine the file extensions supported by
        /// this application.
        /// </summary>
        /// <returns>A list of supported file extensions</returns>
        public static List<string> QuerySupportedFileExtensions()
        {
            List<string> extensions = new List<string>();

            foreach (var parser in InputSubmission.ValidParsers)
            {
                extensions.Add(parser.SupportedFileTypes);
            }

            return extensions;
        }

        /// <summary>
        /// This method will determine the names of the parsers
        /// supported by this application.
        /// </summary>
        /// <returns>A list of supported file extensions</returns>
        public static List<string> QuerySupportedParserNames()
        {
            List<string> names = new List<string>();

            foreach (var parser in InputSubmission.ValidParsers)
            {
                names.Add(parser.Name);
            }

            return names;
        }

        #endregion

    }
}