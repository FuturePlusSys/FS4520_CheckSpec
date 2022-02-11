using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_VideoValidation
{

    /// <summary>
    /// This class verifies the MVID state is set to zero when the VBID's No Video bit is set to 1.
    /// 
    /// This class checkes all active data lanes.
    /// 
    /// This class does NOT verify the absense of Pixel states AFTER a VBID with the No Video Bit being set to 1.
    /// 
    /// </summary>
    class MVID_ClearedToZeroWhenNoVideo
    {
        #region Members

        private ML_Common_Functions m_commonFuncs = null;

        private int MaxErrors { get; set; } = 10;
        private int m_linkWidth = -1;

        private bool m_errorEncountered = false;
        private List<string> m_errMsgs = new List<string>();

        private int m_BSSequenceCount = 0;

        private List<string> m_testInfo = new List<string>();

        private uint m_BSSequenceStartStateIndex = 0x00;
        private uint m_curStateIndex = 0x00;


        private int m_ErrorCount = 0;
        private int m_MSACount = 0;
        private bool m_MVID_NO_VIDEO_STREAM = false;

        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// default Constructor
        /// </summary>
        public MVID_ClearedToZeroWhenNoVideo()
        {
            m_commonFuncs = new ML_Common_Functions();
        }
        #endregion // Constructor(s)

        #region Private Methods

        /// <summary>
        /// Reset class members to default values.
        /// </summary>
        private void resetMembers()
        {
            m_curStateIndex = 0;
            m_ErrorCount = 0;
            m_MSACount = 0;
            m_errorEncountered = false;

            m_testInfo.Clear();
            m_errMsgs.Clear();

            m_BSSequenceCount = 0;
            m_BSSequenceStartStateIndex = 0x00;
            m_MVID_NO_VIDEO_STREAM = false;
        }


        /// <summary>
        /// Create a set strings to be used in the detailed report.
        /// </summary>
        /// <returns></returns>
        private string getTestStatus()
        {
            string status = string.Empty;

            if (!m_errorEncountered)
            {
                status = "  Passed: No errors were encountered.";
            }
            else
            {
                status = "  Failed: VB-ID packet contains an invalid Mvid value.";
            }

            return status;
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
        /// Keep track of the number of BS Sequences we've processed.
        /// </summary>
        /// <param name="stateData"></param>
        /// <param name="laneName"></param>
        private void processBSSequence(byte[] stateData)
        {
            int lane0Data = m_commonFuncs.GetFieldData(stateData, "Lane0");
            if (lane0Data > -1)
            {
                if ((lane0Data == 0x1BC) || (lane0Data == 0x17C))
                {
                    if ((m_curStateIndex - m_BSSequenceStartStateIndex) >= 4)
                    {
                        m_BSSequenceStartStateIndex = m_curStateIndex;
                        m_BSSequenceCount += 1;
                    }
                }
            }
        }


        /// <summary>
        /// Verify the MVID state is set to zero when the VBID's No Video bit is set to 1.
        /// </summary>
        /// <param name="laneName"></param>
        private bool processLane(byte[] stateData, string laneName)
        {
            bool status = true;
            string ECName = m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode"));

            //
            // Assumes Lane 0 is always processed before Lane 1, etc...
            //

            //
            // so that we can show how many lines we've processed.
            //
            if (laneName.ToUpper() == "LANE0")
                processBSSequence(stateData);


            if (ECName.ToUpper().EndsWith("VBID"))
            {
                int lane0Data = m_commonFuncs.GetFieldData(stateData, "Lane0");  // gurantee to have lane0, so use that!
                if ((lane0Data & 0x08) == 0x08)
                    m_MVID_NO_VIDEO_STREAM = true;
                else
                    m_MVID_NO_VIDEO_STREAM = false;
            }
            else if (ECName.ToUpper().EndsWith("MVID"))
            {
                if (m_MVID_NO_VIDEO_STREAM == true)
                {
                    int laneData = m_commonFuncs.GetFieldData(stateData, laneName);
                    if (laneData != 0x00)
                    {
                        updateErrorMessages("MSA Mvid[7:0] is not cleared to 0x00 for section with VBID:NoVideoStream_Flag bit set to 1.");
                        m_errorEncountered = true;
                        status = false;
                    }
                }
            }
           
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
                    // all checking assumes Lane0 ALWAYS has data...
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
            subStrings.Add("Number of BS Sequences Processed: " + m_BSSequenceCount.ToString());
            subStrings.Add("Number of MSA SDPs Processed: " + m_MSACount.ToString());

            subStrings.Add("Test Status:");
            subStrings.Add("  " + getTestStatus());



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

            description.Add(" This class verifies the MVID state is set to zero when the VBID's No Video bit is set to 1.");
            description.Add(" ");
            description.Add("  This class checkes all active data lanes.");
            description.Add(" ");
            description.Add("  This class does NOT verify the absense of Pixel states AFTER a VBID with the No Video Bit being set to 1.");

            return description;
        }
        #endregion // Public Methods
    }
}
