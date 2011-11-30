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
            string formatAsString = "Solexa";
            FastQFormatType expected = FastQFormatType.Solexa; 
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
            string[] expected = new string[] { "Illumina", "Solexa", "Sanger" };
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
    }
}
