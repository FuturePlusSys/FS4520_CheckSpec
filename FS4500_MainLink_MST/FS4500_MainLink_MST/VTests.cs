using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using DP14ValidationTestsInterface;
using DP14ValidationAttributes_ClassLibrary;

namespace FS4500_MainLink_MST
{
    //internal class VTests
    //{
    //}

    [DP14ValidationAttributes("Main Link Tests", "MST", 0)]
    public class VTests : IFS4500_VTests
    {
        #region Members

        private Test_8 m_Test8 = null;
        private Test_9 m_Test9 = null; 
        private Test_11 m_Test11 = null;                  

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
            //MessageBox.Show("testID = " + testID.ToString());

            switch (testID)
            {
                case 8:
                    m_Test8 = new Test_8();
                    m_Test8.Initialize(ConfigParameters);
                    break;
                case 9:
                    m_Test9 = new Test_9();
                    m_Test9.Initialize();
                    break;
                case 11:
                    m_Test11 = new Test_11();
                    m_Test11.Initialize(ConfigParameters);
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

            descriptions.Add("Test Number:8 $$ Test Name:Test8 $$ Description:Identifies unexpected state sequences.");
            descriptions.Add("Test Number:9 $$ Test Name:Test9 $$ Description:Every 512th BS is an SR");
            descriptions.Add("Test Number:11 $$ Test Name:Test11 $$ Description:BE Symbol Must Be followed by a pixel");

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
                    case 8:
                        errMsg.AddRange(m_Test8.ProcessState(stateData));
                        break;
                    case 9:
                        errMsg.AddRange(m_Test9.ProcessState(stateData));
                        break;
                    case 11:
                        errMsg.AddRange(m_Test11.ProcessState(stateData));
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
                    case 8:
                        stats.AddRange(m_Test8.GetTestResultsSummary());
                        break;
                    case 9:
                        stats.AddRange(m_Test9.GetTestResultsSummary());
                        break;
                    case 11:
                        stats.AddRange(m_Test11.GetTestResultsSummary());
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
                    case 8:
                        stats.AddRange(m_Test8.GetTestResultsSummary());
                        break;
                    case 9:
                        stats.AddRange(m_Test9.GetTestResultsSummary());
                        break;
                    case 11:
                        stats.AddRange(m_Test11.GetTestResultsSummary());
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
                    case 8:
                        description.AddRange(m_Test8.GetTestDescription());
                        break;
                    case 9:
                        description.AddRange(m_Test9.GetTestDescription());
                        break;
                    case 11:
                        description.AddRange(m_Test11.GetTestDescription());
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
