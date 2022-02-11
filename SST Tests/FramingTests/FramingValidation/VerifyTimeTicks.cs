using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_FramingValidation
{
    /// <summary>
    /// This class verifies that all the SDP packets had a SS in the beginning and a SE in the end
    /// 
    /// Errors are reported if the Event code changes from an SDP to any else and an SS and SE were not found.
    /// Also reported if an SE is found before an SS.
    /// Reported if SE is found twice within same packet.
    /// 
    /// This class will only check Lane 0 for SS and SE.
    /// </summary>
    class VerifyTimeTicks
    {
        
        #region Members
        private int MaxErrors { get; set; } = 10;

        private List<string> m_errMsgs = new List<string>();
        private int m_ErrorCount = 0;

        private List<string> m_testInfo = new List<string>();
        private uint m_curStateIndex = 0x00;

        private int m_linkWidth = -1;

        private ML_Common_Functions m_commonFuncs = null;

        private long  m_previousTicks = 0x00;
        private long m_currentTicks = 0x00;

        #endregion Members

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public VerifyTimeTicks()
        {
            m_commonFuncs = new ML_Common_Functions();
        }

        #endregion Constructor

        #region Private Methods


        /// <summary>
        /// Process the lane data for BS or BF control characters.
        /// </summary>
        /// <param name="laneName"></param>
        private bool processLane(byte[] stateData)
        {
            bool status = true;
            m_currentTicks = m_commonFuncs.GetTimeTicks(stateData);

            if (m_previousTicks != (m_currentTicks - 1))
            {
                if (m_previousTicks != -1)
                {
                    updateErrorMessages("Time Tick Error");
                }
            }
            else
            {
            }
            m_previousTicks = m_currentTicks;

            //    m_currentTicks - 1)
            //{
            //    if (m_previousTicks != -1)
            //    {
            //        updateErrorMessages("Time Tick Error!");
            //    }
            //}

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

            m_previousTicks = -1;
            m_currentTicks = -1;
        }

        #endregion Private Methods

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
                m_errMsgs.Clear();

                if (m_ErrorCount < MaxErrors)
                {
                    processLane(stateData);
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

            description.Add(" This test verifies time ticks are consecutive.");
            description.Add(" ");

            return description;
        }

        #endregion Public Methods
    }
}
