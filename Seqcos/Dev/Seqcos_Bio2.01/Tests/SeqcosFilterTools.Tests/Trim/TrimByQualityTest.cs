using SeqcosFilterTools.Trim;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Bio;
using Bio.IO;
using Bio.IO.FastQ;
using Bio.IO.FastA;
using SeqcosApp;

namespace FilterTools.Tests
{
    
    
    /// <summary>
    ///This is a test class for TrimByQualityTest and is intended
    ///to contain all TrimByQualityTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TrimByQualityTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        /// Trim a Sanger-encoded FASTQ sequence from both sides
        ///</summary>
        [TestMethod()]
        public void FastqTrimSanger1()
        {
            FastQFormatType format = FastQFormatType.Sanger;
            QualitativeSequence seqObj = new QualitativeSequence(Alphabets.DNA, format,
                                            "GGCGCACTTACACCCTACAT", "!!!000IIIGGGIHHH$$$$");
            QualitativeSequence expected = new QualitativeSequence(Alphabets.DNA, format,
                                            "CTTACACCCT", "IIIGGGIHHH");
            TrimByQuality target = new TrimByQuality(new FastQParser(), new FastQFormatter(), new FastQFormatter(), 20, true, 1); 
            ISequence actual;
            actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
            Assert.AreEqual(BioHelper.GetEncodedQualityScoreStringSequence(expected), BioHelper.GetEncodedQualityScoreStringSequence(actual as QualitativeSequence));
        }

        /// <summary>
        /// Trim a Sanger-encoded FASTQ sequence with low quality bases only at the 3' end 
        /// </summary>
        [TestMethod()]
        public void FastqTrimSanger2()
        {
            FastQFormatType format = FastQFormatType.Sanger;
            QualitativeSequence seqObj = new QualitativeSequence(Alphabets.DNA, format,
                                            "GGCGCACTTACACCCTACAT", "ABCABCIIIGGGIHHH$$$$");
            QualitativeSequence expected = new QualitativeSequence(Alphabets.DNA, format,
                                            "GGCGCACTTACACCCT", "ABCABCIIIGGGIHHH");
            TrimByQuality target = new TrimByQuality(new FastQParser(), new FastQFormatter(), new FastQFormatter(), 20, true, 1); 
            ISequence actual;
            actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
            Assert.AreEqual(BioHelper.GetEncodedQualityScoreStringSequence(expected), BioHelper.GetEncodedQualityScoreStringSequence(actual as QualitativeSequence));
        }

        /// <summary>
        /// Trim a Sanger-encoded FASTQ sequence that restricts trimming from the start
        /// </summary>
        [TestMethod()]
        public void FastqTrimSanger3()
        {
            FastQFormatType format = FastQFormatType.Sanger;
            QualitativeSequence seqObj = new QualitativeSequence(Alphabets.DNA, format,
                                            "GGCGCACTTACACCCTACAT", "!IIGGGIIIGGGIHHH$$$$");
            QualitativeSequence expected = new QualitativeSequence(Alphabets.DNA, format,
                                            "GGCGCACTTACACCCT", "!IIGGGIIIGGGIHHH");
            TrimByQuality target = new TrimByQuality(new FastQParser(), new FastQFormatter(), new FastQFormatter(), 20, false, 1); 
            ISequence actual;
            actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
            Assert.AreEqual(BioHelper.GetEncodedQualityScoreStringSequence(expected), BioHelper.GetEncodedQualityScoreStringSequence(actual as QualitativeSequence));
        }

        /// <summary>
        /// Trim a Solexa/Illumina_v_1.0-encoded FASTQ sequence
        /// </summary>
        [TestMethod()]
        public void FastqTrimSolexa1()
        {
            FastQFormatType format = FastQFormatType.Solexa_Illumina_v1_0;
            QualitativeSequence seqObj = new QualitativeSequence(Alphabets.DNA, format,
                                            "GGCGCACTTACACCCTACAT", "AAABBBabacabcabca???");
            QualitativeSequence expected = new QualitativeSequence(Alphabets.DNA, format,
                                            "CTTACACCCTA", "abacabcabca");
            TrimByQuality target = new TrimByQuality(new FastQParser(), new FastQFormatter(), new FastQFormatter(), 20, true, 1); 
            ISequence actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
            Assert.AreEqual(BioHelper.GetEncodedQualityScoreStringSequence(expected), BioHelper.GetEncodedQualityScoreStringSequence(actual as QualitativeSequence));
        }

        /// <summary>
        /// Trim an Illumina_v1_5-encoded FASTQ sequence
        /// </summary>
        [TestMethod()]
        public void FastqTrimIllumina1_5()
        {
            FastQFormatType format = FastQFormatType.Illumina_v1_5;
            QualitativeSequence seqObj = new QualitativeSequence(Alphabets.DNA, format,
                                            "GGCGCACTTACACCCTACAT", "CCCBBBabacabcabcaCCC");
            QualitativeSequence expected = new QualitativeSequence(Alphabets.DNA, format,
                                            "CTTACACCCTA", "abacabcabca");
            TrimByQuality target = new TrimByQuality(new FastQParser(), new FastQFormatter(), new FastQFormatter(), 20, true, 1);
            ISequence actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
            Assert.AreEqual(BioHelper.GetEncodedQualityScoreStringSequence(expected), BioHelper.GetEncodedQualityScoreStringSequence(actual as QualitativeSequence));
        } 
    }
}
