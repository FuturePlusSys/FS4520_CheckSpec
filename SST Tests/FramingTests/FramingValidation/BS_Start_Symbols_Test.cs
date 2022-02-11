using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_FramingValidation
{
    /// <summary>
    /// This class verifies the four state blanking start control characters sequences.
    /// 
    /// The class verifies the data lane value for all active lanes (link width) and
    /// ensures all lanes contain the same value.
    /// 
    /// Additionally, the sequence of control characters i.e. BS, BF, BF, BS, is verified.
    /// 
    /// The Class verifies SR's and BS's.
    /// 
    /// The Class DOES NOT verify every 512 BS is replaced with SR..
    /// The Class DOES NOT verify if the 1st and 4th symbols (BS/SR) are equal to one another.
    /// </summary>
    public class BS_Start_Symbols_Test
    {
        #region Members

        private const int CtrlCharValue_SR = 0x11C;
        private const int CtrlCharValue_BS = 0x1BC;
        private const int CtrlCharValue_BF = 0x17C;

        private ML_Common_Functions m_commonFuncs = null;

        private int MaxErrors { get; set; } = 10;
        private List<string> m_errMsgs = new List<string>();
        private int m_ErrorCount = 0;

        private List<string> m_testInfo = new List<string>();
        private uint m_curStateIndex = 0x00;

        private int m_linkWidth = -1;
        private List<int> m_dataLanes = null;

        private int m_BSSequenceCount = 0;
        private int m_startSequenceCount = 0x00;
        private bool m_ErrorDetected = false;
        private bool m_inBlankingStartSequence = false;

        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public BS_Start_Symbols_Test()
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

            if (m_ErrorCount == 0 )
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
            m_ErrorCount = 0;
            m_ErrorDetected = false;

            m_errMsgs.Clear();
            m_testInfo.Clear();

            m_curStateIndex = 0x00;
            m_testInfo.Clear();
            m_errMsgs.Clear();
            m_BSSequenceCount = 0;
            m_inBlankingStartSequence = false;

            m_startSequenceCount = 0x00;
        }



        /// <summary>
        /// Process the lane data for BS or BF control characters.
        /// </summary>
        /// <param name="laneName"></param>
        //private bool processLane(byte[] stateData, string laneName)
        private bool processState(byte[] stateData) //, string laneName)
        {
            bool status = true;

            //
            // First... get all active data lane values
            //
            for (int i = 0; i < m_linkWidth; i++)
            {
                m_dataLanes = m_commonFuncs.GetLanesData(stateData); 
                if (m_inBlankingStartSequence == false)
                {
                    // check for all three control characters to ensure we catch out of order sequences...
                    // Check all data lanes...
                    if ((m_dataLanes[i] == CtrlCharValue_SR) || (m_dataLanes[i] == CtrlCharValue_BS) || (m_dataLanes[i] == CtrlCharValue_BF))
                    {
                        m_inBlankingStartSequence = true;
                        m_BSSequenceCount += 1;
                    }
                }
            }


            // 
            // Second -- is this a blanking start control character
            //
            if (m_inBlankingStartSequence == true) 
            {
                // this is the first validity check... all data lanes have the same value.
                if (m_linkWidth > 1)
                {
                    for (int i = 1; i < m_linkWidth; i++)
                    {
                        if (m_dataLanes[i - 1] != m_dataLanes[i])
                        {
                            //status = false;
                            m_ErrorDetected = true;
                            updateErrorMessages("Data Lanes contain different Control Characters");
                        }
                    }
                }
            }


            //
            // Third -- verify the expected control character is contained in all active data lanes
            //
            if (m_inBlankingStartSequence)
            {
                if (!m_ErrorDetected)
                {
                    //
                    // determine if lane 0 has a Blanking Start control symbol.
                    //
                    if ((m_startSequenceCount == 0) || (m_startSequenceCount == 3))
                    {
                        m_startSequenceCount += 1;
                        if ((m_dataLanes[0] != CtrlCharValue_SR) && (m_dataLanes[0] != CtrlCharValue_BS))
                        {
                            status = false;
                            m_ErrorDetected = true;

                            updateErrorMessages("Invalid BS/SR Control Character");
                        }
                    }
                    else if ((m_startSequenceCount == 1) || (m_startSequenceCount == 2))
                    {
                        m_startSequenceCount += 1;
                        if ((m_dataLanes[0] != CtrlCharValue_BF))
                        {
                            status = false;
                            m_ErrorDetected = true;  // we are done with this blanking start section....

                            updateErrorMessages("Invalid BF Control Character");
                        }
                    }
                }
                else
                {
                    m_startSequenceCount += 1;

                    // don't clear out the status variables until we are passed the current sequence...
                    if (m_startSequenceCount > 4)
                    {
                        m_startSequenceCount = 0;
                        m_inBlankingStartSequence = false;
                    }
                }
            }

            if (m_startSequenceCount == 4)
            {
                m_startSequenceCount = 0;
                m_inBlankingStartSequence = false;
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
                    status = processState(stateData); 
                }
            }
            catch (Exception ex)
            {
                m_errMsgs.Add("Processing Error: " + ex.Message);
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

            description.Add(" This test verifies the four state blanking start control characters sequences.");
            description.Add(" ");
            description.Add("  The test verifies the data lane value for all active lanes (link width) and ");
            description.Add("  ensures all lanes contain the same value.   Additionally, the sequence of control ");
            description.Add("  characters i.e. BS, BF, BF, BS, is verified.");
            description.Add("  ");
            description.Add("  The test verifies SR's and BS's.");
            description.Add("  ");
            description.Add("  The test DOES NOT verify every 512 BS is replaced with SR.");
            description.Add("  The test DOES NOT verify if the 1st and 4th symbols (BS/SR) are equal to one another.");

            return description;
        }
    }

    #endregion // Public Methods
}

