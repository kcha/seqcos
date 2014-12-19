using SeqcosFilterTools.Trim;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Bio;
using Bio.IO;
using Bio.IO.FastQ;
using Bio.IO.FastA;
using SeqcosApp;

namespace FilterTools.Tests.Trim
{
    [TestClass]
    public class TrimByRegexTest
    {
        /// <summary>
        /// Trim a FASTA sequence
        /// </summary>
        [TestMethod()]
        public void FastaTrimRegex1()
        {
            Sequence seqObj = new Sequence(Alphabets.DNA, "GGGCCCGATTACATTTAAA");
            Sequence expected = new Sequence(Alphabets.DNA, "GGGCCCTTTAAA");
            TrimByRegex target = new TrimByRegex(new FastAParser(), new FastAFormatter(), new FastAFormatter(), "GATTACA");
            ISequence actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
        }

        /// <summary>
        /// Trim a FASTA sequence from the 3' end
        /// </summary>
        [TestMethod()]
        public void FastaTrimRegex2()
        {
            Sequence seqObj = new Sequence(Alphabets.DNA, "TTTAAAGATTACATTTAAA");
            Sequence expected = new Sequence(Alphabets.DNA, "TTTAAAGATTACA");
            TrimByRegex target = new TrimByRegex(new FastAParser(), new FastAFormatter(), new FastAFormatter(), @"TTTAAA$");
            ISequence actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
        }

        /// <summary>
        /// Trim a FASTQ sequence
        /// </summary>
        [TestMethod()]
        public void FastqTrimRegex1()
        {
            QualitativeSequence seqObj = new QualitativeSequence(Alphabets.DNA, FastQFormatType.Sanger, "GGGCCCGATTACATTTAAA", "ABCABCIIIIIIIABCABC");
            QualitativeSequence expected = new QualitativeSequence(Alphabets.DNA, FastQFormatType.Sanger, "GGGCCCTTTAAA", "ABCABCABCABC");
            TrimByRegex target = new TrimByRegex(new FastQParser(), new FastQFormatter(), new FastQFormatter(), "GATTACA");
            ISequence actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
            Assert.AreEqual(BioHelper.GetEncodedQualityScoreStringSequence(expected), BioHelper.GetEncodedQualityScoreStringSequence(actual as QualitativeSequence));
        }
    }
}