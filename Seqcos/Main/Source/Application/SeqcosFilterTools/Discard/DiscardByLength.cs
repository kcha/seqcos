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
using System.Linq;
using System.Text;
using Bio;
using Bio.IO;

namespace SeqcosFilterTools.Discard
{
    /// <summary>
    /// Discards reads if its length is less
    /// than a minimum threshold.
    /// </summary>
    public class DiscardByLength : Discarder
    {
        /// <summary>
        /// Minimum length of reads that are kept from discarding
        /// </summary>
        private long minLengthThreshold;

        /// <summary>
        /// Get or set the minimum read length threshold
        /// </summary>
        public long MinLengthThreshold
        {
            get { return minLengthThreshold; }
            set { minLengthThreshold = value; }
        }


        #region Constructor

        /// <summary>
        /// Constructor for discarding reads based on minimum length.
        /// </summary>
        /// <param name="parser">Input sequence parser</param>
        /// <param name="filtered">Output sequence formatter</param>
        /// <param name="discarded">Output discarded sequences formatter</param>
        /// <param name="length">The minimum length that reads must satisfy in 
        /// order to not be discarded.</param>
        public DiscardByLength(ISequenceParser parser, ISequenceFormatter filtered, ISequenceFormatter discarded, long length)
            : base(parser, filtered, discarded)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException("Minimum length must be > 0.");
            minLengthThreshold = length;
        }

        /// <summary>
        /// This is an empty constructor. Used for data binding in WPF.
        /// </summary>
        public DiscardByLength() : base() { }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether a read satisfies the conditions to be discarded.
        /// i.e. Does it meet the minimum length requirements?
        /// </summary>
        /// <param name="seqObj">A sequence object</param>
        /// <returns>True if it does not meet the minimum length and thus
        /// can be discarded. False, otherwise.</returns>
        public override bool CanDiscard(ISequence seqObj)
        {
            if (seqObj.Count < minLengthThreshold)
                return true;
            return false;
        }

        #endregion
    }
}