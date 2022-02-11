using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DP14ValidationTestsInterface;
using DP14ValidationAttributes_ClassLibrary;

namespace FS4500_ML_AudioValidation
{
    [DP14ValidationAttributes("Audio Tests", "SST", 3)]
    public class VTests : IFS4500_VTests
    {
        #region Members

        private AudioTS_Variance MaudNaudTest = null;           // Test1
        private AudioSDPRate audioSDPRate = null;               // Test2                  

        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public VTests()
        {
        }


        #endregion // Constructor(s)

        #region Private Methods
        #endregion // Private Method

        #region Public Method

        /// <summary>
        /// Public method in which the FS4500 Probe Manager will invoke prior to testing.
        /// </summary>
        /// <param name="testTitle"></param>
        public void Initialize(int testID, List<string> ConfigParameters, int maxReportedErrors)
        {

            switch (testID)
            {
                case 1:
                    MaudNaudTest = new AudioTS_Variance();
                    MaudNaudTest.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 2:
                    audioSDPRate = new AudioSDPRate();
                    audioSDPRate.Initialize(ConfigParameters, maxReportedErrors);
                    break;

                default:
                    break;
            }

            return;
        }


        /// <summary>
        /// get descriptions
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestDescriptions()
        {
            List<string> descriptions = new List<string>();

            descriptions.Add("Test Number:1 $$ Test Name:1) Audio TS SDP Verification $$ Description: Verifies variance between Audio TS SDP is less than 0.05% (DP1.4a 2.2.5.2)");
            descriptions.Add("Test Number:2 $$ Test Name:2) Audio Sample Delivery Run Rate $$ Description: Verifies Audio Samples are generated within +/- 4 samples of a predicted rate. (DP1.4a 2.2.5.2)");

            return descriptions;
        }


        /// <summary>
        /// Execute/run a specified test
        /// </summary>
        /// <param name="testNumber"></param>
        public List<string> doTest(int testNumber, byte[] stateData)
        {
            List<string> errMsg = new List<string>();
            try
            {
                switch (testNumber)
                {
                    case 1:
                        errMsg.AddRange(MaudNaudTest.ProcessState(stateData));
                        break;
                    case 2:
                        errMsg.AddRange(audioSDPRate.ProcessState(stateData));
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                errMsg.Add("Processing Error: " + ex.Message);
            }

            return errMsg;
        }


        /// <summary>
        /// Returns a list of strings providing the invoking program the ability to display test statistics.
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestResultsSummary(int testNumber)
        {
            List<string> stats = new List<string>();

            try
            {
                switch (testNumber)
                {
                    case 1:
                        stats.AddRange(MaudNaudTest.GetTestResultsSummary());
                        break;
                    case 2:
                        stats.AddRange(audioSDPRate.GetTestResultsSummary());
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                stats.Add("Processing Error: " + ex.Message);
            }

            return stats;
        }


        /// <summary>
        /// Returns a list of strings providing the invoking program the ability to display test statistics.
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestResultsDetailed(int testNumber)
        {
            List<string> stats = null;

            List<string> errMsg = new List<string>();
            try
            {
                switch (testNumber)
                {
                    case 1:
                        stats.AddRange(MaudNaudTest.GetTestResultsSummary());
                        break;
                    case 2:
                        stats.AddRange(audioSDPRate.GetTestResultsSummary());
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                errMsg.Add("Processing Error: " + ex.Message);
            }

            return stats;
        }


        /// <summary>
        /// Returns a list of strings providing the invoking program the ability to display test statistics.
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestOverview(int testNumber)
        {
            List<string> description = new List<string>();

            List<string> errMsg = new List<string>();
            try
            {
                switch (testNumber)
                {
                    case 1:
                        description.AddRange(MaudNaudTest.GetTestDescription());
                        break;
                    case 2:
                        description.AddRange(audioSDPRate.GetTestDescription());
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                errMsg.Add("Processing Error: " + ex.Message);
            }

            return description;
        }
        #endregion // Public Methods
    }
}
