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

namespace SeqcosFilterTools.Trim
{
    /// <summary>
    /// Trims a read based on a given regular expression pattern.
    /// </summary>
    public class TrimByRegex : Trimmer
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
        /// Constructor for trimming reads based on regex pattern
        /// </summary>
        /// <param name="parser">Input sequences parser</param>
        /// <param name="filtered">Output sequences formatter</param>
        /// <param name="discarded">Discarded sequences formatter</param>
        /// <param name="regexPattern">Regular expression pattern</param>
        public TrimByRegex(ISequenceParser parser, ISequenceFormatter filtered, ISequenceFormatter discarded, string regexPattern)
            : base(parser, filtered, discarded, false)
        {
            Pattern = regexPattern;
        }

        /// <summary>
        /// This is an empty constructor. Used for data binding in WPF.
        /// </summary>
        public TrimByRegex()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Trim a sequence object based on given regular expression.
        /// i.e. remove the segment that matches with the regular expression.
        /// 
        /// Currently, we are just calling Regex.Replace(), so ANY match
        /// anywhere on the read can be replaced. We do not restrict matching
        /// to boundaries of the read. 
        /// </summary>
        /// <param name="seqObj"></param>
        /// <returns></returns>
        public override ISequence Trim(ISequence seqObj)
        {
            StringBuilder newSequence = new StringBuilder();
            StringBuilder newQualityScores = new StringBuilder();
            List<long> breakpoints = new List<long>() {0, seqObj.Count};
            long newLength = 0;

            string sequence = new string(seqObj.Select(b => (char)b).ToArray());

            MatchCollection matches = pattern.Matches(sequence);

            // Exit if there are no matches
            if (matches.Count == 0) { return null; }

            // Generate a list of 'breakpoints' (caused by regex matches) in the original sequence string
            foreach (Match match in matches)
            {
                long start = match.Index;
                long end = match.Index + match.Length;
                newLength += match.Length;

                // Sanity check
                if (end > seqObj.Count)
                    throw new ArgumentOutOfRangeException("End segment position is larger than sequence count");

                breakpoints.Add(start);
                breakpoints.Add(end);
            }

            // Sort the segment list
            breakpoints.Sort();

            // Go through each adjacent 'pair' of breakpoints in the list and
            // generate new sub-Sequence objects
            for (int i = 0; i < breakpoints.Count; i = i + 2)
            {
                long start = breakpoints[i];
                long end = breakpoints[i + 1];

                // skip to next pair if the start and end are the same.
                if (start == end) { continue; }

                var subSeqObj = seqObj.GetSubSequence(start, end - start);

                string subSequence = new string(subSeqObj.Select(b => (char)b).ToArray());

                newSequence.Append(subSequence);

                if (seqObj is QualitativeSequence)
                {
                    string scores = new string((subSeqObj as QualitativeSequence).QualityScores.Select(b => (char)b).ToArray());
                    newQualityScores.Append(scores);
                }
            }


            ISequence newSeqObj = null;

            if (seqObj is QualitativeSequence)
                newSeqObj = new QualitativeSequence(seqObj.Alphabet, (seqObj as QualitativeSequence).FormatType, newSequence.ToString(), newQualityScores.ToString());
            else
                newSeqObj = new Sequence(seqObj.Alphabet, newSequence.ToString());

            newSeqObj.ID = seqObj.ID;

            return newSeqObj;
        }

        #endregion

    }
}
