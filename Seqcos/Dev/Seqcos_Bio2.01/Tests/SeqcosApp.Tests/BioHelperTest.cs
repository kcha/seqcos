using SeqcosApp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Bio;
using System.Collections.Generic;

namespace Tests
{
    
    
    /// <summary>
    ///This is a test class for BioHelperTest and is intended
    ///to contain all BioHelperTest Unit Tests
    ///</summary>
    [TestClass()]
    public class BioHelperTest
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
        /// Verifies that the FastqFormat enum is returned
        ///</summary>
        [TestMethod()]
        public void GetQualityFormatTypeTest()
        {
            string formatAsString = "Solexa_Illumina_v1_0";
            FastQFormatType expected = FastQFormatType.Solexa_Illumina_v1_0; 
            FastQFormatType actual;
            actual = BioHelper.GetQualityFormatType(formatAsString);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that method returns all expected Fastq formats as a string array
        ///</summary>
        [TestMethod()]
        public void QueryValidFastqFormatsTest()
        {
            string[] expected = new string[] { "Illumina_v1_3", "Illumina_v1_5", "Illumina_v1_8", "Sanger", "Solexa_Illumina_v1_0" };
            string[] actual;
            actual = BioHelper.QueryValidFastqFormats();
            Assert.AreEqual(expected.Length, actual.Length);
            foreach (string format in actual)
            {
                Assert.IsTrue(expected.Any(s => s.Equals(format)));
            }
        }

        /// <summary>
        /// Verify that the generic all files filter is returned
        ///</summary>
        [TestMethod()]
        public void QuerySupportedFileFiltersTest()
        {
            string all = "All files (*.*)|*.*";
            List<string> actual;
            actual = BioHelper.QuerySupportedFileFilters();
            Assert.IsTrue(actual.Any(s => s.Equals(all)));
        }

        /// <summary>
        ///A test for GetStringSequence
        ///</summary>
        [TestMethod()]
        public void GetStringSequenceTest()
        {
            ISequence seqObj = new QualitativeSequence(Alphabets.DNA, FastQFormatType.Sanger,
                                            "GGCGCACTTACACCCTACATCCATTG", "IIIIG1?II;IIIII1IIII1%.I7I");
            string expected = "GGCGCACTTACACCCTACATCCATTG";
            string actual;
            actual = BioHelper.GetStringSequence(seqObj);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Test for GetEncodedQualityScoreStringSequence
        /// </summary>
        [TestMethod()]
        public void GetEncodedQualityScoreTest()
        {
            QualitativeSequence seqObj = new QualitativeSequence(Alphabets.DNA, FastQFormatType.Sanger,
                                            "TACACCCTACATCCATTGAAAA", "!!\"\"##$$%%&&''()ABCDEI");
            string expected = "!!\"\"##$$%%&&''()ABCDEI";
            string actual;
            actual = BioHelper.GetEncodedQualityScoreStringSequence(seqObj);
            Assert.AreEqual(expected, actual);
        }
    }
}
