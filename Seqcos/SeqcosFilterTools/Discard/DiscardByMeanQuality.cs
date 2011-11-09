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
using SeqcosApp.Analyzer;
using Bio;
using Bio.IO;
using Bio.IO.FastQ;

namespace SeqcosFilterTools.Discard
{
    /// <summary>
    /// Discards reads if its mean quality score is less than a specified
    /// minimum threshold.
    /// In order to use this class, the input must be in FASTQ format (or
    /// in the future, BAM and SAM).
    /// </summary>
    public class DiscardByMeanQuality : Discarder
    {
        /// <summary>
        /// The mean quality threshold 
        /// </summary>
        private byte meanQualityThreshold;

        /// <summary>
        /// Gets or sets the mean quality threshold
        /// </summary>
        public byte MeanQualityThreshold
        {
            get { return meanQualityThreshold; }
            set { meanQualityThreshold = value; }
        }


        #region Constructor

        /// <summary>
        /// Constructor for discarding reads based on minimum mean quality score.
        /// 
        /// NOTE:
        /// Require Sanger Phred-base scores (i.e. ASCII-33)
        /// </summary>
        /// <param name="parser">Input sequence parser</param>
        /// <param name="filtered">Formatter for filtered reads</param>
        /// <param name="discarded">Formatter for discarded reads</param>
        /// <param name="mean">Sanger Phred-based mean quality score</param>
        public DiscardByMeanQuality(ISequenceParser parser, ISequenceFormatter filtered, ISequenceFormatter discarded, byte mean)
            : base(parser, filtered, discarded)
        {
            if (!(parser is FastQParser))
                throw new ArgumentException("Invalid SequenceParser type.");
            if (mean < 0 || mean > QualitativeSequence.SangerMaxQualScore - QualitativeSequence.SangerMinQualScore)
                throw new ArgumentOutOfRangeException("Invalid Phred-based quality score threshold.");

            this.MeanQualityThreshold = mean;
        }

        /// <summary>
        /// This is an empty constructor. Used for data binding in WPF.
        /// </summary>
        public DiscardByMeanQuality() : base() { }
        

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether a read satisfies the conditions to be discarded.
        /// i.e. Does it meet the minimum mean quality score requirements?
        /// </summary>
        /// <param name="seqObj">A sequence object</param>
        /// <returns>True if it does not meet the minimum mean and thus
        /// will be discarded. False, otherwise.</returns>
        public override bool CanDiscard(ISequence seqObj)
        {
            var myMean = QualityScoreAnalyzer.GetMeanFromBytes(
                            ((QualitativeSequence)seqObj).QualityScores.ToArray()
                            );

            if (myMean < MeanQualityThreshold)
                return true;
            return false;
        }

        #endregion
    }
}
