using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_VideoValidation
{

    /// <summary>
    /// This clas verified that the Pixel Transfer Units, made up of 
    /// pixels and one set of stuffing dummy states, are are between 
    /// 32 and 64 states in length.
    /// 
    /// The class only operates lane 0.
    /// </summary>
    class ActiveVideoTransferUnitLength
    {
        #region Members

        private ML_Common_Functions m_commonFuncs = null;

        private int MaxErrors { get; set; } = 10;
        private int m_linkWidth = -1;

        private int m_ErrorCount = 0;
        private List<string> m_errMsgs = new List<string>();
        private List<string> m_testInfo = new List<string>();
        //private bool m_inhibitChecking = false;

        private uint m_curStateIndex = 0x00;

        private Trace_Section_ID[] m_sectionID = new Trace_Section_ID[4];
        private int[] m_TULength = new int[4];
        private int[] m_TUCount = new int[4];


        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public ActiveVideoTransferUnitLength()
        {
            m_commonFuncs = new ML_Common_Functions();
        }

        #endregion // Constructor(s)

        #region Private Methods

        /// <summary>
        /// Create a set strings to be used in the detailed report.
        /// </summary>
        /// <returns></returns>
        private string getTestStatus()
        {
            string status = string.Empty;

            if (m_ErrorCount == 0)
            {
                status = "Passed: No errors were encountered.";
            }
            else
            {
                status = "Failed: Errors Encountered";
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
        /// Reset class members to default values.
        /// </summary>
        private void resetMembers()
        {
            m_ErrorCount = 0;
            m_curStateIndex = 0x00;
            m_testInfo.Clear();
            m_errMsgs.Clear();
            //m_inhibitChecking = false;

            Array.Clear(m_TULength, 0, m_TULength.Length);
            Array.Clear(m_TUCount, 0, m_TUCount.Length);

            for (int i = 0; i < 4; i++)
                m_sectionID[i] = Trace_Section_ID.Unknown;

        }


        /// <summary>
        /// Verify the Transfer Unit state count.
        /// </summary>
        /// <param name="laneIndex"></param>
        /// <returns></returns>
        private bool verifyTULength(int laneIndex)
        {
            bool status = true;

            if ((m_TULength[laneIndex] < 32) || (m_TULength[laneIndex] > 64))
                status = false;

            return status;
        }


        /// <summary>
        /// Update the local member indicate we are either in Blanking or Active sections of the trace data
        /// </summary>
        /// <param name="ECName"></param>
        private void updateSectionID(int laneIndex, int laneData)
        {
            if ((laneData == 0x1BC) || (laneData == 0x11C) || (laneData == 0x17C)) // BS, SR  or BF ctrl char    
            {
                m_sectionID[laneIndex] = Trace_Section_ID.Blanking;
            }
            if (laneData == 0x1FB)  // BE ctrl char  
            {
                m_sectionID[laneIndex] = Trace_Section_ID.Active;
                m_TULength[laneIndex] = 0x00;
            }
        }


        /// <summary>
        /// Process the lane data for Stuffing Sumbols Framing
        /// </summary>
        /// Assumption/Limitations:  The test will not verify a partial FS/FE section 
        ///                          at the very beginning of the captured data (if the
        ///                          FS symbol is not located in the captured data).
        ///                          
        ///        Lane X
        ///      +---------+  ---- Start of Transfer Unit
        ///      |         |          :
        ///      |  Valid  |          :
        ///      |  Data   |          :
        ///      | Symbols |          :
        ///      |    :    |          :
        ///      |    :    |          :
        ///      +---------+          :
        ///      |   FS    
        ///      +---------+       32 - 64 states...
        ///      | Stuffed |
        ///      |   Data  |          :
        ///      | Sumbols |          :
        ///      +---------+          :
        ///      |   FE    |          :
        ///      +---------+  ---- End of Transfer Unit
        ///                         
        /// <param name="laneName"></param>
        private bool processLane(byte[] stateData, string laneName)
        {
            bool status = true;

            int laneData = m_commonFuncs.GetFieldData(stateData, laneName);
            int laneIndex = m_commonFuncs.GetLaneIndex(laneName);

            if (laneData > -1)
            {
                updateSectionID(laneIndex, laneData);  // changes m_sectionID variable with Event Codes of Blanking Start or End          
                if (m_sectionID[laneIndex] == Trace_Section_ID.Active)
                {
                    if (laneData == 0x1F7)             // FE ctrl char
                    {
                        // Verify the TU length and report an error if necessary
                        if (verifyTULength(laneIndex) == false)
                        {
                            updateErrorMessages("Invalid Transfer Unit Length");
                            //m_inhibitChecking = true;
                        }

                        // reset the Transfer Unit state count
                        m_TULength[laneIndex] = 0x00;

                        // keep count of the number of TU's we've processed.
                        m_TUCount[laneIndex] += 1;
                    }
                    else
                    {
                        //if (!m_inhibitChecking)
                        //{
                            // simply increment the TU state count
                            m_TULength[laneIndex] += 1;
                        //}
                    }
                }
            }
            else
            {
                m_testInfo.Add(laneName + " - Get Lane Data Failure");
                m_ErrorCount += 1;
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
                if (m_ErrorCount < MaxErrors)
                {
                    // all checking assumes Lane0 ALWAYS has data...
                    status = processLane(stateData, "Lane0");
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
            subStrings.Add("Number Of States Processed: "         + String.Format("{0:n0}", m_curStateIndex + 1));
            subStrings.Add("Number of Transfer Units Processed: " + String.Format("{0:n0}", m_TUCount[0]));

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

            description.Add(" This test verifies Pixel Transfer Units, made up of");
            description.Add(" pixels and one set of stuffing dummy states, have a length between");
            description.Add(" 32 and 64 states.");
            description.Add(" ");
            description.Add(" The test only operates lane 0.");

            return description;
        }
    }

    #endregion // Public Methods
}
