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
    public class TrimByRegexArgs : FilterToolArgs
    {
        public TrimByRegex trimmer { get; private set; }

        public TrimByRegexArgs(InputSubmission input, ISequenceFormatter filtered, ISequenceFormatter discarded, string pattern, string outFile)
            : base(input, outFile)
        {
            trimmer = new TrimByRegex(input.Parser, filtered, discarded, pattern);
        }
    }
}
