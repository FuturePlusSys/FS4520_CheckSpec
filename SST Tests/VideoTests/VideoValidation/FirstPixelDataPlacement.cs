using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_VideoValidation
{
    /// <summary>
    /// This class verifies pixel data immediately follows the BE control characters.
    /// 
    /// This test tests all active data lane.
    /// 
    /// This class does not evaluate the pixel value(s).
    /// </summary>
    class FirstPixelDataPlacement
    {
        #region Members
        private ML_Common_Functions m_commonFuncs = null;

        private int MaxErrors { get; set; } = 10;

        private bool m_errorEncountered = false;
        private int m_ErrorCount = 0;
        private List<string> m_errMsgs = new List<string>();

        private List<string> m_testInfo = new List<string>();
        private uint m_curStateIndex = 0x00;

        private int m_ActiveVideo_SegmentCount = 0x00;

        private string m_prevECName = "";
        private int m_linkWidth = -1;
        private bool m_inhibitErrors = false;

        #endregion // Members

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public FirstPixelDataPlacement()
        {
            m_commonFuncs = new ML_Common_Functions();
        }

        #region Constructor(s)
        #endregion // Constructor(s)

        #region Private Methods

        /// <summary>
        /// Reset class members to default values.
        /// </summary>
        private void resetMembers()
        {
            m_inhibitErrors = false;
            m_errorEncountered = false;
            m_ErrorCount = 0;
            m_curStateIndex = 0x00;
            m_ActiveVideo_SegmentCount = 0x00;
            m_prevECName = "";
            m_testInfo.Clear();
            m_errMsgs.Clear();
        }



        /// <summary>
        ///  get the index used to index for the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private int getLaneIndex(string name)
        {
            int index = -1;

            switch (name.ToLower())
            {
                case "lane0":
                    index = 0;
                    break;

                case "lane1":
                    index = 1;
                    break;

                case "lane2":
                    index = 2;
                    break;

                case "lane3":
                    index = 3;
                    break;

                default:
                    break;
            }

            return index;
        }


        /// <summary>
        /// Update the error message and error count
        /// </summary>
        /// <param name="errorMsg"></param>
        private void updateErrorMessages(string errorMsg)
        {
            m_errMsgs.Add(errorMsg);
            m_ErrorCount += 1;
        }


        /// <summary>
        /// Process the lane data for BS or BF control characters.
        /// </summary>
        /// <param name="laneName"></param>
        private bool processLane(byte[] stateData, string laneName)
        {
            bool status = true;
            int countIndex = getLaneIndex(laneName);

            int lane0Data = m_commonFuncs.GetFieldData(stateData, laneName);
            if (lane0Data > -1)
            {
                string ECName = m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode"));
                if (m_prevECName.EndsWith("BE")) // Horizontal or Veritcal
                {
                    if (!m_inhibitErrors && (ECName == "Hor. BS") || (ECName == "Ver. BS") || (ECName == "Hor. SR") || (ECName == "Ver. SR"))
                    {
                        m_prevECName = ECName;
                    }
                    else if (!m_inhibitErrors && !ECName.EndsWith("Pixel"))
                    {
                        // Error
                        updateErrorMessages(laneName + " - Invalid Start-of Active Video Segment");
                        m_inhibitErrors = true;
                    }

                    if ((m_linkWidth == 1) ||
                        ((m_linkWidth == 2) && (laneName == "Lane1")) ||
                        ((m_linkWidth == 4) && (laneName == "Lane3")))
                    {
                        m_prevECName = "";  // Reset
                        m_inhibitErrors = false;
                    }
                }
                else if (ECName.EndsWith("BE"))
                {
                    if ((m_linkWidth == 1) ||
                        ((m_linkWidth == 2) && (laneName == "Lane1")) ||
                        ((m_linkWidth == 4) && (laneName == "Lane3")))
                    {
                        m_prevECName = ECName;
                        m_ActiveVideo_SegmentCount += 1;
                    }
                }
            }
            else
            {
                m_testInfo.Add("Lane0 - Get Lane Data Failure");
                m_ErrorCount += 1;
            }

            return status;
        }


        /// <summary>
        /// Create a set strings to be used in the detailed report.
        /// </summary>
        /// <returns></returns>
        private List<string> getTestStatus()
        {
            List<string> status = new List<string>();

            if (m_ErrorCount == 0)
                status.Add("  Passed: No errors were encountered.");
            else
                status.Add("  Failed: Errors Encountered");

            return status;
        }

        #endregion // Private Methods

        #region Public Methods
        /// <summary>
        /// Public method in which the FS4500 Probe Manager will invoke prior to testing.
        /// </summary>
        /// <param name="ConfigParameters"></param>
        public void Initialize(List<string> ConfigParameters, int maxReportedErrors)
        {
            resetMembers();

            m_linkWidth = m_commonFuncs.GetConfigParameterValue(ConfigParameters, "WIDTH");
            m_ErrorCount = 0;
            MaxErrors = maxReportedErrors;

            return;
        }


        /// <summary>
        /// Process a given state
        /// </summary>
        /// <param name="stateData"></param>
        /// <returns></returns>
        public List<string> ProcessState(params byte[] stateData)
        {
            try
            {
                bool status = true;
                m_errMsgs.Clear();

                if (m_ErrorCount < MaxErrors)
                {
                    status = processLane(stateData, "Lane0");
                    if (status && m_linkWidth >= 2)
                        status = processLane(stateData, "Lane1");
                    if (status && m_linkWidth > 2)
                    {
                        status = processLane(stateData, "Lane2");
                        if (status)
                            status = processLane(stateData, "Lane3");
                    }
                }
            }
            catch (Exception ex)
            {
                m_testInfo.Add("Processing Error: " + ex.Message);
            }

            // just so we can report in the status section how many states were processed.
            m_curStateIndex += 1;

            return m_errMsgs;
        }

        /// <summary>
        /// Returns a list of strings providing the invoking program the ability to display test statistics.
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestResultsSummary()
        {
            List<string> stats = new List<string>();

            // didn't use string builder because I didn't want to split the strings on '\r'... too ugly!

            List<string> subStrings = new List<string>();
            subStrings.Add("Number Of States Processed: " + String.Format("{0:n0}", m_curStateIndex + 1));
            subStrings.Add("Number Of Active Video Segments Encountered: " + m_ActiveVideo_SegmentCount.ToString());

            subStrings.Add("Test Status:");
            subStrings.AddRange(getTestStatus());

            if (subStrings.Count > 0)
                stats.AddRange(subStrings);

            return stats;
        }


        /// <summary>
        /// Returns a list of strings providing the invoking program the ability to display test statistics.
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestResultsDetailed()
        {
            List<string> stats = null;

            stats.Add("None");

            return stats;
        }


        /// <summary>
        /// Return testual explaination of what the test is doing..
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestDescription()
        {
            List<string> description = new List<string>();

            description.Add(" This test verifies pixel data immediately follows BE control characters.");
            description.Add("  ");
            description.Add(" This test tests all active data lane.");
            description.Add(" ");
            description.Add(" This test does not evaluate the pixel value(s).");

            return description;
        }
        #endregion // Public Methods
    }
}
