using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_FramingValidation
{
    /// <summary>
    /// This class verifies Info Frame or MSA Secondary Data Packets
    /// are issued once per frame during the veritical blanking section
    /// (end of frame).
    /// 
    /// The class detects vertical VBIDs and expects to encounter a single 
    /// Info Frame SDP before the next Horizontal VBID is encountered.
    /// 
    /// The class reports an error if more than one Info Frame or MSA SDP is encountered 
    /// between the Ver. VBIDs and the Hor. VBID.  
    ///     NOTE: Ver. VBIDs denote the end of frames, Hor VBID denotes the start of frame
    ///     
    /// The class reports an error if info frames or MSA are located in the non-blanking section.
    /// 
    /// The class processes/utilizes Event Codes to identify state types.  
    /// 
    /// The class does not process any lane data to make any of its decisions.
    /// </summary>
    class IssuedOncePerFrame
    {
        #region Members

        public string SDP_ECName { get; set; } = "";
        private int MaxErrors { get; set; } = 10;

        private bool m_errorEncountered = false;
        private List<string> m_errMsgs = new List<string>();

        private List<string> m_testInfo = new List<string>();
        private uint m_curStateIndex = 0x00;

        private int m_linkWidth = -1;

        private ML_Common_Functions m_commonFuncs = null;

        private int m_ErrorCount = 0;
        private int m_SPDCount_Total = 0x00;
        private int m_SPDCount = 0x00;
        private int m_frameCount = 0x00;
        private bool m_inSDP = false;
        private int m_horBSCount = 0x00;


        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public IssuedOncePerFrame()
        {
            m_commonFuncs = new ML_Common_Functions();
        }

        #endregion // Constructor(s)

        #region Private Methods

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
        /// Reset class members to default values.
        /// </summary>
        private void resetMembers()
        {
            m_errorEncountered = false;
            m_ErrorCount = 0;
            m_curStateIndex = 0x00;
            m_testInfo.Clear();
            m_errMsgs.Clear();

            m_curStateIndex = 0x00;
            m_inSDP = false;

            m_horBSCount = 0x00;
            m_SPDCount_Total = 0x00;
            m_SPDCount = 0x00;
        }




        /// <summary>
        /// This method locates a specifed SDP, Audio Info Frame or MSAm and ensures it occurs  
        /// once per frame.
        /// 
        /// </summary>
        /// <param name="laneName"></param>
        private bool processLane(byte[] stateData) 
        {
            bool status = true;
            string ECName = m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode"));


            if (ECName == "Hor. BS")
            {
                m_horBSCount += 1;              // increment the count of Horizontal BS's, don't care if there are groups of 4 of these...
            }
            else if (ECName == "Ver. BS")
            { 
                if (m_horBSCount > 0)           // count > 0 indicates we have toggled to the start of frame...
                {
                    m_frameCount += 1;          // increment the number of start of frames we've seen...
                    if (m_frameCount > 1)       // need to have processed the very first frame before we can report any errors
                    {
                        m_SPDCount_Total += m_SPDCount;
                        if ((m_SPDCount == 0) || (m_SPDCount > 1))
                            updateErrorMessages("Invalid number of " + SDP_ECName + " in frame:  " + m_SPDCount);
                    }
                    else
                    {
                        m_SPDCount_Total += 1;
                    }

                    m_SPDCount = 0;       // info frame counts for this frame...
                    m_horBSCount = 0;           // reset the horziontal BS count... so we'll know when the end/start of the next frame will happen
                }
            }
            else if (ECName.Contains(SDP_ECName)) //"Info Frame"))
            {
                if (!m_inSDP)
                {
                    m_SPDCount += 1;      // increment the count...
                    m_inSDP = true;
                }
            }
            else
            {
                if (m_inSDP)
                    m_inSDP = false;
            }

            return status;
        }

        #endregion // Private Method

        #region Public Method

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

                if (((SDP_ECName == "Info Frame") || (SDP_ECName == "MSA")) && (m_ErrorCount < MaxErrors))
                {
                    status = processLane(stateData); 
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
            subStrings.Add("Number Of " + SDP_ECName + " Encountered: " + m_SPDCount_Total.ToString()); // m_InfoFrameProcessed.ToString());

            subStrings.Add("Test Status:");
            subStrings.AddRange( getTestStatus() );

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

            description.Add("  This test verifies Info Frame or MSA Secondary Data Packets");
            description.Add("  are issued once per frame during the veritical blanking section.");
            description.Add("  ");
            description.Add("  The test detects vertical VBIDs and expects to encounter a single");
            description.Add("  Info Frame SDP before the next Horizontal VBID is encountered.");
            description.Add("  ");
            description.Add("  The test reports an error if more than one Info Frame or MSA SDP is encountered");
            description.Add("  between the Ver. VBIDs and the Hor. VBID.");
            description.Add("      NOTE: Ver. VBIDs denote the end of frames, Hor VBID denotes the start of frame ");
            description.Add("  ");
            description.Add("  The test reports an error if info frames or MSA are located in the non-blanking section.");
            description.Add("  ");
            description.Add("  The test processes/utilizes Event Codes to identify state types.");
            description.Add("  ");
            description.Add("  The test does not process any lane data to make any of its decisions.");

            return description;
        }
    }

    #endregion // Public Methods
}
