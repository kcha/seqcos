// *********************************************************************
// 
//     Copyright (c) Microsoft, 2011. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *******************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bio;
using Bio.Algorithms.Alignment;
using Bio.IO;
using Bio.SimilarityMatrices;
using SeqcosApp.Analyzer.NCBI;
using SeqcosApp.Properties;
using System.Threading;

namespace SeqcosApp.Analyzer
{
    /// <summary>
    /// Class that searches for sequence contaminants in the input data, such as
    /// adapters and/or primers.
    /// </summary>
    public class SequenceContaminationFinder
    {

        #region Members

        public readonly IEnumerable<ISequence> TargetSequences; // target == input sequences

        // SW alignment-related members
        //private string illuminaContaminantsFile = "../Univec/UniVec_Core.fasta";
        //public IList<ISequence> ReferenceSequences { get; private set; } // adapter sequences
        //public Dictionary<ISequence, List<ISequence>> ContaminatedSequences { get; private set; }

        // BLAST-related members
        public BlastLocalHandler Blaster { get; private set; }

        // Dictionary where key is the subject (i.e. database sequence) sequence name, and formatAsString
        // is a list of query sequence IDs that have a HSP with the subject.
        public Dictionary<string, List<string>> BlastHspCounter { get; private set; }

        private const double minimumScoreWeight = 0.6; // weight used to calculate the minimum allowed score for best matches

        #endregion

        #region Constructor

        /// <summary>
        /// Compare input sequences with a list of known contaminants.
        /// </summary>
        /// <param name="targetParser">The target (aka input) sequences</param>
        public SequenceContaminationFinder(ISequenceParser targetParser)
        {
            if (targetParser == null)
                throw new ArgumentNullException("targetParser");

            // Load target (aka input sqeuences) from file
            this.TargetSequences = targetParser.Parse();

            // Initially set this as null until a BLAST Xml file is processed.
            this.BlastHspCounter = new Dictionary<string,List<string>>();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Execute a local BLAST against a sequence database. 
        /// </summary>
        /// <param name="db">Name of the BLAST database (use filename only).</param>
        /// <param name="targetFasta">Filename of the input FASTA file.</param>
        public void RunLocalBlast(string db, string targetFasta)
        {
            this.Blaster = new BlastLocalHandler();

            #region BLAST parameters

            IBlastParameters args;

            if (db.Equals(Resource.UniVec) || db.Equals(Resource.UniVec_Core))
            {
                args = new UniVecParameters();
            }
            else
            {
                args = new BlastLocalParameters();
            }

            args.Database = db;
            args.Threads = Environment.ProcessorCount;
            string outputFilename = Path.ChangeExtension(targetFasta, Properties.Resource.BLAST_OUTPUT_SUFFIX_ASN);

            #endregion

            this.Blaster.ExecuteLocal(targetFasta, outputFilename, args);

            // Convert to XML
            string xmlFilename = Path.ChangeExtension(outputFilename, Resource.XmlExtension);
            BlastTools.FormatASNTo("5", outputFilename, xmlFilename);
            this.BlastHspCounter = BlastTools.ProcessBlastXml(xmlFilename);

            // Convert to CSV
            string csvFilename = Path.ChangeExtension(outputFilename, Resource.CsvExtension);
            BlastTools.FormatASNTo("10", outputFilename, csvFilename);
        }

        #endregion

    }
}
