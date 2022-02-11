using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_VideoValidation
{
    /// <summary>
    /// This class verifies blank lines do not contain any pixel states.
    /// 
    /// The class extracts the Vertical_Blanking_Flag and No_Video bits from
    /// each encountered VBID (lane 0 only).  The class verifies the blanking
    /// section does not contain any pixel states for which either VBID sub-field
    /// is set to 1.
    /// 
    /// This class only operates on lane 0.
    /// </summary>
    class VBID_NoVideo_Blanking_Tests
    {
        #region Members
        private ML_Common_Functions m_commonFuncs = null;

        private int MaxErrors { get; set; } = 10;
        private int m_linkWidth = -1;

        private int m_ErrorCount = 0;
        private List<string> m_errMsgs = new List<string>();
        private bool m_inhibitSequenceProcessing = true;

        private int m_BSSequenceCount = 0;

        private List<string> m_testInfo = new List<string>();
        private uint m_BSSequenceStartStateIndex = 0x00;
        private uint m_curStateIndex = 0x00;

        private bool m_verticalBlanking = false;

        #endregion // Members

        #region Constructor(s)
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public VBID_NoVideo_Blanking_Tests()
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
            m_inhibitSequenceProcessing = true;  // haven't seen the first BS sequence nor the first VBID...

            m_testInfo.Clear();
            m_errMsgs.Clear();

            m_BSSequenceStartStateIndex = 0;
            m_verticalBlanking = false;
        }


        /// <summary>
        /// Create a set strings to be used in the detailed report.
        /// </summary>
        /// <returns></returns>
        private string getTestStatus()
        {
            string status = string.Empty;

            if (m_ErrorCount == 0)
            {
                status = "  Passed: No errors were encountered.";
            }
            else
            {
                status = "  Failed: Unexpected Pixel States Encountered.";
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
        private void processBSSequence(byte[] stateData, string laneName)
        {
            int lane0Data = m_commonFuncs.GetFieldData(stateData, laneName);
            if (lane0Data > -1)
            {
                if ((lane0Data == 0x1BC) || (lane0Data == 0x17C))
                {
                    if ((m_curStateIndex - m_BSSequenceStartStateIndex) >= 4)
                    {
                        m_BSSequenceStartStateIndex = m_curStateIndex;
                        m_BSSequenceCount += 1;
                    }

                    m_inhibitSequenceProcessing = false;  // reset once we've prcessed a faulty line!
                }
            }
        }


        /// <summary>
        /// Process the lane data for BS or BE control characters.
        /// </summary>
        /// <param name="laneName"></param>
        /// Evaluates Lane 0's EC to determine when a VBID state is processed;
        /// at which point, the code will extract the VerticalBlanking_Flag and 
        /// NoVideoStream_Flag bits... if either is set to '1', then the function 
        /// will flag any ensuing states with an EventCode of 'Pixel' as an error!
        /// 
        /// The method will only examine data in lane 0... because the other lanes are 
        /// just more of the same!
        /// 
        /// ***** Change the logic to expect a Pixel state AFTER each and every Blanking 
        /// ***** End event code... covers all three variants that Ed and Greg have seen
        /// ***** while testing.
        private bool processLane(byte[] stateData, string laneName)
        {
            bool status = true;
            

            // increment a counter to keep track of the number of BS seqeunces we've seen.
            processBSSequence(stateData, laneName);         // resets the m_inhibitSequenceProcessing flag

            string ECName = m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode"));
            if (ECName.EndsWith("VBID"))
            {
                int lane0Data = m_commonFuncs.GetFieldData(stateData, "Lane0");
                if ((lane0Data & 0x01) == 0x01)
                {
                    m_verticalBlanking = true;
                }
                else
                {
                    m_verticalBlanking = false;
                }
            }
            else
            {
                if ((m_verticalBlanking == true) && (m_inhibitSequenceProcessing == false))
                {
                    if ((ECName == "Hor. BE") || (ECName == "Ver. BE"))
                    {
                        updateErrorMessages("Unexpected Blanking End Encountered.");
                        m_inhibitSequenceProcessing = true;
                    }
                    else if ((ECName == "F0 Pixel") || (ECName == "F1 Pixel"))
                    {
                        updateErrorMessages("Unexpected Pixel Encountered.");
                        m_inhibitSequenceProcessing = true;
                    }
                    else if ((ECName == "F0 Stuff") || (ECName == "F1 Stuff")) 
                    {
                        updateErrorMessages("Unexpected Active Dummy (Stuffed) Encountered.");
                        m_inhibitSequenceProcessing = true;
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

            description.Add(" This test verifies blank lines do not contain any pixel states.");
            description.Add(" ");
            description.Add(" The test extracts the Vertical_Blanking_Flag and No_Video bits from");
            description.Add(" each encountered VBID (lane 0 only).  The class verifies the blanking");
            description.Add(" section does not contain any pixel states for which either VBID sub-field");
            description.Add(" the current BS sequence.");
            description.Add(" ");
            description.Add(" This test only operates on lane 0.");

            return description;
        }

        #endregion // Public Methods
    }
}

