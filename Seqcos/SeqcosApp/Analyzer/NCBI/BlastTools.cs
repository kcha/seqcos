// *********************************************************************
// 
//     Copyright (c) Microsoft, 2011. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *******************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Bio.Web.Blast;

namespace SeqcosApp.Analyzer.NCBI
{
    /// <summary>
    /// Collection of static methods that provide functionality to
    /// BLAST related tasks
    /// </summary>
    public static class BlastTools
    {

        #region Public methods

        /// <summary>
        /// Process a BLAST XML file and store the IDs of the query
        /// and subject sequences that form a HSP.
        /// </summary>
        /// <param name="xmlFilename">Filename of the BLAST XML</param>
        /// <returns>Dictionary of HSPs</returns>
        public static Dictionary<string, List<string>> ProcessBlastXml(string xmlFilename)
        {
            var blastResultsList = ParseBlastXml(xmlFilename);

            var hspTable = new Dictionary<string, List<string>>();

            // Iterate through the BlastResult heirarchy
            foreach (var result in blastResultsList)
            {
                // BLAST search record (aka query sequence) level 
                foreach (var record in result.Records)
                {
                    // HSPs found with this particular record
                    foreach (var hsp in record.Hits)
                    {
                        List<string> queries;

                        if (hspTable.TryGetValue(hsp.Def, out queries))
                        {
                            queries.Add(record.IterationQueryDefinition);
                            hspTable[hsp.Def] = queries;
                        }
                        else
                        {
                            hspTable.Add(hsp.Def, new List<String>() { record.IterationQueryDefinition });
                        }
                    }
                }
            }

            return hspTable;
        }

        /// <summary>
        /// Call blast_formatter executable to convert ASN to other formats defined
        /// in the -outfmt option of BLAST.
        /// </summary>
        /// <param name="format">Format code</param>
        /// <param name="input">Input ASN file</param>
        /// <param name="output">Output file</param>
        public static void FormatASNTo(string format, string input, string output)
        {
            StringBuilder args = new StringBuilder();
            args.Append(FormatArgument("-archive", InDoubleQuotes(input)));
            args.Append(FormatArgument("-outfmt", format, true));
            args.Append(FormatArgument("-out", InDoubleQuotes(output), true));

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Properties.Resource.BLAST_FORMATTER,
                Arguments = args.ToString(),
            };
            Process p = Process.Start(psi);
            p.WaitForExit();
        }

        /// <summary>
        /// Format BLAST arguments to string
        /// </summary>
        /// <returns>Arguments formatted for command-line execution</returns>
        public static string FormatBlastArguments(string query, string output, IBlastParameters args)
        {
            // Check if query file exists
            if (!File.Exists(query))
                throw new FileNotFoundException(Properties.Resource.FileNotFound, query);

            StringBuilder str = new StringBuilder();
            str.Append(FormatArgument("-db", InDoubleQuotes(args.Database)));
            str.Append(FormatArgument("-query", InDoubleQuotes(query), true));
            str.Append(FormatArgument("-out", InDoubleQuotes(output), true));

            if (args.GapOpen != int.MaxValue)
                str.Append(FormatArgument("-gapopen", args.GapOpen.ToString(), true));

            if (args.GapExtend != int.MaxValue)
                str.Append(FormatArgument("-gapextend", args.GapExtend.ToString(), true));

            if (args.Mismatch <= 0)
                str.Append(FormatArgument("-penalty", args.Mismatch.ToString(), true));

            if (args.EValue != int.MaxValue)
                str.Append(FormatArgument("-evalue", args.EValue.ToString(), true));

            str.Append(FormatArgument("-outfmt", args.OutputFormat.ToString(), true));
            str.Append(FormatArgument("-num_threads", args.Threads.ToString(), true));

            return str.ToString();
        }


        /// <summary>
        /// Return a text with double quotes
        /// </summary>
        /// <param name="text">The text to be enclosed with quotes</param>
        /// <returns>The resulting string with enclosed double quotes</returns>
        public static string InDoubleQuotes(string text)
        {
            return "\"" + text + "\"";
        }

        /// <summary>
        /// Helper method for formatting parameters.
        /// </summary>
        /// <param name="parameter">The parameter type/name.</param>
        /// <param name="formatAsString">The formatAsString of the paramter.</param>
        /// <param name="spaceInFront">If true, adds a space in front of the parameter (used when adding multiple parameters).</param>
        /// <returns>Formatted parameter string for command=line execution</returns>
        public static string FormatArgument(string parameter, string value, bool spaceInFront = false)
        {
            string arg = (value == null) ? parameter : parameter + " " + value;
            return spaceInFront ? " " + arg : arg;
        }

        /// <summary>
        /// This will query the default BLAST database directory
        /// and retrieve a list of available databases for query.
        /// </summary>
        /// <returns>A list of unique database names</returns>
        public static List<string> QueryAvailableBlastDatabases()
        {
            Dictionary<string, int> databases = new Dictionary<string, int>();

            string blastDbPath = Environment.GetEnvironmentVariable(Properties.Resource.BlastDbEnvironmentVariable);

            if (blastDbPath != null)
            {
                string[] listing = Directory.GetFiles(blastDbPath);

                foreach (var file in listing)
                {
                    string ext = Path.GetExtension(file);
                    string name = Path.GetFileNameWithoutExtension(file);

                    if (ext == ".nsq")
                        databases.Add(name, 1);
                }
            }

            return new List<string>(databases.Keys);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Parse the BLAST XML result file
        /// </summary>
        /// <param name="xmlFilename">Filename of the BLAST XML file</param>
        /// <returns>BlastXML</returns>
        private static IList<BlastResult> ParseBlastXml(string xmlFilename)
        {
            if (!File.Exists(xmlFilename))
                throw new FileNotFoundException("BLAST report does not exist.");

            return new BlastXmlParser().Parse(xmlFilename);
        }

        #endregion
    }
}
