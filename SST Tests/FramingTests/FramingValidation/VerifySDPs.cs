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
    class VerifySDPs
    {
        private class EncounteredPkts
        {
            public string ECName { get; set; } = "";
            public int Count { get; set; } = 0;

            public EncounteredPkts(string name, int count)
            {
                ECName = name;
                Count = count;
            }

            public EncounteredPkts(string name)
            {
                ECName = name;
                Count = 1;
            }
        }


        #region Members
        private int MaxErrors { get; set; } = 10;

        private bool m_errorEncountered = false;
        private List<string> m_errMsgs = new List<string>();
        private bool m_inhibitChecking = false;

        private List<string> m_testInfo = new List<string>();
        private uint m_curStateIndex = 0x00;

        private int m_linkWidth = -1;

        private ML_Common_Functions m_commonFuncs = null;

        private int m_ErrorCount = 0;

        private uint[] m_SS_Index = new uint[4];
        private uint[] m_SS_Count = new uint[4];
        private uint[] m_SE_Count = new uint[4];
        private bool[] m_inMSA = new bool[4];

        private int[] m_previousEventCode = new int[4];
        private int[] m_currentEventCode = new int[4];

        private List<EncounteredPkts> m_encounteredPkts = null;
        #endregion Members

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public VerifySDPs()
        {
            m_commonFuncs = new ML_Common_Functions();
        }

        #endregion Constructor

        #region Private Methods

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
        /// Add a count to the packet counter
        /// </summary>
        /// <param name="ECName"></param>
        private void incrementPktCount(string ECName)
        {
            bool found = false;
            for (int i = 0; i < m_encounteredPkts.Count; i++)
            {
                if (m_encounteredPkts[i].ECName == ECName)
                {
                    m_encounteredPkts[i].Count += 1;
                    found = true;
                    break;
                }
            }

            if (!found)
                m_encounteredPkts.Add(new EncounteredPkts(ECName));
        }



        ///// <summary>
        ///// Reset the counts!
        ///// </summary>
        //private void clearCounts()
        //{
        //    Array.Clear(m_inMSA, 0, m_inMSA.Length);
        //    Array.Clear(m_SS_Count, 0, m_SS_Count.Length);
        //    Array.Clear(m_SE_Count, 0, m_SE_Count.Length);
        //}




        /// <summary>
        /// Process the lane data for BS or BF control characters.
        /// </summary>
        /// <param name="laneName"></param>
        /// 
        /// 
        ///   SDP Index                            SDP Index              SDP Index
        ///   |                                    |                      |
        ///   |   +------+------+------+------+    |  +------+------+     | +------+
        ///   |   | SS   | SS   | SS   | SS   |    |  | SS   | SS   |     |  | SS   |
        ///   |   +-------------+------+------+    |  +------+------+     |  +------+
        ///   1   | HB0  | HB1  | HB2  | HB3  |    1  | HB0  | HB1  |     1  | HB0  |
        ///   2   | PB0  | PB1  | PB2  | PB3  |    2  | PB0  | PB1  |     2  | PB0  |
        ///   3   | DB0  | DB4  | DB8  | DB12 |    3  | HB2  | HB3  |     3  | HB1  |
        ///   4   | DB1  | DB5  | DB9  | DB13 |    4  | PB2  | PB3  |     4  | PB1  |
        ///   5   | DB2  | DB6  | DB10 | DB14 |    5  | DB0  | DB4  |     5  | HB2  |
        ///   6   | DB3  | DB7  | DB11 | DB15 |    6  | DB1  | DB5  |     6  | PB2  |
        ///   7   | PB4  | PB5  | PB6  | PB7  |    7  | DB3  | DB6  |     7  | HB3  |
        ///   8   | DB16 | DB20 | DB24 | DB28 |    8  | DB4  | DB7  |     8  | PB3  |
        ///   9   | DB17 | DB21 | DB25 | DB29 |    9  | PB4  | P15  |     9  | DB0  |
        ///   10  | DB18 | DB22 | DB26 | 0's  |    10 | DB8  | DB12 |     10 | DB1  |
        ///   11  | DB19 | DB23 | DB27 | 0's  |    11 | DB9  | DB13 |     11 | DB2  |
        ///   12  | DB19 | DB23 | PB10 | PB11 |    12 | DB10 | DB14 |     12 | DB3  |
        ///       +------+------+------+------+    13 | DB11 | DB15 |     13 | PB4  |
        ///       | SE   | SE   | SE   | SE   |    14 | PB6  | PB7  |     14 | DB4  |
        ///       +------+------+------+------+    15 | DB16 | DB20 |     15 | DB5  |
        ///                                        16 | DB17 | DB21 |     16 | DB6  |
        ///                                        17 | DB18 | DB22 |     17 | DB7  |
        ///                                        18 | DB19 | DB23 |     17 | PB5  |
        ///                                        19 | PB8  | PB9  |     18 | DB8  |      
        ///                                        20 | DB24 | DB28 |     20 | DB9  |       
        ///                                        21 | DB25 | DB29 |     21 | DB10 |       
        ///                                        22 | DB26 | 0's  |     22 | DB11 |        
        ///                                        23 | DB27 | 0's  |     23 | PB6  |   
        ///                                        24 | PB10 | PB11 |     24 | DB12 | 
        ///                                           +------+------+     25 | DB13 | 
        ///                                           | SE   | SE   |     26 | DB14 |  
        ///                                           +------+------+     27 | DB15 |        
        ///                                                               28 | PB7  | 
        ///                                                               29 | DB16 |       
        ///                                                               30 | DB17 |        
        ///                                                               31 | DB18 |        
        ///                                                               32 | DB19 |       
        ///                                                               33 | PB8  |           
        ///                                                               34 | DB20 |       
        ///                                                               35 | DB21 |        
        ///                                                               36 | DB22 |        
        ///                                                               37 | DB23 | 
        ///                                                               38 | PB9  |           
        ///                                                               39 | DB24 |       
        ///                                                               40 | DB25 |        
        ///                                                               41 | DB26 |        
        ///                                                               42 | DB27 | 
        ///                                                               43 | PB10 |           
        ///                                                               44 | DB28 |       
        ///                                                               45 | DB29 |        
        ///                                                               46 | 0's  |        
        ///                                                               47 | 0's  | 
        ///                                                               48 | PB11 |
        ///                                                                  +------+
        ///                                                                  | SE   |
        ///                                                                  +------+                                                            
        private bool processLane(byte[] stateData, string laneName)
        {
            bool status = true;

            int laneData = m_commonFuncs.GetFieldData(stateData, laneName);
            int laneIndex = getLaneIndex(laneName);


            // keep track of the current and previous state's event codes;
            // used to detect missing SE's...
            m_previousEventCode[laneIndex] = m_currentEventCode[laneIndex];
            m_currentEventCode[laneIndex] = m_commonFuncs.GetFieldData(stateData, "EventCode");


            if (laneData == 0x15C)                                                  // SS Ctrl Char
            {
                m_SS_Count[laneIndex] += 1;
                if (m_SS_Count[laneIndex] == 1)
                {
                    m_SS_Index[laneIndex] = m_curStateIndex;
                }
                else if (m_SS_Count[laneIndex] == 2)
                {
                    if (m_SS_Index[laneIndex] == (m_curStateIndex - 1))
                    {
                        m_inMSA[laneIndex] = true;
                    }
                    else
                    {
                        updateErrorMessages("Multiple SS Symbols");
                        m_inhibitChecking = true;
                    }
                }
                else// if (m_SS_Count[laneIndex] > 2)
                {
                    updateErrorMessages("Three or more SS Symbols");
                    m_inhibitChecking = true;
                }
            }
            else if (laneData == 0x1FD)                                             // SE Ctrl Char
            {
                if (!m_inhibitChecking)
                {
                    m_SE_Count[laneIndex] += 1;
                    if (!m_inMSA[laneIndex])
                    {
                        if ((m_SS_Count[laneIndex] == 1) && (m_SE_Count[laneIndex] == 1))
                        {
                            if (laneIndex == 0)
                            {
                                // we could get the HB1 value... but this is easier for now...
                                incrementPktCount(m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode")));
                            }
                        }
                        else //if ((m_SS_Count[laneIndex] != 1) || (m_SE_Count[laneIndex] != 1))
                        {
                            updateErrorMessages("Invalid Number of SS and/or SE Symbols");
                            m_inhibitChecking = true;
                        }
                    }
                    else // m_inMSA == true
                    {
                        if ((m_SS_Count[laneIndex] == 2) || (m_SE_Count[laneIndex] == 1))
                        {
                            if (laneIndex == 0)
                            {
                                // we could get the HB1 value... but this is easier for now...
                                incrementPktCount(m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode")));
                            }
                        }
                        else
                        {
                            updateErrorMessages("Invalid Number of MSA SS/SE Symbols");
                            m_inhibitChecking = true;
                        }
                    }
                }
                m_SS_Count[laneIndex] = 0;
                m_SE_Count[laneIndex] = 0;
                m_inMSA[laneIndex] = false;
            }
            else
            {
                if (!m_inhibitChecking)
                {
                    if (m_previousEventCode[laneIndex] != m_currentEventCode[laneIndex])
                    {
                        if (((m_SS_Count[laneIndex] == 0) && (m_SE_Count[laneIndex] > 0)) ||
                            ((m_SS_Count[laneIndex] > 0) && (m_SE_Count[laneIndex] == 0)))
                        {
                            updateErrorMessages("Missing SS or SE Symbol");
                            m_inhibitChecking = true;
                        }

                        //if (laneIndex == (m_linkWidth - 1))
                        //{
                        //    m_inhibitChecking = false;
                        //    Array.Clear(m_SS_Index, 0, m_SS_Index.Length);
                        //    Array.Clear(m_SS_Count, 0, m_SS_Count.Length);
                        //    Array.Clear(m_SE_Count, 0, m_SE_Count.Length);
                        //    Array.Clear(m_inMSA, 0, m_inMSA.Length);
                        //}
                    }
                }
                //else
                //{
                //    if (m_previousEventCode[laneIndex] != m_currentEventCode[laneIndex])
                //    {
                //        if (laneIndex == (m_linkWidth - 1))
                //        {
                //            m_inhibitChecking = false;
                //            Array.Clear(m_SS_Index, 0, m_SS_Index.Length);
                //            Array.Clear(m_SS_Count, 0, m_SS_Count.Length);
                //            Array.Clear(m_SE_Count, 0, m_SE_Count.Length);
                //            Array.Clear(m_inMSA, 0, m_inMSA.Length);
                //        }
                //    }
                //}

                if (m_previousEventCode[laneIndex] != m_currentEventCode[laneIndex])
                {
                    if (laneIndex == (m_linkWidth - 1))
                    {
                        m_inhibitChecking = false;
                        Array.Clear(m_SS_Index, 0, m_SS_Index.Length);
                        Array.Clear(m_SS_Count, 0, m_SS_Count.Length);
                        Array.Clear(m_SE_Count, 0, m_SE_Count.Length);
                        Array.Clear(m_inMSA, 0, m_inMSA.Length);
                    }
                }
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

            Array.Clear(m_SS_Index, 0, m_SS_Index.Length);
            Array.Clear(m_SS_Count, 0, m_SS_Count.Length);
            Array.Clear(m_SE_Count, 0, m_SE_Count.Length);
            Array.Clear(m_inMSA, 0, m_inMSA.Length);

            Array.Clear(m_previousEventCode, 0, m_previousEventCode.Length);
            Array.Clear(m_currentEventCode, 0, m_currentEventCode.Length);

            m_inhibitChecking = false;
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
            m_encounteredPkts = new List<EncounteredPkts>();
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
                    if (m_linkWidth == 1)
                    {
                        status = processLane(stateData, "Lane0");
                    }
                    else if (m_linkWidth == 2)
                    {
                        status = processLane(stateData, "Lane0");
                        if (status)
                            status = processLane(stateData, "Lane1");
                    }
                    else // if (m_linkWidth == 4)
                    {
                        // all checking assumes Lane0 ALWAYS has data...
                        status = processLane(stateData, "Lane0");
                        if (status)
                            status = processLane(stateData, "Lane1");
                        if (status)
                            status = processLane(stateData, "Lane2");
                        if (status)
                            status = processLane(stateData, "Lane3");
                    }

                    //// all checking assumes Lane0 ALWAYS has data...
                    //status = processLane(stateData, "Lane0");
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
            for (int i = 0; i < m_encounteredPkts.Count; i++)
            {
                subStrings.Add(m_encounteredPkts[i].ECName + ": " + m_encounteredPkts[i].Count);
            }

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

            description.Add(" This test verifies that all the SDP packets begin with an");
            description.Add(" SS control character and end with an SE control character.");
            description.Add(" ");

            return description;
        }
        #endregion Public Methods
    }
}
