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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bio;
using Bio.IO;
using ShoNS.Array;
using SeqcosApp.Properties;

namespace SeqcosApp.Analyzer
{
    /// <summary>
    /// Perform base quality-level QC
    /// </summary>
    public class QualityScoreAnalyzer : QcAnalyzer
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
        /// Store the filename for writing FileStream contents
        /// </summary>
        //public string StreamFileName { get; private set; }

        /// <summary>
        /// Create a memory stream as a backing store for base quality data
        /// </summary>
        public Stream MemStream { get; private set; }

        /// <summary>
        /// An array of mean quality scores per read
        /// </summary>
        public DoubleArray QualityScoreBySequenceMeans { get; private set; }

        /// <summary>
        /// Minimum base quality score
        /// </summary>
        public double BaseQualityScoreMin { get; private set; }

        /// <summary>
        /// Maximum base quality score
        /// </summary>
        public double BaseQualityScoreMax { get; private set; }

        /// <summary>
        /// Mean base quality score
        /// </summary>
        public double BaseQualityScoreMean { get; private set; }

        /// <summary>
        /// Minimum mean quality score
        /// </summary>
        public double ReadQualityScoreMin { get; private set; }

        /// <summary>
        /// Maximum mean quality score
        /// </summary>
        public double ReadQualityScoreMax { get; private set; }

        /// <summary>
        /// Mean of mean quality scores
        /// </summary>
        public double ReadQualityScoreMean { get; private set; }

        /// <summary>
        /// Standard deviation of mean quality scores
        /// </summary>
        public double ReadQualityScoreStd { get; private set; }

