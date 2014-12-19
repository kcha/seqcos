// *********************************************************************
// 
//     Copyright (c) 2011 Microsoft. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *******************************************************************
using System;

namespace SeqcosApp.Analyzer.NCBI
{
    /// <summary>
    /// This class holds the specific parameters used
    /// by UniVec to BLAST sequences.
    /// See: http://www.ncbi.nlm.nih.gov/VecScreen/VecScreen_docs.html#Parameters
    /// </summary>
    public class UniVecParameters : IBlastParameters
    {
        #region Properties

        /// <summary>
        /// Cost to open a gap
        /// </summary>
        public int GapOpen { get; set; }

        /// <summary>
        /// Cost to extend a gap
        /// </summary>
        public int GapExtend { get; set; }

        /// <summary>
        /// Penalty for nucleotide mismatch
        /// </summary>
        public int Mismatch { get; set; }

        /// <summary>
        /// Expectation value (E) threshold for saving hits
        /// </summary>
        public double EValue { get; set; }

        /// <summary>
        /// Alignment view options (Use -help for more info
        /// and for custom output formats)
        ///  -outfmt 
        ///       alignment view options:
        ///         0 = pairwise,
        ///         1 = query-anchored showing identities,
        ///         2 = query-anchored no identities,
        ///         3 = flat query-anchored, show identities,
        ///         4 = flat query-anchored, no identities,
        ///         5 = XML Blast output,
        ///         6 = tabular,
        ///         7 = tabular with comment lines,
        ///         8 = Text ASN.1,
        ///         9 = Binary ASN.1,
        ///        10 = Comma-separated values,
        ///        11 = BLAST archive format (ASN.1)
        /// </summary>
        public string OutputFormat { get; set; }

        /// <summary>
        /// Number of threads (CPUs) to use in the BLAST search
        /// </summary>
        public int Threads { get; set; }

        /// <summary>
        /// BLAST database name
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Number of input sequences to be searched with BLAST
        /// </summary>
        public int NumInputSequences { get; set; }

        #endregion

        /// <summary>
        /// Creates a class containing pre-defined UniVec parameters
        /// </summary>
        public UniVecParameters()
        {
            GapOpen = 3;
            GapExtend = 3;
            Mismatch = -5;
            EValue = 700;
            Threads = Environment.ProcessorCount;
            OutputFormat = Properties.Resource.BLAST_OUTPUT_FORMAT;
            Database = Properties.Resource.UniVec;
            NumInputSequences = Convert.ToInt32(Properties.Resource.BLAST_MAX_SEQUENCES_DEFAULT);
        }
    }
}
