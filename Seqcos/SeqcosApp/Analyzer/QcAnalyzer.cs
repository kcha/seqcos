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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Bio;
using Bio.IO;

namespace SeqcosApp.Analyzer
{
    /// <summary>
    /// This abstract class defines common QC analysis 
    /// properties at both the base position and sequence levels
    /// </summary>
    abstract public class QcAnalyzer
    {
        #region Members
        /// <summary>
        /// The total number of reads
        /// </summary>
        abstract public long Count { get; protected set; }

        /// <summary>
        /// The maximum read length in the dataset
        /// </summary>
        abstract public long ReadLengthMax { get; protected set; }

        /// <summary>
        /// Determines whether ContentByPosition() was executed
        /// </summary>
        public bool HasRunContentByPosition { get; protected set; }

        /// <summary>
        /// Determines whether ContentBySequence() was executed
        /// </summary>
        public bool HasRunContentBySequence { get; protected set; }

        /// <summary>
        /// Determines whether all core analysis steps have been executed
        /// by this instance.
        /// </summary>
        public bool IsReady
        {
            get
            {
                return HasRunContentByPosition && HasRunContentBySequence;
            }
        }

        /// <summary>
        /// Name of the input file
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// Filename filename
        /// </summary>
        public string Prefix
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this.FileName);
            }
        }

        /// <summary>
        /// Enumerable of input sequences
        /// </summary>
        protected IEnumerable<ISequence> Sequences;

        /// <summary>
        /// Background worker thread for GUI
        /// </summary>
        internal BackgroundWorker Worker = null;

        /// <summary>
        /// Background worker arguments for GUI
        /// </summary>
        internal DoWorkEventArgs WorkerArgs = null;

        #endregion

        #region Constructor

        public QcAnalyzer(ISequenceParser parser, string file)
        {
            if (parser == null)
                throw new ArgumentNullException("parser");
            if (file == null)
                throw new ArgumentNullException("file");
            
            this.Sequences = parser.Parse();
            this.FileName = file;

            this.HasRunContentByPosition = false;
            this.HasRunContentBySequence = false;
        }

        public QcAnalyzer(ISequenceParser parser, string prefix, BackgroundWorker worker, DoWorkEventArgs e)
            : this(parser, prefix)
        {
            this.Worker = worker;
            this.WorkerArgs = e;
        }
        
        #endregion

        /// <summary>
        /// Iterate through each sequence and store the read lengths in an array
        /// as well as determine the maximum read length.
        /// </summary>
        /// <returns>List of sequence lengths</returns>
        public virtual List<long> DetermineReadLengths()
        {
            if (this.Sequences == null)
                throw new ArgumentNullException("Sequences");

            List<long> lengths = new List<long>();
            this.ReadLengthMax = 0;

            foreach (var seqObj in this.Sequences)
            {
                // store lengths
                lengths.Add(seqObj.Count);

                // determine the maximum read length
                if (seqObj.Count > this.ReadLengthMax)
                    this.ReadLengthMax = seqObj.Count;
            }

            return lengths;
        }

        /// <summary>
        /// Run all QC operations
        /// </summary>
        public virtual void Process()
        {
            ContentByPosition();
            ContentBySequence();
        }
        

        /// <summary>
        /// Run QC at base position-level
        /// </summary>
        /// <param name="worker">Background working thread (used by GUI)</param>
        /// <param name="e">Worker event arguments</param>
        abstract public void ContentByPosition();

        /// <summary>
        /// Run QC at sequence-level
        /// </summary>
        /// <param name="worker">Background working thread (used by GUI)</param>
        /// <param name="e">Worker event arguments</param>
        abstract public void ContentBySequence();
    }
}