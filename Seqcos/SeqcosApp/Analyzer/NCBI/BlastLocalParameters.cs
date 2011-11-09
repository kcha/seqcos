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
using System.Linq;
using System.Text;
using System.IO;
using SeqcosApp.Properties;

namespace SeqcosApp.Analyzer.NCBI
{
    /// <summary>
    /// Sets the parameters for executing a local NCBI BLAST on Windows
    /// </summary>
    public class BlastLocalParameters : IBlastParameters
    {
        #region Private Members

        private int mismatch;
        private int threads;

        #endregion

        #region Public Members

        public int GapOpen { get; set; }

        public int GapExtend { get; set; }

        public int Mismatch
        {
            get
            {
                return this.mismatch;
            }
            set
            {
                if (value > 0)
                    throw new ArgumentOutOfRangeException("Mismatch value must be <= 0.");
                this.mismatch = value;
            }
        }

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
        /// BLAST database name
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Number of input sequences to be searched with BLAST
        /// </summary>
        public int NumInputSequences { get; set; }

        /// <summary>
        /// Number of threads (CPUs) to use in the BLAST search
        /// </summary>
        public int Threads
        {
            get
            {
                return this.threads;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("Number of threads must be >= 1.");
                this.threads = value;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Selection of BLAST parameters to be set. Not all available parameters
        /// are defined here. Only those for the purposes of this application.
        /// </summary>
        public BlastLocalParameters()
        {
            // Set default values of parameters.
            // Source: http://www.ncbi.nlm.nih.gov/BLAST/blastcgihelp.shtml#other_advanced
            GapOpen = 5;
            GapExtend = 2;
            Mismatch = -2;        
            OutputFormat = Resource.BLAST_OUTPUT_FORMAT;
            EValue = 10;
            Threads = 1; 
            Database = Resource.BLAST_DB_DEFAULT;
            NumInputSequences = Convert.ToInt32(Properties.Resource.BLAST_MAX_SEQUENCES_DEFAULT);
        }

        #endregion

        #region Other Public methods




        
        #endregion
    }
}
