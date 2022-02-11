using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS4500_MainLink_MST
{
    internal class Test_11
    {
        #region Members

        // Test 9 Variables
        List<string> m_errMsgs = new List<string>();

        private bool m_BEProcessed = false;

        public int CurrentStateIndex { get; set; } = 0;


        #endregion // Members

        #region Constructor(s)

        public Test_11()
        {
        }

        #endregion // Constructor(s)

        #region Private Methods
        #endregion // Private Methods

        #region Public Methods

        public bool Initialize(List<string> ConfigParameters)
        {
            // Test 8 Variables
            m_errMsgs = new List<string>();
            m_BEProcessed = false;
            CurrentStateIndex = 0;

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


            int eventCode = (byte)(stateData[9] >> 2);

            CurrentStateIndex += 1;
            if (m_BEProcessed == false)
            {
                if (eventCode == 0x15) // Horizontal or vertical BE Event Code
                {
                    m_BEProcessed = true;
                }
            }
            else
            {
                if (m_BEProcessed == true)
                {
                    if (eventCode != 0x08)  // F0 or F1 Pixel
                        m_errMsgs.Add("Pixel Does Not Immediately Follow BE Symbol.");

                    m_BEProcessed = false;
                }
            }

            return m_errMsgs;
            #endregion // Public Methods
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
            subStrings.Add("Number Of States Processed: " + String.Format("{0:n0}", CurrentStateIndex + 1));

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
    }
}
