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
using System.Linq;
using SeqcosApp.Properties;
using System.IO;
using System.Text;

namespace SeqcosApp
{
    /// <summary>
    /// Container for holding all the read statistics calculated in
    /// the QC analysis. It can also parse a CSV file from a previous
    /// file and hold its results.
    /// </summary>
    public class InputStatistics
    {
        #region Members

        public readonly Filenames myFilenames;

        public readonly long NumberOfReads;

        public readonly long ReadLengthMin;

        public readonly long ReadLengthMax;

        public readonly double ReadLengthMean;

        public readonly double ReadGcContentMin;

        public readonly double ReadGcContentMax;

        public readonly double ReadGcContentMean;

        public readonly double ReadGcContentStd;

        public readonly double BasePhredScoreMin;

        public readonly double BasePhredScoreMax;

        public readonly double BasePhredScoreMean;

        public readonly double ReadPhredScoreMin;

        public readonly double ReadPhredScoreMax;

        public readonly double ReadPhredScoreMean;

        public readonly string FastQFormat;

        #endregion

        /// <summary>
        /// Constructor for holding all read statistics calculated
        /// from the QC analysis.
        /// </summary>
        /// <param name="main"></param>
        public InputStatistics(Seqcos main)
        {
            if (main == null)
                throw new ArgumentNullException("Seqcos");

            this.myFilenames = main.FileList;

            if (main.SequenceQc != null && main.SequenceQc.IsReady)
            {
                this.NumberOfReads = main.SequenceQc.Count;

                this.ReadLengthMin = main.SequenceQc.ReadLengths.Min();
                this.ReadLengthMax = main.SequenceQc.ReadLengths.Max();
                this.ReadLengthMean = main.SequenceQc.ReadLengths.Average();

                this.ReadGcContentMin = main.SequenceQc.GCContentBySequenceArray.Min();
                this.ReadGcContentMax = main.SequenceQc.GCContentBySequenceArray.Max();
                this.ReadGcContentMean = main.SequenceQc.GCContentBySequenceArray.Mean();
                this.ReadGcContentStd = main.SequenceQc.GCContentBySequenceArray.Std();
            }

            if (main.QualityScoreQc != null && main.QualityScoreQc.IsReady)
            {
                if (this.NumberOfReads == 0)
                {
                    this.NumberOfReads = main.QualityScoreQc.Count;
                }

                this.BasePhredScoreMin = main.QualityScoreQc.BaseQualityScoreMin;
                this.BasePhredScoreMax = main.QualityScoreQc.BaseQualityScoreMax;
                this.BasePhredScoreMean = main.QualityScoreQc.BaseQualityScoreMean;

                this.ReadPhredScoreMin = main.QualityScoreQc.ReadQualityScoreMin;
                this.ReadPhredScoreMax = main.QualityScoreQc.ReadQualityScoreMax;
                this.ReadPhredScoreMean = main.QualityScoreQc.ReadQualityScoreMean;

                this.FastQFormat = main.QualityScoreQc.FormatType.ToString();
            }
        }

        /// <summary>
        /// Alternative constructor for populating the read statistics
        /// from a previous run via a supplied CSV file
        /// </summary>
        /// <param name="csvFile">CSV file from a previous run</param>
        public InputStatistics(string csvFile)
        {
            if (!File.Exists(csvFile))
            {
                throw new FileNotFoundException(Resource.FileNotFound);
            }

            using (StreamReader inStream = new StreamReader(csvFile))
            {
                string line;

                while ((line = inStream.ReadLine()) != null)
                {
                    string[] items = line.Split(Resource.Delimiter.ToCharArray());

                    if (items[0].Equals(Resource.CsvNumReads))
                    {
                        NumberOfReads = Convert.ToInt64(items[1]);
                    }

                    else if (items[0].Equals(Resource.CsvReadLength))
                    {
                        ReadLengthMin = Convert.ToInt64(items[1]);
                        ReadLengthMax = Convert.ToInt64(items[2]);
                        ReadLengthMean = Convert.ToDouble(items[3]);
                    }

                    else if (items[0].Equals(Resource.CsvReadGcContent))
                    {
                        ReadGcContentMin = Convert.ToDouble(items[1]);
                        ReadGcContentMax = Convert.ToDouble(items[2]);
                        ReadGcContentMean = Convert.ToDouble(items[3]);
                    }

                    else if (items[0].Equals(Resource.CsvPhredBaseQualityScore))
                    {
                        BasePhredScoreMin = Convert.ToDouble(items[1]);
                        BasePhredScoreMax = Convert.ToDouble(items[2]);
                        BasePhredScoreMean = Convert.ToDouble(items[3]);
                    }

                    else if (items[0].Equals(Resource.CsvPhredReadQualityScore))
                    {
                        ReadPhredScoreMin = Convert.ToDouble(items[1]);
                        ReadPhredScoreMax = Convert.ToDouble(items[2]);
                        ReadPhredScoreMean = Convert.ToDouble(items[3]);
                    }
                }
            }
        }

