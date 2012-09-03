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
    ///This is a test class for TrimByLengthTest and is intended
    ///to contain all TrimByLengthTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TrimByLengthTest
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

        /// <summary>
        /// Verify that a Sequence object is trimmed correctly
        /// by 1 bp from left side
        ///</summary>
        [TestMethod()]
        public void FastqTrimFromLeftTest1()
        {
            ISequence seqObj = new QualitativeSequence(Alphabets.DNA, FastQFormatType.Sanger,
                                            "GGCGCACTTACACCCTACATCCATTG", "IIIIG1?II;IIIII1IIII1%.I7I");
            ISequence expected = new QualitativeSequence(Alphabets.DNA, FastQFormatType.Sanger,
                                            "GCGCACTTACACCCTACATCCATTG", "IIIG1?II;IIIII1IIII1%.I7I");
            double trimLength = seqObj.Count - 1;
            bool trimFromStart = true;
            TrimByLength target = new TrimByLength(new FastQParser(), new FastQFormatter(), null, trimLength, trimFromStart); 
            ISequence actual;
            actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
            string expectedQualityScores = System.Text.Encoding.Default.GetString((expected as QualitativeSequence).GetEncodedQualityScores());
            string actualQualityScores = System.Text.Encoding.Default.GetString((actual as QualitativeSequence).GetEncodedQualityScores());
            Assert.AreEqual(expectedQualityScores, actualQualityScores);
        }

        /// <summary>
        /// Verify that a Sequence object is trimmed correctly
        /// by l-1 bp from left side, where l is length of read
        ///</summary>
        [TestMethod()]
        public void FastqTrimFromLeftTest2()
        {
            ISequence seqObj = new QualitativeSequence(Alphabets.DNA, FastQFormatType.Sanger,
                                            "GGCGCACTTACACCCTACATCCATTG", "IIIIG1?II;IIIII1IIII1%.I7I");
            ISequence expected = new QualitativeSequence(Alphabets.DNA, FastQFormatType.Sanger,
                                            "G", "I");
            double trimLength = 1;
            bool trimFromStart = true;
            TrimByLength target = new TrimByLength(new FastQParser(), new FastQFormatter(), null, trimLength, trimFromStart);
            ISequence actual;
            actual = target.Trim(seqObj);
            Assert.AreEqual(BioHelper.GetStringSequence(expected), BioHelper.GetStringSequence(actual));
            string expectedQualityScores = System.Text.Encoding.Default.GetString((expected as QualitativeSequence).GetEncodedQualityScores());
            string actualQualityScores = System.Text.Encoding.Default.GetString((actual as QualitativeSequence).GetEncodedQualityScores());
            Assert.AreEqual(expectedQualityScores, actualQualityScores);
        }

        /// <summary>
        /// Verify that null is returned if trim length is greater than original length.
        ///</summary>
        [TestMethod()]
        public void FastqTrimNullTest()
        {
            ISequence seqObj = new QualitativeSequence(Alphabets.DNA, FastQFormatType.Sanger,
                                            "GGCGCACTTACACCCTACATCCATTG", "IIIIG1?II;IIIII1IIII1%.I7I");
            double trimLength = seqObj.Count + 1;
            bool trimFromStart = false;
            TrimByLength target = new TrimByLength(new FastQParser(), new FastQFormatter(), null, trimLength, trimFromStart);
            ISequence actual;
            actual = target.Trim(seqObj);
            Assert.IsNull(actual);
        }
    }
}
