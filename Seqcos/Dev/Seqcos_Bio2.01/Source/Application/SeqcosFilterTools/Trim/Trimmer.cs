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
using Bio;
using Bio.IO;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SeqcosFilterTools.Trim
{
    /// <summary>
    /// This abstract class implements sequence trimming functions. 
    /// The specific trimming functionality is implemented by derived classes.
    /// </summary>
    abstract public class Trimmer
    {
        #region Members
        protected IEnumerable<ISequence> Sequences;
        protected ISequenceFormatter FilteredWriter;
        protected ISequenceFormatter DiscardedWriter;
        public int Counted { get; protected set; }
        public int TrimCount { get; protected set; }
        public int DiscardCount { get; protected set; }
        public bool TrimFromStart { get; protected set; }

        /// <summary>
        /// Background worker thread for GUI
        /// </summary>
        public BackgroundWorker Worker = null;

        /// <summary>
        /// Background worker arguments for GUI
        /// </summary>
        public DoWorkEventArgs WorkerArgs = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Base constructor for trimming sequences
        /// </summary>
        /// <param name="parser">SequenceParser for input data</param>
        /// <param name="filtered">SequenceFormatter for output data</param>
        /// <param name="discarded">SequenceParser for discarded data</param>
        /// <param name="fromLeft">Trim from the start of the read</param>
        public Trimmer(ISequenceParser parser, ISequenceFormatter filtered, ISequenceFormatter discarded, bool fromLeft)
        {
            if (parser == null)
                throw new ArgumentNullException("parser");
            if (filtered == null)
                throw new ArgumentNullException("filtered");

            this.Sequences = parser.Parse();
            this.FilteredWriter = filtered;
            this.DiscardedWriter = discarded;
            this.Counted = 0;
            this.TrimCount = 0;
            this.DiscardCount = 0;
            this.TrimFromStart = fromLeft;
        }

        /// <summary>
        /// This is an empty constructor. Used for data binding in WPF.
        /// </summary>
        public Trimmer()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Trim all sequences
        /// </summary>
        public void TrimAll()
        {
            //foreach (var seqObj in Sequences)
            Parallel.ForEach(this.Sequences, (seqObj) =>
            {
                if (Worker != null && WorkerArgs != null)
                {
                    if (Worker.CancellationPending)
                    {
                        WorkerArgs.Cancel = true;
                        return;
                    }
                }


                // call Trim() to perform specific trimming function
                // and return the new Seq object
                var trimmedSeqObj = Trim(seqObj);

                // write the sequence
                if (trimmedSeqObj != null)
                {
                    lock (FilteredWriter)
                    {
                        FilteredWriter.Write(trimmedSeqObj);
                    }
                    TrimCount++;
                }
                else    // discard the read
                {
                    if (DiscardedWriter != null)
                    {
                        lock (DiscardedWriter)
                        {
                            DiscardedWriter.Write(seqObj);
                        }
                    }
                    DiscardCount++;
                }
                Counted++;
            }
            );
        }

        /// <summary>
        /// Abstract method for trimming reads
        /// </summary>
        public abstract ISequence Trim(ISequence seqObj);

        #endregion
    }
}
