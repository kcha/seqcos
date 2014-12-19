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
using SeqcosFilterTools.Discard;
using Bio.IO;
using SeqcosApp;

namespace SeqcosGui
{
    /// <summary>
    /// Event arguments for discarding reads based on length
    /// </summary>
    public class DiscardByLengthArgs : FilterToolArgs
    {
        /// <summary>
        /// Discarder application object
        /// </summary>
        public DiscardByLength discarder { get; private set; }

        /// <summary>
        /// Constructor for DiscardByLength Event Args
        /// </summary>
        /// <param name="input">Input information</param>
        /// <param name="filtered">Output sequence formatter</param>
        /// <param name="discarded">Discarded reads sequence formatter</param>
        /// <param name="length">Length threshold for discarding reads</param>
        /// <param name="outFile">Output filename</param>
        public DiscardByLengthArgs(InputSubmission input, ISequenceFormatter filtered, ISequenceFormatter discarded, long length, string outFile)
            : base(input, outFile)
        {
            discarder = new DiscardByLength(input.Parser, filtered, discarded, length);
        }
    }
}
