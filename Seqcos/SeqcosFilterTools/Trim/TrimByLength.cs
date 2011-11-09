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
using SeqcosApp;
using Bio;
using Bio.IO;

namespace SeqcosFilterTools.Trim
{
    /// <summary>
    /// Given a FASTQ/FASTA file, trim each read to size s, where s is less
    /// than the original read length.
    /// 
    /// Reads may also be trimmed based on
    /// percentage, in which s needs to be a decimal between 0 and 1. Any
    /// other decimal greater than 1 will be rounded to the nearest integer.
    /// </summary>
    public class TrimByLength : Trimmer
    {
        #region Member variables

        /// <summary>
        /// Store original trim length as specified by user
        /// </summary>
        private double trimLength;

        /// <summary>
        /// Format the trim length before returning it to other classes.
        /// </summary>
        public double TrimLength
        {
            get
            {
                // Return the integer size if > 1
                if (trimLength > 1)
                    return Math.Round(trimLength);
                // Otherwise return the decimal if between 0 and 1
                else
                    return trimLength;
            }
            set
            {
                trimLength = value;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for trimming sequences based on length or percentage
        /// </summary>
        /// <param name="parser">Input sequences parser</param>
        /// <param name="filtered">Output sequences formatter</param>
        /// <param name="discarded">Discarded sequences formatter</param>
        /// <param name="newLength">If > 1, this is the minimum length (rounded to the nearest integer) 
        /// sequences will be trimmed at. If between 0 and 1, this will be treated as 
        /// a percentage and reads are trimmed based on this amount (i.e. if newLength = 0.5, 50% 
        /// of the read will be trimmed.</param>
        /// <param name="fromStart">Trim from the start of the read</param>
        public TrimByLength(ISequenceParser parser, ISequenceFormatter filtered, ISequenceFormatter discarded, double newLength, bool fromStart) 
            : base(parser, filtered, discarded, fromStart)
        {
            if (newLength <= 0)
                throw new ArgumentOutOfRangeException("Trim length cannot be less than zero.");

            this.TrimLength = newLength;
        }

        /// <summary>
        /// This is an empty constructor. Used for data binding in WPF.
        /// </summary>
        public TrimByLength() : base()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Trim a sequence object.
        /// </summary>
        /// <param name="seqObj">The sequence object to be trimmed</param>
        /// <returns>A new sequence object with trimmed sequence. Or null if the
        /// sequence length is less than the trim length.</returns>
        public override ISequence Trim(ISequence seqObj)
        {
            long start, end;

            // Return null if the sequence length is less than the trim length
            if (seqObj.Count < TrimLength)
                return null;

            if (TrimFromStart)
            {
                end = seqObj.Count;

                if (IsAFraction(TrimLength))
                    start = end - (long)(Convert.ToDouble(seqObj.Count) * TrimLength);
                else
                    start = end - (long)TrimLength;
            }
            else
            {
                start = 0;

                if (IsAFraction(TrimLength))
                    end = (long)(Convert.ToDouble(seqObj.Count) * TrimLength);
                else
                    end = (long)TrimLength;
            }

            var newSeqObj = seqObj.GetSubSequence(start, end - start);
            newSeqObj.ID = seqObj.ID; 

            return newSeqObj;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Check whether this formatAsString is between 0 and 1
        /// </summary>
        /// <param name="formatAsString">The formatAsString to be tested</param>
        /// <returns>True if it is between 0 and 1. Otherwise false.</returns>
        private bool IsAFraction(double value)
        {
            if (value > 0 && value < 1)
                return true;

            return false;
        }

        #endregion
    }
}