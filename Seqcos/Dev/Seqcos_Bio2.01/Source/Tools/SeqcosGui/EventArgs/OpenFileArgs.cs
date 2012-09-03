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
using SeqcosApp;
using SeqcosApp.Analyzer.NCBI;
using Bio;

namespace SeqcosGui
{
    /// <summary>
    /// This class defines the custom Event Arguments for executing the 
    /// QC analysis
    /// </summary>
    public class OpenFileArgs : EventArgs
    {
        #region Public members

        /// <summary>
        /// Holds information about the input file and parser type
        /// </summary>
        public InputSubmission InputInfo { get; private set; }

        /// <summary>
        /// Determines whether sequence QC should be run
        /// </summary>
        public bool CanRunSequenceQc { get; set; }

        /// <summary>
        /// Determines whether quality score QC should be run
        /// </summary>
        public bool CanRunQualityScoreQc { get; set; }

        /// <summary>
        /// Holds arguments for running local BLAST
        /// </summary>
        public IBlastParameters BlastArgs { get; set; }

        /// <summary>
        /// Fastq format type
        /// </summary>
        public string FastqFormat { get; set; }

        #endregion

        public OpenFileArgs(InputSubmission input, bool runSequenceQc, bool runQualityScoreQc, IBlastParameters blast, string format)
        {
            this.InputInfo = input;
            this.CanRunSequenceQc = runSequenceQc;
            this.CanRunQualityScoreQc = runQualityScoreQc;
            this.BlastArgs = blast;
            this.FastqFormat = format;
        }

        /// <summary>
        /// Determines whether BLAST should be executed
        /// </summary>
        public bool CanRunBlast
        {
            get
            {
                if (this.BlastArgs == null)
                    return false;
                return true;
            }
        }
    }
}
