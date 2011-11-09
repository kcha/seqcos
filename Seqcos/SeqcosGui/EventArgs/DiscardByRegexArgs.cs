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
using SeqcosFilterTools.Discard;

namespace SeqcosGui
{
    public class DiscardByRegexArgs : FilterToolArgs
    {
        /// <summary>
        /// Discarder application object
        /// </summary>
        public DiscardByRegex discarder { get; private set; }

        /// <summary>
        /// Constructor for DiscardByRegex Event Args
        /// </summary>
        /// <param name="input">Input information</param>        
        /// <param name="filtered">Output sequence formatter</param>
        /// <param name="discarded">Discarded reads sequence formatter</param>
        /// <param name="pattern">Regular expression pattern</param>
        /// <param name="outFile">Output filename</param>
        public DiscardByRegexArgs(InputSubmission input, ISequenceFormatter filtered, ISequenceFormatter discarded, string pattern, string outFile)
            : base(input, outFile)
        {
            discarder = new DiscardByRegex(input.Parser, filtered, discarded, pattern);
        }


    }
}