        public FastQFormatType FormatType { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for performing quality score-level QC
        /// </summary>
        /// <param name="sequences">Sequence parser</param>
        /// <param name="readLengthMax">Maximum read length</param>
        /// <param name="count">Total number of reads</param>
        /// <param name="format">FastQ Format Type.</param>
        /// <param name="filename">input filename</param>
        public QualityScoreAnalyzer(ISequenceParser sequences, long readLengthMax, long count, FastQFormatType format, string filename)
            : base(sequences, filename)
        {
            Initialize(format, readLengthMax, count);
        }

        /// <summary>
        /// Constructor when called from GUI. This is the standard constructor used when SequenceAnalyzer is called before this and 
        /// has already calculated readLengthMax and count.
        /// </summary>
        /// <param name="sequences">Sequence parser</param>
        /// <param name="readLengthMax">Maximum read length</param>
        /// <param name="count">Total number of reads</param>
        /// <param name="format">FastQ Format Type</param>
        /// <param name="filename">input filename</param>
        /// <param name="worker">Background worker</param>
        /// <param name="e">Background Worker event args</param>
        public QualityScoreAnalyzer(ISequenceParser sequences, long readLengthMax, long count, FastQFormatType format, string filename, BackgroundWorker worker, DoWorkEventArgs e)
            : base(sequences, filename, worker, e)
        {
            Initialize(format, readLengthMax, count);
        }

        /// <summary>
        /// Alternative constructor for performing quality score-level QC when 
        /// read length max and count is not available and needs to be determined.
        /// </summary>
        /// <param name="sequences">Sequence parser</param>
        /// <param name="format">FastQ Format Type.</param>
        /// <param name="filename">the input filename</param>
        public QualityScoreAnalyzer(ISequenceParser sequences, FastQFormatType format, string filename) : base(sequences, filename)
        {
            List<long> lengths = this.DetermineReadLengths();
            this.Count = lengths.Count;
            Initialize(format);
        }

        /// <summary>
        /// Constructor when called from GUI. Used when read length max and count is not available and needs to be determined.
        /// </summary>
        /// <param name="sequences">Sequence parser</param>
        /// <param name="format">FastQ Format Type.</param>
        /// <param name="filename">the input filename</param>
        /// <param name="worker">Background worker</param>
        /// <param name="e">Background Worker event args</param>
        public QualityScoreAnalyzer(ISequenceParser sequences, FastQFormatType format, string filename, BackgroundWorker worker, DoWorkEventArgs e)
            : base(sequences, filename, worker, e)
        {
            List<long> lengths = this.DetermineReadLengths();
            this.Count = lengths.Count;
            Initialize(format);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Iterate through each sequence and for each position, record the number of occurrences
        /// of each base quality score.
        /// </summary>
        public override void ContentByPosition()
        {
            if (Sequences == null)
                throw new ArgumentNullException("Sequences");

            // Open a MemoryStream to store the quality scores.
            // We are bulding the MemStream such that each consecutive block
            // of size this.Count contains all quality scores at position i, where i = [1, this.Count].
            // This way, we can extract each block and calculate statistics for each base position
            // (as needed in order to generate a boxplot)
            // e.g.
            // i.e. Let qx,y be the quality score q at position y of sequence x
            // then the stream is represented as follows:
            // [q1,1][q2,1][q3,1]....[q1,(length(sequence 1))][q1,2][q2,2]...etc...[q(total # of reads),(length(sequence x))]
            // <------- position 1 qualities ----------------><--- pos 2 qualities ---><etc.>
            //using (Stream s = new FileStream(this.StreamFileName, FileMode.Create))
            //{
            this.MemStream = new MemoryStream((int)(this.ReadLengthMax * this.Count));
            if (MemStream.CanWrite)
            {
                // Keep track of the number of reads processed
                int readNumber = 0;
                // Keep track of the number of bases processed (for calculating mean)
                int bases = 0;

                byte[] bytesToWrite = new byte[this.ReadLengthMax];

                foreach (QualitativeSequence seqObj in Sequences)
                {
                    if (Worker != null && WorkerArgs != null && Worker.CancellationPending)
                    {
                        WorkerArgs.Cancel = true;
                        break;
                    }

                    MemStream.Position = (int)readNumber;

                    // Copy quality scores to a byte array.
                    byte[] quals = ConvertToPhred(((QualitativeSequence)seqObj).QualityScores.ToArray(), this.FormatType);

                    for (int i = 0; i < this.ReadLengthMax; i++)
                    {
                        if (Worker != null && WorkerArgs != null && Worker.CancellationPending)
                        {
                            WorkerArgs.Cancel = true;
                            break;
                        }

                        // If the read is shorter than the max read length, fill
                        // the remaining space with zeroes.
                        byte qual = (i < quals.Length) ? quals[i] : (byte)0;
                        MemStream.WriteByte(qual);

                        var qualDouble = Convert.ToDouble(qual);

                        // Determine minimum base quality score
                        if (qualDouble < this.BaseQualityScoreMin)
                            this.BaseQualityScoreMin = qualDouble;

                        // Determine maximum base quality score
                        if (qualDouble > this.BaseQualityScoreMax)
                            this.BaseQualityScoreMax = qualDouble;

                        // Tally the sum for calculating the mean afterwards
                        this.BaseQualityScoreMean += qualDouble;

                        if (i != this.ReadLengthMax - 1)
                        {
                            // advance cursor to the next position by moving down x positions, 
                            // where x is the total number of sequences
                            if (MemStream.CanSeek)
                            {
                                MemStream.Seek(this.Count - 1, SeekOrigin.Current);
                            }
                            else
                            {
                                throw new NotSupportedException("Unable to write to FileStream");
                            }
                        }

                        bases++;
                    }

                    readNumber++;
                }

                this.BaseQualityScoreMean /= bases;
            }
            //}

            this.HasRunContentByPosition = true;
        }

        /// <summary>
        /// Iterate through each sequence and calculate the average quality score
        /// </summary>
        /// <param name="sequences">List of sequences</param>
        public override void ContentBySequence()
        {
            if (Sequences == null)
                throw new ArgumentNullException("Sequences");

            this.ReadQualityScoreMin = double.PositiveInfinity;
            this.ReadQualityScoreMax = double.NegativeInfinity;
            this.ReadQualityScoreMean = 0;

            //int i = 0;
            //foreach (var seqObj in sequences)
            Parallel.ForEach(Sequences, (seqObj, state, i) =>
            {
                if (Worker != null && WorkerArgs != null && Worker.CancellationPending)
                {
                    WorkerArgs.Cancel = true;
                    state.Stop();
                }

                QualitativeSequence qSeqObj = (QualitativeSequence)seqObj;
                var qualityScoresAry = ConvertToPhred((qSeqObj).QualityScores.ToArray(), this.FormatType);

                QualityScoreBySequenceMeans[(int)i] = QualityScoreAnalyzer.GetMeanFromBytes(qualityScoresAry);

                //i++;
            }
            );

            // get the min/max/average read quality scores
            this.ReadQualityScoreMin = QualityScoreBySequenceMeans.Min();
            this.ReadQualityScoreMax = QualityScoreBySequenceMeans.Max();
            this.ReadQualityScoreMean = QualityScoreBySequenceMeans.Average();
            this.ReadQualityScoreStd = QualityScoreBySequenceMeans.Std();

            this.HasRunContentBySequence = true;
        }

        /// <summary>
        /// Calculate mean from a byte array
        /// </summary>
        /// <param name="values">Array of byte values</param>
        /// <returns>Mean of values</returns>
        public static double GetMeanFromBytes(byte[] values)
        {
            double mean = 0;

            foreach (var element in values)
            {
                mean += element;
            }

            return mean / values.Length;
        }

        /// <summary>
        /// Convert a FASTQ raw quality score to Phred
        /// </summary>
        /// <param name="value">Quality score in ASCII decimal</param>
        /// <param name="format">Original format of the quality score</param>
        /// <returns>Phred-scaled quality score</returns>
        public static byte ConvertToPhred(byte value, FastQFormatType format)
        {
            byte result = value;

            switch (format)
            {
                case FastQFormatType.Solexa:
                    if (!IsBetween(value, QualitativeSequence.SolexaMinQualScore, QualitativeSequence.SolexaMaxQualScore))
                        throw new ArgumentOutOfRangeException("value", Resource.QualityScoreOutOfRange);
                    result = (byte)(value - QualitativeSequence.SolexaMinQualScore);
                    break;

                case FastQFormatType.Sanger:
                    if (!IsBetween(value, QualitativeSequence.SangerMinQualScore, QualitativeSequence.SangerMaxQualScore))
                        throw new ArgumentOutOfRangeException("value", Resource.QualityScoreOutOfRange);
                    result = (byte)(value - QualitativeSequence.SangerMinQualScore);
                    break;

                default:
                    if (!IsBetween(value, QualitativeSequence.IlluminaMinQualScore, QualitativeSequence.IlluminaMaxQualScore))
                        throw new ArgumentOutOfRangeException("value", Resource.QualityScoreOutOfRange);
                    result = (byte)(value - QualitativeSequence.IlluminaMinQualScore);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Determines whether value is between two numbers
        /// </summary>
        /// <param name="value">The value to be checked</param>
        /// <param name="min">The minimum</param>
        /// <param name="max">The maximum</param>
        /// <returns>True if value is between min and max</returns>
        private static bool IsBetween(byte value, byte min, byte max)
        {
            if (value >= min && value <= max)
                return true;
            return false;
        }

        /// <summary>
        /// Convert a FASTQ raw quality score to Phred
        /// </summary>
        /// <param name="value">Array of quality scores in ASCII decimal</param>
        /// <param name="format">Original format of the quality score</param>
        /// <returns>Phred-scaled quality score</returns>
        public static byte[] ConvertToPhred(byte[] values, FastQFormatType format)
        {
            byte[] result = new byte[values.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ConvertToPhred(values[i], format);
            }
            return result;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Set up the instance variables
        /// </summary>
        /// <param name="format">FastQ Format Type</param>
        private void Initialize(FastQFormatType format)
        {
            if (this.Count == 0)
                throw new ArgumentOutOfRangeException("Zero sequence records were processed");

            /**
            // set the filename for the FileStream
            if (this.Prefix != "")
            {
                // include output directory
                this.StreamFileName = this.Prefix + "/" + this.Prefix + ".quals.dat";
            }
            else
            {
                this.StreamFileName = "quals.dat";
            }
             **/

            this.FormatType = format;
            this.QualityScoreBySequenceMeans = new DoubleArray((int)this.Count);

            this.BaseQualityScoreMin = Double.PositiveInfinity;
            this.BaseQualityScoreMax = Double.NegativeInfinity;
            this.BaseQualityScoreMean = 0;
        }

        /// <summary>
        /// Set up instance variables, but with read length max and count already known.
        /// </summary>
        /// <param name="format">FastQ Format Type</param>
        /// <param name="readLengthMax">Maximum read length</param>
        /// <param name="count">Total number of reads</param>
        private void Initialize(FastQFormatType format, long readLengthMax, long count)
        {
            if (readLengthMax <= 0)
                throw new ArgumentOutOfRangeException("Negative or zero read length entered.");

            this.ReadLengthMax = readLengthMax;
            this.Count = count;

            Initialize(format);
        }

        #endregion
    }
}