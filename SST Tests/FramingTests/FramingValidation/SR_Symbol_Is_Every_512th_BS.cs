using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_FramingValidation
{
    /// <summary>
    /// This class verifies that every 512th BS sequence is replaced with an SR control character.
    /// 
    /// This class operates on lane 0 only.
    /// 
    /// </summary>
    class SR_Symbol_Is_Every_512th_BS
    {
        #region Members

        private ML_Common_Functions m_commonFuncs = null;

        private int MaxErrors { get; set; } = 10;
        private int m_linkWidth = -1;

        private int[] m_SRSequence = new int[] { 0x11c, 0x17c, 0x17c, 0x11c };
        private int m_SRSequenceIndex = -1;


        private bool m_errorEncountered = false;
        private List<string> m_errMsgs = new List<string>();
        private int m_ErrorCount = 0;

        private List<string> m_testInfo = new List<string>();
        private uint m_curStateIndex = 0x00;

        private int m_SR_Count = 0;
        private int m_BS_Count = 0;
        private uint m_prevBSStateIndex = 0;

        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public SR_Symbol_Is_Every_512th_BS()
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
            m_errorEncountered = false;
            m_ErrorCount = 0;
            m_curStateIndex = 0x00;
            m_testInfo.Clear();
            m_errMsgs.Clear();

            m_SR_Count = 0x00;
            m_BS_Count = 0x00;
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
                // expecting state sequence of... Scrambler Reset Symbol Sequence.
                // SR
                // BF
                // BF
                // SR

                if (lane0Data == 0x11C)  // SR Symbol
                {
                    m_SRSequenceIndex += 1;
                    if (m_SRSequenceIndex == 3)
                    {
                        if (m_SR_Count > 0)
                        {
                            if (m_BS_Count != 511)
                            {
                                updateErrorMessages("Invalid Number of BS Symbols between SR Symbols");
                            }
                        }
                        m_SR_Count += 1;
                        m_BS_Count = 0;
                    }
                    else if (m_SRSequenceIndex > 3)
                    {
                        updateErrorMessages("Invalid SR Symbol Sequence");
                    }
                }
                else if (lane0Data == 0x17C)  // BF Symbol 
                {
                    m_SRSequenceIndex += 1;
                    if (m_SRSequenceIndex > 3)
                    {
                        updateErrorMessages("Invalid SR Symbol Sequence");
                    } 
                }
                else if (lane0Data == 0x1BC)  // BS Symbol
                {
                    if (m_SR_Count > 0)  // must wait and process the first SR contained in the listing before we start counting BS symbols
                    {
                        if (m_curStateIndex > (m_prevBSStateIndex + 4) )  // only count 1 of the 4 symbol set.
                        {
                            m_BS_Count += 1;
                            m_prevBSStateIndex = m_curStateIndex;
                        }
                    } 
                }
                else
                {
                    if ( m_SRSequenceIndex > -1)
                        m_SRSequenceIndex = -1;
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
            subStrings.Add("Number Of SR Symbols Encountered: " + m_SR_Count.ToString());

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

            description.Add(" This test verifies that every 512th BS sequence is replaced with an SR control character.");
            description.Add("  ");
            description.Add("  This test operates on lane 0 only.");

            return description;
        }
        
        #endregion // Public Methods
    }
}