        /// <summary>
        /// Writes the sequence-level statistics
        /// </summary>
        /// <returns>StringBuilder text object</returns>
        public StringBuilder WriteSequenceLevelStats()
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine(String.Join(Resource.Delimiter, new string[] { Resource.CsvNumReads, NumberOfReads.ToString() }));

            text.AppendLine(String.Join(Resource.Delimiter, new string[] { Resource.CsvReadLength, 
                        ReadLengthMin.ToString(), ReadLengthMax.ToString(), ReadLengthMean.ToString() }));

            text.AppendLine(String.Join(Resource.Delimiter, new string[] { Resource.CsvReadGcContent,
                                        Math.Round(ReadGcContentMin, 1).ToString(), 
                                        Math.Round(ReadGcContentMax, 1).ToString(), 
                                        Math.Round(ReadGcContentMean, 1).ToString()
                                        }));

            return text;
        }

        /// <summary>
        /// Writes the quality score-level statistics
        /// </summary>
        /// <returns>StringBuilder text object</returns>
        public StringBuilder WriteQualityScoreLevelStats()
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine(String.Join(Resource.Delimiter, new string[] { Resource.CsvPhredBaseQualityScore,
                                        Math.Round(BasePhredScoreMin, 1).ToString(), 
                                        Math.Round(BasePhredScoreMax, 1).ToString(), 
                                        Math.Round(BasePhredScoreMean, 1).ToString() 
                                        }));

            text.AppendLine(String.Join(Resource.Delimiter, new string[] { Resource.CsvPhredReadQualityScore,
                                        Math.Round(ReadPhredScoreMin, 1).ToString(),
                                        Math.Round(ReadPhredScoreMax, 1).ToString(),
                                        Math.Round(ReadPhredScoreMean, 1).ToString() 
                                        }));

            return text;
        }

        /// <summary>
        /// Writes the sequence-level plot filenames
        /// </summary>
        /// <param name="excelFormat">Use Excel hyperlink format</param>
        /// <returns>StringBuilder text object</returns>
        public StringBuilder WriteSequenceLevelHyperlinks(bool excelFormat)
        {
            StringBuilder text = new StringBuilder();

            String formula = GetHyperlink(myFilenames.SequenceLengths, excelFormat);
            text.AppendLine(String.Join(Resource.Delimiter, new string[] { "Sequence Length Distributions", formula }));

            formula = GetHyperlink(myFilenames.GCContentBySequence, excelFormat);
            text.AppendLine(String.Join(Resource.Delimiter, new string[] { "GC Content By Sequence", formula }));

            formula = GetHyperlink(myFilenames.SymbolCountByPosition, excelFormat);
            text.AppendLine(String.Join(Resource.Delimiter, new string[] { "Symbol Count By Position", formula }));

            return text;
        }

        /// <summary>
        /// Writes the quality score-level plot filenames
        /// </summary>
        /// <param name="excelFormat">Use Excel hyperlink format</param>
        /// <returns>StringBuilder text object</returns>
        public StringBuilder WriteQualityScoreLevelHyperlinks(bool noExcel)
        {
            StringBuilder text = new StringBuilder();

            string formula = GetHyperlink(myFilenames.QualityScoreBySequence, noExcel);
            text.AppendLine(String.Join(Resource.Delimiter, new string[] { "Quality Score By Sequence", formula }));

            formula = GetHyperlink(myFilenames.QualityScoreByPosition, noExcel);
            text.AppendLine(String.Join(Resource.Delimiter, new string[] { "Quality Score By Position", formula }));

            return text;
        }

        #region Private members

        /// <summary>
        /// Generate Excel Hyperlink formula tag
        /// </summary>
        /// <param name="text">URI to hyperlink to</param>
        /// <param name="excelFormat">True if Excel-formatted hyperlinks are desired. False, othrewise.</param>
        /// <returns>Excel formatted formula</returns>
        private string GetHyperlink(string text, bool excelFormat)
        {
            return excelFormat ? "=HYPERLINK(\"" + text + "\")" : text;
        }

        #endregion

    }
}
