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
using SeqcosFilterTools.Trim;


namespace SeqcosGui
{
    /// <summary>
    /// This class defines the custom event arguments for
    /// trimming reads based on length
    /// </summary>
    public class TrimByLengthArgs : FilterToolArgs
    {
        /// <summary>
        /// Trimmer application object
        /// </summary>
        public TrimByLength trimmer { get; private set; }
        
        /// <summary>
        /// Constructor for holding Trim by length event arguments
        /// </summary>
        /// <param name="trimFromStart"></param>
        /// <param name="input"></param>
        /// <param name="filtered"></param>
        /// <param name="discarded"></param>
        public TrimByLengthArgs(double newLength, bool trimFromStart, InputSubmission input, ISequenceFormatter filtered, ISequenceFormatter discarded, string outFile)
            : base(input, outFile)
        {
            trimmer = new TrimByLength(input.Parser, filtered, discarded, newLength, trimFromStart);
        }
    }
}
