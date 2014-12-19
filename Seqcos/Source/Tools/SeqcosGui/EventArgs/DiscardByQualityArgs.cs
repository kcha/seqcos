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

using Bio.IO;
using SeqcosApp;
using SeqcosFilterTools.Discard;

namespace SeqcosGui
{
    /// <summary>
    /// Event arguments for discarding reads based on mean quality score
    /// </summary>
    public class DiscardByMeanQualityArgs : FilterToolArgs
    {
        /// <summary>
        /// Discarder application object
        /// </summary>
        public DiscardByMeanQuality discarder { get; private set; }

        /// <summary>
        /// Constructor for DiscardByMeanQuality Event Args
        /// </summary>
        /// <param name="input">Input information</param>        
        /// <param name="filtered">Output sequence formatter</param>
        /// <param name="discarded">Discarded reads sequence formatter</param>
        /// <param name="mean">Minimum mean quality threshold</param>
        /// <param name="outFile">Output filename</param>
        public DiscardByMeanQualityArgs(InputSubmission input, ISequenceFormatter filtered, ISequenceFormatter discarded, byte mean, string outFile)
            : base(input, outFile)
        {
            discarder = new DiscardByMeanQuality(input.Parser, filtered, discarded, mean);
        }
    }
}
