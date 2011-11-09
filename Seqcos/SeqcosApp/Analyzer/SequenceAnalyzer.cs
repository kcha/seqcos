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
using System.Linq;
using System.Threading.Tasks;
using Bio;
using Bio.IO;
using ShoNS.Array;

namespace SeqcosApp.Analyzer
{
    /// <summary>
    /// Perform sequence-level QC
    /// </summary>
    public class SequenceAnalyzer : QcAnalyzer
    {
        #region Member Variables

        /// <summary>
        /// Maximum read length found in the input data set
        /// </summary>
        public override long ReadLengthMax { get; protected set; }

        /// <summary>
        /// Total number of reads 
        /// </summary>
        public override long Count { get; protected set; }

        /// <summary>
        /// List of read lengths found in the input data set
        /// </summary>
        public IntArray ReadLengths { get; private set; }

        /// <summary>
        /// A table storing symbol counts per base position
        /// </summary>
        public Dictionary<byte, int[]> SymbolCountByPositionTable { get; private set; }

        /// <summary>
        /// An array storing SequenceStatistics objects per read
        /// </summary>
        public SequenceStatistics[] SymbolCountBySequenceArray { get; private set; }       

        /// <summary>
        /// The sequence alphabet of the input data set
        /// </summary>
        public IAlphabet Alphabet { get; private set; }

        /// <summary>
        /// An array storing the GC content at each base position
        /// </summary>
        public DoubleArray GCContentByPositionArray { get; private set; }

