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
using System.Text.RegularExpressions;
using Bio;
using Bio.IO;

namespace SeqcosFilterTools.Discard
{
    /// <summary>
    /// Discards a read if it matches a given regular expression pattern
    /// </summary>
    public class DiscardByRegex : Discarder
    {
        /// <summary>
        /// Regular expression pattern
        /// </summary>
        private Regex pattern;

        /// <summary>
        /// Gets or sets the regular expression pattern
        /// </summary>
        public string Pattern
        {
            get 
            {
                if (pattern == null)
                    return string.Empty;

                return pattern.ToString();
            }
            set
            {
                try
                {
                    pattern = new Regex(value);
                }
                catch (ArgumentException ex)
                {
                    throw ex;
                }
            }
        }

        #region Constructor

        /// <summary>
        /// Constructor for discarding reads based on regex pattern
        /// </summary>
        /// <param name="parser">Input sequences parser</param>
        /// <param name="filtered">Output sequences formatter</param>
        /// <param name="discarded">Discarded sequences formatter</param>
        /// <param name="regexPattern">Regular expression pattern</param>
        public DiscardByRegex(ISequenceParser parser, ISequenceFormatter filtered, ISequenceFormatter discarded, string regexPattern)
            : base(parser, filtered, discarded)
        {
            Pattern = regexPattern;
        }

        /// <summary>
        /// This is an empty constructor. Used for data binding in WPF.
        /// </summary>
        public DiscardByRegex() : base() { }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether a read has a match with the specified regular expression
        /// and should be discarded.
        /// </summary>
        /// <param name="seqObj">A sequence object</param>
        /// <returns>True if it does not meet the minimum mean and thus
        /// will be discarded. False, otherwise.</returns>
        public override bool CanDiscard(ISequence seqObj)
        {
            string sequence = new string(seqObj.Select(b => (char)b).ToArray());

            return pattern.IsMatch(sequence);
        }

        #endregion
    }
}
