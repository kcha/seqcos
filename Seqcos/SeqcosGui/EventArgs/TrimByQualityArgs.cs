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
using Bio.IO;
using SeqcosApp;
using SeqcosFilterTools.Trim;

namespace SeqcosGui
{
    /// <summary>
    /// This class defines the custom event arguments for
    /// trimming reads based on quality score
    /// </summary>
    public class TrimByQualityArgs : FilterToolArgs
    {
        /// <summary>
        /// Trimmer application object
        /// </summary>
        public TrimByQuality trimmer { get; private set; }

        /// <summary>
        /// Constructor for holding Trim By Quality event arguments
        /// </summary>
        /// <param name="q"></param>
        /// <param name="trimFromStart"></param>
        /// <param name="minLength"></param>
        /// <param name="input"></param>
        /// <param name="filtered"></param>
        /// <param name="discarded"></param>
        /// <param name="outFile"></param>
        public TrimByQualityArgs(byte q, bool trimFromStart, int minLength, InputSubmission input, ISequenceFormatter filtered, ISequenceFormatter discarded, string outFile)
            : base(input, outFile)
        {
            trimmer = new TrimByQuality(input.Parser, filtered, discarded, q, trimFromStart, minLength);
        }
    }
}
