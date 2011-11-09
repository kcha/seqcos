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
using System.Linq;
using System.Text;
using Bio;
using Bio.IO;
using Bio.IO.FastQ;
using Bio.IO.FastA;
using System.IO;

namespace SeqcosApp
{
    /// <summary>
    /// Collection of custom Bio-related helper functions
    /// </summary>
    public static class BioHelper
    {
        /// <summary>
        /// Convert a list of ISequences to FASTA format and write to file. In order to reduce the amount of compute time required
        /// by BLAST, we limit the number of sequences being fed to BLAST.
        /// </summary>
        /// <param name="sequences">IEnumerable list of Sequence objects</param>
        /// <param name="output">Name of the output FASTA file</param>
        /// <param name="maxSequences">Optional maximum number of sequences to convert</param>
        /// <param name="overwrite">If true, any existing file with the same name will be overwritten. Otherwise, the file will not be overwritten and conversion will be skipped.</param>
        /// <returns>True if a Fasta file was written, false if it already exists</returns>
        public static bool ConvertToFASTA(IEnumerable<ISequence> sequences, string output, int maxSequences, bool overwrite = false)
        {
            // If conditions:
            // 1. File doesn't exist; OR
            // 2. File exists but is empty; OR
            // 3. File exists but overwrite flag is set.
            if (!File.Exists(output) || new FileInfo(output).Length == 0 || overwrite)
            {
                FastAFormatter fa = new FastAFormatter(output);

                int count = 0;
                foreach (var seqObj in sequences)
                {
                    fa.Write(seqObj);
                    ++count;

                    if (count >= maxSequences)
                        break;
                }

                fa.Close();
                return true;
            }
            return false;
        }

        /// <summary>
        /// This will determine the valid file types supported
        /// by the framework. 
        /// </summary>
        /// <returns>List of file filters supported by the QC app</returns>
        public static List<string> QuerySupportedFileFilters()
        {
            List<string> filters = new List<string>();

            // Add the generic all files filter
            filters.Add("All files (*.*)|*.*");

            foreach (var parser in InputSubmission.ValidParsers)
            {
                StringBuilder extensions = new StringBuilder(parser.SupportedFileTypes);
                extensions.Replace(".", "*.");
                extensions.Replace(",", ";");

                StringBuilder str = new StringBuilder();
                str.AppendFormat("{0} files ", parser.Name);
                str.AppendFormat("({0})", extensions.ToString());
                str.Append("|");
                str.Append(extensions.ToString());

                filters.Add(str.ToString());
            }

            return filters;
        }

        /// <summary>
        /// Query Bio for valid Fastq format types
        /// </summary>
        /// <returns></returns>
        public static Array QueryValidFastqFormats()
        {
            Array formats = Enum.GetValues(typeof(FastQFormatType));        
            return formats;
        }

        /// <summary>
        /// Get the FastQFormatType enum value corresponding to a given string value
        /// </summary>
        /// <param name="formatAsString">Fastq format as a string</param>
        /// <returns>FastQ format type enum</returns>
        public static FastQFormatType GetQualityFormatType(string formatAsString)
        {
            try
            {
                FastQFormatType format = (FastQFormatType)Enum.Parse(typeof(FastQFormatType), formatAsString, true);

                return format;
            }
            catch (ArgumentNullException ex)
            {
                throw new ArgumentNullException(ex.Message);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
    }
}
