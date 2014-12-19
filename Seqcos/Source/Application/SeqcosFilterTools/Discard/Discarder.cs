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
using Bio;
using Bio.IO;
using System.Threading.Tasks;
using System.ComponentModel;

namespace SeqcosFilterTools.Discard
{
    /// <summary>
    /// This abstract class implements sequence discarding functions.
    /// </summary>
    abstract public class Discarder
    {
        #region Members

        protected IEnumerable<ISequence> Sequences;
        protected ISequenceFormatter FilteredWriter;
        protected ISequenceFormatter DiscardedWriter;
        public int Counted { get; set; }
        public int DiscardCount { get; set; }


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
        /// Base constructor for discarding sequences
        /// </summary>
        /// <param name="parser">SequenceParser for input data</param>
        /// <param name="filtered">SequenceFormatter for filtered data</param>
        /// <param name="discarded">SequenceFormatter for discarded data</param>
        public Discarder(ISequenceParser parser, ISequenceFormatter filtered, ISequenceFormatter discarded)
        {
            if (parser == null)
                throw new ArgumentNullException("parser");
            if (filtered == null)
                throw new ArgumentNullException("filtered");

            this.Sequences = parser.Parse();
            this.FilteredWriter = filtered;
            this.DiscardedWriter = discarded;
            this.Counted = 0;
            this.DiscardCount = 0;
        }

        /// <summary>
        /// This is an empty constructor. Used for data binding in WPF.
        /// </summary>
        public Discarder()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// This function takes a set of Sequence objects and determines
        /// which reads should be discarded. The discarding logic is 
        /// implemented by classes that derive this base class.
        /// </summary>
        public void DiscardReads()
        {
            foreach (var seqObj in this.Sequences)
            {
                if (Worker != null && WorkerArgs != null && Worker.CancellationPending)
                {
                    WorkerArgs.Cancel = true;
                    return;
                }

                if (!CanDiscard(seqObj))
                {
                    // Write non-discarded reads
                    lock (this.FilteredWriter)      // Using lock in case we switch to using Parallel.ForEach
                    {
                        this.FilteredWriter.Write(seqObj);
                    }
                }
                else
                {
                    if (this.DiscardedWriter != null)
                    {
                        // Write discarded reads
                        lock (this.DiscardedWriter)
                        {
                            this.DiscardedWriter.Write(seqObj);
                        }
                    }
                    DiscardCount++;
                }
                Counted++;
            }
        }

        /// <summary>
        /// Abstract method for discarding reads
        /// </summary>
        abstract public bool CanDiscard(ISequence seqObj);

        #endregion
    }
}
