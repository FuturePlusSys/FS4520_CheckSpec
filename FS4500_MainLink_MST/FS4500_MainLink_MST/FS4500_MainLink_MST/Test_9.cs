using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS4500_MainLink_MST
{
    internal class Test_9
    {
        #region Members

        // Test 9 Variables
        List<string> m_errMsgs = new List<string>();

        private int m_curStateIndex = 0;
        private int m_groupingCount = -1;
        private int m_groupingStartIndex = -1;
        private int m_SRCount = 0;
        private int m_BSCount = 0;


        #endregion // Members

        #region Constructor(s)

        public Test_9()
        {
        }

        #endregion // Constructor(s)

        #region Private Methods
        #endregion // Private Methods

        #region Public Methods

        public bool Initialize()
        {
            // Test 8 Variables
            m_errMsgs = new List<string>();

            m_curStateIndex = 0;
            m_groupingCount = -1;
            m_groupingStartIndex = -1;
            m_SRCount = 0;
            m_BSCount = 0;

            return true;
        }


        /// <summary>
        /// Process the given state.
        /// </summary>
        /// <param name="stateData"></param>
        /// <returns></returns>
        public List<string> ProcessState(params byte[] stateData)
        {
            m_errMsgs.Clear();
            m_curStateIndex++;


            int eventCode = (byte)(stateData[9] >> 2);
            if (eventCode == 0x0B) // Horizontal or vertical SR Event Code
            {
                if (m_groupingCount == 0)
                {
                    m_groupingStartIndex = m_curStateIndex;
                    m_groupingCount += 1;

                    m_SRCount += 1;
                    m_BSCount = 0;
                }
                else if (m_groupingCount < 4)
                {
                    m_groupingCount += 1;
                }
                else // grouping count >= 4
                {
                    m_groupingCount += 1;  // mainly for trouble shooting.
                    m_errMsgs.Add("SR Symbol Grouping Error.");
                }
            }
            else if (eventCode == 0x0A) // Horizontal or vertical BS Event Code
            {
                if (m_groupingCount == 0)
                {
                    m_groupingCount += 1;
                    m_BSCount += 1;
                }
                else if (m_groupingCount < 4)
                {
                    m_groupingCount += 1;
                }
                else // grouping count >= 4
                {
                    m_groupingCount += 1;  // mainly for trouble shooting.
                    m_errMsgs.Add("BS Symbol Grouping Error.");
                }
            }
            else
            {
                if (m_groupingCount > 0)
                    m_groupingCount = 0;
            }


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

            return stats;
        }



        /// <summary>
        /// Return testual explaination of what the test is doing..
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestDescription()
        {
            List<string> description = new List<string>();

            description.Add(" TBD... ");
            description.Add(" ");

            return description;
        }
        #endregion // Public Methods
    }
}
