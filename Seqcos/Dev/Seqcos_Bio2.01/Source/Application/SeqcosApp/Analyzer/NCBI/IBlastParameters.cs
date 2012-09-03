// *******************************************************************************
// 
//     Copyright (c) 2011 Microsoft. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *******************************************************************************
namespace SeqcosApp.Analyzer.NCBI
{
    /// <summary>
    /// Defines a set of parameters required by BLAST.
    /// </summary>
    public interface IBlastParameters
    {
        /// <summary>
        /// Cost to open a gap
        /// </summary>
        int GapOpen { get; set; }

        /// <summary>
        /// Cost to extend a gap
        /// </summary>
        int GapExtend { get;  set; }

        /// <summary>
        /// Penalty for nucleotide mismatch
        /// </summary>
        int Mismatch { get;  set; }

        /// <summary>
        /// Expectation formatAsString (E) threshold for saving hits
        /// </summary>
        double EValue { get;  set; }

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
        string OutputFormat { get;  set; }

        /// <summary>
        /// Number of threads (CPUs) to use in the BLAST search
        /// </summary>
        int Threads { get;  set; }

        /// <summary>
        /// BLAST database name
        /// </summary>
        string Database { get;  set; }

        /// <summary>
        /// Number of input sequences to be searched with BLAST
        /// </summary>
        int NumInputSequences { get; set; }
    }
}
