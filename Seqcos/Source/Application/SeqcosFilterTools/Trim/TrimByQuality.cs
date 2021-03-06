﻿// *********************************************************************
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
using System.Linq;
using Bio;
using Bio.IO;
using Bio.IO.FastQ;
using SeqcosApp.Analyzer;

namespace SeqcosFilterTools.Trim
{
    /// <summary>
    /// Given a FASTQ file, trim each read based on a quality threshold.
    /// </summary>
    public class TrimByQuality : Trimmer
    {
        /// <summary>
        /// The quality score threshold that reads will be trimmed to
        /// </summary>
        private byte qualityThreshold;

        /// <summary>
        /// Get or set the quality score threshold that reads will be trimmed to
        /// </summary>
        public byte QualityThreshold
        {
            get { return qualityThreshold; }
            set { qualityThreshold = value; }
        }

        /// <summary>
        /// The minimum read length that reads can be trimmed to. 
        /// Anything less will be discarded.
        /// </summary>
        private int minLength;

        /// <summary>
        /// Get or set the minimum read length that reads can be trimmed to.
        /// </summary>
        public int MinLength 
        {
            get { return minLength; }
            set { minLength = value; }
        }

        /// <summary>
        /// Default minimum read length that reads can be trimmed to.
        /// </summary>
        public static int MIN_LENGTH_DEFAULT = 1;

        #region Constructor

        /// <summary>
        /// Trim sequences based on quality
        /// 
        /// NOTE:
        /// Currently, the application assumes the FASTQ format is SANGER
        /// (Illumina 1.8+ is adopting Sanger format anyways). Thus, the
        /// Phred quality score threshold must be within 0 and 93.
        /// </summary>
        /// <param name="parser">Input sequences parser</param>
        /// <param name="filtered">Output sequences formatter</param>
        /// <param name="discarded">Discarded sequences formatter</param>
        /// <param name="q">Sanger Phred-based quality score threshold</param>
        /// <param name="minLength">Minimum trim length</param>
        /// <param name="fromStart">Indicates whether trimming from the start of the read is permitted</param>
        public TrimByQuality(ISequenceParser parser, ISequenceFormatter filtered, ISequenceFormatter discarded, byte q, bool fromStart, int minLength = 1)
            : base(parser, filtered, discarded, fromStart)
        {
            if (q < 0 || q > QualitativeSequence.SangerMaxQualScore - QualitativeSequence.SangerMinQualScore)
                throw new ArgumentOutOfRangeException("Invalid Phred-based quality score threshold.");

            if (minLength < 0)
                throw new ArgumentOutOfRangeException("Minimum length cannot be less than zero.");

            if (q < 0)
                throw new ArgumentOutOfRangeException("Quality score threshold must be greater than zero.");

            this.QualityThreshold = q;
            this.MinLength = minLength;
        }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public TrimByQuality()
            : base()
        {
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Implement the Maximum Contiguous Subsequence Sum alogirthm.
        /// Source: http://www.cs.ucf.edu/~reinhard/classes/cop3503/lectures/AlgAnalysis04.pdf
        /// Maximum Subarray Problem: http://en.wikipedia.org/wiki/Maximum_subarray_problem
        /// 
        /// Find the subsequence with the maximum sum of differences between the actual
        /// base quality score and the given cutoff score.
        /// </summary>
        /// <param name="seqObj">The sequence object to be trimmed</param>
        /// <returns>A new sequence object with trimmed sequence. Or null if a maximum subsequence
        /// cannot be found (i.e. quality scores are below the cutoff)</returns>
        public override ISequence Trim(ISequence seqObj)
        {
            byte[] scores = ((QualitativeSequence)seqObj).QualityScores.ToArray();
            scores = QualityScoreAnalyzer.ConvertToPhred(scores, ((QualitativeSequence)seqObj).FormatType);

            // Implement maximum sum segment algorithm.
            int start = 0;
            int sum = 0;
            int maxSum = 0;
            int maxStart = 0;
            int maxEnd = -1;

            for (int i = 0; i < scores.Length; i++)
            {
                sum += scores[i] - QualityThreshold;

                // If sum is negative, the new subsequence resets from the next position.
                if (sum < 0 && TrimFromStart)
                {
                    start = i + 1;
                    sum = 0;        // reset the sum to start the new subsequence
                }

                if (sum > maxSum)
                {
                    maxSum = sum;
                    maxStart = start;
                    maxEnd = i;
                }
            }

            // Return null if a maximum subsequence cannot be found
            if (maxStart > maxEnd)
                return null;

            var newLength = maxEnd - maxStart + 1;
            // Also return null if the new trim length is less than the required minimum length
            if (newLength < MinLength)
                return null;
            
            var newSeqObj = seqObj.GetSubSequence(maxStart, newLength);
            newSeqObj.ID = seqObj.ID;
            return newSeqObj;
        }

        #endregion
    }
}
