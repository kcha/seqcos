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

namespace SeqcosGui
{
    /// <summary>
    /// This abstract class defines the custom event arguments for 
    /// executing the Trim  tool
    /// </summary>
    public abstract class FilterToolArgs : EventArgs
    {
        #region Public members

        /// <summary>
        /// Stores the full path of the output filename
        /// </summary>
        public string OutputFilename { get; protected set; }

        /// <summary>
        /// Holds information about the input file and parser type
        /// </summary>
        public InputSubmission InputInfo { get; protected set; }

        #endregion

        /// <summary>
        /// Base constructor for filter tool event arguments
        /// </summary>
        /// <param name="input">Input information</param>
        /// <param name="outFile">Output filename</param>
        public FilterToolArgs(InputSubmission input, string outFile)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (outFile == null)
                throw new ArgumentNullException("outFile");

            this.InputInfo = input;
            this.OutputFilename = outFile;
        }
    }
}