        /// <summary>
        /// An array storing the GC content per read
        /// </summary>
        public DoubleArray GCContentBySequenceArray { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for performing sequence-level QC.
        /// </summary>
        /// <param name="selectedParser">ISequence object</param>
        /// <param name="filename">Prefix of input filename</param>
        public SequenceAnalyzer(ISequenceParser selectedParser, string prefix) : base(selectedParser, prefix)
        {
            Initialize();
        }

        /// <summary>
        /// Constructor when called from GUI for performing sequence-level QC.
        /// </summary>
        /// <param name="selectedParser">ISequence object</param>
        /// <param name="filename">Prefix of input filename</param>
        /// <param name="count">Total number of reads</param>
        /// <param name="filename">Prefix of input filename</param>
        public SequenceAnalyzer(ISequenceParser selectedParser, string prefix, BackgroundWorker worker, DoWorkEventArgs e) : base(selectedParser, prefix, worker, e)
        {
            Initialize();
        }

        /// <summary>
        /// Called from constructor to initialize instance values
        /// </summary>
        private void Initialize()
        {
            // Determine the read lengths of the input data and store maximum read length
            this.ReadLengths = IntArray.From(this.DetermineReadLengths());
            this.Count = this.ReadLengths.Count;

            if (this.Count == 0)
                throw new ArgumentOutOfRangeException("Zero sequence records were processed");

            this.SymbolCountByPositionTable = new Dictionary<byte, int[]>();

            // Determine the alphabet of this input file
            this.Alphabet = this.Sequences.FirstOrDefault().Alphabet;

            this.GCContentByPositionArray = new DoubleArray((int)ReadLengthMax);
            this.GCContentBySequenceArray = new DoubleArray((int)Count);
        }

        #endregion

        #region Public Methods

        public override void Process()
        {
            base.Process();
            GCContentByPosition();
        }

        /// <summary>
        /// Iterate through each sequence and for each position, record the number of occurrences
        /// of each base symbol
        /// </summary>
        public override void ContentByPosition()
        {
            if (this.Sequences == null)
                throw new ArgumentNullException("Sequences");

            foreach (var seqObj in this.Sequences)
            {
                if (Worker != null && WorkerArgs != null && Worker.CancellationPending)
                {
                    WorkerArgs.Cancel = true;
                    break;
                }

                int p = 0;

                // iterate through each position in sequence string
                foreach (var symbol in seqObj)
                {
                    int[] positions;

                    // check if SymbolCountByPositionTable has this symbol initialized
                    if (SymbolCountByPositionTable.TryGetValue(symbol, out positions))
                    {
                        positions[p]++;
                        SymbolCountByPositionTable[symbol] = positions;
                    }
                    else
                    {
                        // initialize a new List for this symbol
                        SymbolCountByPositionTable.Add(symbol, new int[this.ReadLengthMax]);
                        SymbolCountByPositionTable[symbol][p] = 1;
                    }

                    p++;
                }
            }
            HasRunContentByPosition = true;
        }

        /// <summary>
        /// Iterate through each sequence and calculate the symbol content.
        /// </summary>
        public override void ContentBySequence()
        {
            if (this.Sequences == null)
                throw new ArgumentNullException("Sequences");

            //int i = 0;
            //foreach (var seqObj in this.sequences)
            Parallel.ForEach(this.Sequences, (seqObj, state, i) =>
            {
                if (Worker != null && WorkerArgs != null && Worker.CancellationPending)
                {
                    WorkerArgs.Cancel = true;
                    state.Stop();
                }

                // Instead of storing all SequenceStatistics objects, just calculate the required GC content values here.
                SequenceStatistics seqStats = new SequenceStatistics(seqObj);
                double gcPercent = GCContentBySequence(seqStats, seqObj.Count);
                GCContentBySequenceArray[(int)i] = gcPercent;

                //i++;
            }
            );
            HasRunContentBySequence = true;
        }

        /// <summary>
        /// Calculate GC content at each position
        /// </summary>
        /// <returns>Array of GC content as a decimal</return>
        public void GCContentByPosition()
        {
            if (this.Sequences == null)
                throw new ArgumentNullException("sequences");

            if (!HasRunContentByPosition)
                ContentByPosition();

            //double[] gcByPosition = new double[this.ReadLengthMax];

            for (int i = 0; i < this.ReadLengthMax; i++)
            {
                if (Worker != null && WorkerArgs != null && Worker.CancellationPending)
                {
                    WorkerArgs.Cancel = true;
                    break;
                }

                int totalReadsAtThisPosition = CalculateTotalReadsAtPosition(i);
                                
                // C - 67, G - 71
                GCContentByPositionArray[i] = 100*(double)(GetNumReadsAtPositionWithSymbol(67, i) + GetNumReadsAtPositionWithSymbol(71, i))
                                        / totalReadsAtThisPosition;
            }

            //return gcByPosition;
        }

        /// <summary>
        /// Calculate GC content for a read
        /// </summary>
        /// <param name="seqStats">SequenceStatistics object</param>
        /// <param name="length">The length of the read</param>
        /// <returns>GC percentage of a read</returns>
        public double GCContentBySequence(SequenceStatistics seqStats, long length)
        {
            if (seqStats == null)
                throw new ArgumentNullException("seqStats");

            return 100 * (double)(seqStats.GetCount('G') + seqStats.GetCount('C') + seqStats.GetCount('S')) / length;
        }

        /// <summary>
        /// Calculate the sum of counts at each defined base position
        /// </summary>
        /// <returns>Array of sums</returns>
        public int[] GetSumByPosition()
        {
            if (!HasRunContentByPosition)
                ContentByPosition();

            int[] sums = new int[this.ReadLengthMax];

            // iterate through each base position
            for (int i = 0; i < this.ReadLengthMax; i++)
            {
                // iterate through each defined symbol in the table
                foreach (var key in this.SymbolCountByPositionTable.Keys)
                {
                    sums[i] += this.SymbolCountByPositionTable[key][i];
                }
            }

            return sums;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Helper method for retrieving the number of reads with symbol at position.
        /// </summary>
        /// <param name="symbol">The symbol of interest</param>
        /// <param name="position">Position in read sequence</param>
        /// <returns>Number of reads</returns>
        private int GetNumReadsAtPositionWithSymbol(byte symbol, int position)
        {
            int[] positions;

            if (SymbolCountByPositionTable.TryGetValue(symbol, out positions))
            {
                return positions[position];
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculate number of reads at a given position.
        /// NB: This method is used in case we have variable read lengths in the dataset.
        /// </summary>
        /// <param name="i">position within the read</param>
        /// <returns>Total number of reads at position</returns>
        private int CalculateTotalReadsAtPosition(int i)
        {
            int total = 0;
            List<byte> symbols = SymbolCountByPositionTable.Keys.ToList();

            foreach (var symbol in symbols)
            {
                total += SymbolCountByPositionTable[symbol][i];
            }

            return total;
        }
        #endregion
    }
}