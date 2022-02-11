using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DP14ValidationTestsInterface;
using DP14ValidationAttributes_ClassLibrary;

namespace FS4500_eDP14SST_MainLink
{
    [DP14ValidationAttributes("MainLink Tests", "eDPSST", 1)]
    public class VTests : IFS4500_VTests
    {

        #region Members

        private SR_Symbol_Is_Every_512th_BS SR_512th_Test = null;



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
                    SR_512th_Test = new SR_Symbol_Is_Every_512th_BS();
                    SR_512th_Test.Initialize(ConfigParameters, maxReportedErrors);
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

            descriptions.Add("Test Number:1 $$ Test Name:1) SR is Every 512th BS $$ Description: Verifies every 512th BS is an SR");

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
                        errMsg.AddRange(SR_512th_Test.ProcessState(stateData));
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
                        stats.AddRange(SR_512th_Test.GetTestResultsSummary());
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
                        stats.AddRange(SR_512th_Test.GetTestResultsSummary());
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
                        description.AddRange(SR_512th_Test.GetTestDescription());
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
