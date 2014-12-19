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
using System.IO;
using SeqcosApp.Properties;

namespace SeqcosApp
{
    /// <summary>
    /// Class for establishing filenames of output files
    /// </summary>
    public class Filenames
    {
        /// <summary>
        /// Format of the image files
        /// </summary>
        private readonly string imageFormat;

        /// <summary>
        /// The filename of the input file
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// Gets the filename of the input filename
        /// </summary>
        public string Prefix
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this.FileName);
            }
        }
        
        /// <summary>
        /// Filename of plot showing mean quality score of reads
        /// </summary>
        public string QualityScoreBySequence;

        /// <summary>
        /// Filename of boxplot showing base quality scores at each position of reads
        /// </summary>
        public string QualityScoreByPosition;

        /// <summary>
        /// Filename of plot showing distribution of sequence lengths
        /// </summary>
        public string SequenceLengths;

        /// <summary>
        /// Filename of plot showing distribution of GC content
        /// </summary>
        public string GCContentBySequence;

        /// <summary>
        /// Filename of plot showing distribution of nucleotides at each position
        /// </summary>
        public string SymbolCountByPosition;

        /// <summary>
        /// Filename of output csv file
        /// </summary>
        public string CSV;

        /// <summary>
        /// Constructor for generating standard image file names
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="chartFormat"></param>
        public Filenames(string filename, string chartFormat)
        {
            imageFormat = "." + chartFormat;
            FileName = filename;

            QualityScoreBySequence = Prefix + "." + Resource.QualityScoreBySequenceKeyword + imageFormat;
            QualityScoreByPosition = Prefix + "." + Resource.QualityScoreByPositionKeyword + imageFormat;
            SequenceLengths = Prefix + "." + Resource.SequenceLengthDistributionKeyword + imageFormat;
            GCContentBySequence = Prefix + "." + Resource.GCContentBySequenceKeyword + imageFormat;
            SymbolCountByPosition = Prefix + "." + Resource.SymbolCountByPositionKeyword + imageFormat;
            CSV = Prefix + ".stats.csv";
        }

        /// <summary>
        /// Gets a list of filenames of sequence-related plots
        /// </summary>
        /// <returns>List of filenames</returns>
        public List<string> GetSequenceLevelFilenames()
        {
            List<string> myList = new List<string>();
            myList.Add(SequenceLengths);
            myList.Add(GCContentBySequence);
            myList.Add(SymbolCountByPosition);
            return myList;
        }

        /// <summary>
        /// Get a list of filenames of quality score-related plots
        /// </summary>
        /// <returns>List of filenames</returns>
        public List<string> GetQualityLevelFilenames()
        {
            List<string> myList = new List<string>();
            myList.Add(QualityScoreByPosition);
            myList.Add(QualityScoreBySequence);
            return myList;
        }
    }
}
