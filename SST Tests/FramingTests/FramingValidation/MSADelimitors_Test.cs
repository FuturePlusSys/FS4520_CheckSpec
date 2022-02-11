using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_FramingValidation
{
    /// <summary>
    /// This class verifies the Main Stream Attribute secondary data packet 
    /// begins with two sequencial states containing SS control chars in 
    /// all active data lanes, and ends with and SE control character in 
    /// all active data lanes.
    /// </summary>
    class MSADelimitors_Test
    {
        #region Members

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


        private ML_Common_Functions m_commonFuncs = null;

        private int m_linkWidth = -1;
        private int MaxErrors { get; set; } = 10;

        private int m_ErrorCount = 0;
        private List<string> m_errMsgs = new List<string>();
        private List<string> m_testInfo = new List<string>();

        private uint m_curStateIndex = 0x00;

        private int m_MSACount = 0x00;



        private bool m_inhibitChecking = false;

        private uint[] m_SS_Index = new uint[4];
        private uint[] m_SS_Count = new uint[4];
        private uint[] m_SE_Count = new uint[4];
        private bool[] m_inMSA = new bool[4];

        private int[] m_previousEventCode = new int[4];
        private int[] m_currentEventCode = new int[4];


        private List<EncounteredPkts> m_encounteredPkts = null;


        #endregion // Members

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public MSADelimitors_Test()
        {
            m_commonFuncs = new ML_Common_Functions();
        }

        #region Constructor(s)
        #endregion // Constructor(s)

        #region Private Methods

        /// <summary>
        /// Reset class members to default values.
        /// </summary>
        private void resetMembers()
        {
            m_MSACount = 0;
            m_ErrorCount = 0;
            m_curStateIndex = 0x00;

            m_testInfo.Clear();
            m_errMsgs.Clear();

            m_inhibitChecking = false;

            Array.Clear(m_SS_Index, 0, m_SS_Index.Length);
            Array.Clear(m_SS_Count, 0, m_SS_Count.Length);
            Array.Clear(m_SE_Count, 0, m_SE_Count.Length);
            Array.Clear(m_inMSA, 0, m_inMSA.Length);

            Array.Clear(m_previousEventCode, 0, m_previousEventCode.Length);
            Array.Clear(m_currentEventCode, 0, m_currentEventCode.Length);
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
        /// Update the error message and error count
        /// </summary>
        /// <param name="errorMsg"></param>
        private void updateErrorMessages(string errorMsg)
        {
            m_errMsgs.Add(errorMsg);
            m_ErrorCount += 1;
        }


        /// <summary>
        /// Verify all data lanes contain the SS symbol value (0x15c)
        /// </summary>
        /// <param name="dataLanes"></param>
        /// <returns></returns>
        private bool dataLanesContainSymbol_SS(List<int> dataLanes)
        {
            bool status = false;

            if (m_linkWidth == 1)
            {
                if (dataLanes[0] == 0x15C)
                    status = true;
            }
            else if (m_linkWidth == 2)
            {
                if ((dataLanes[0] == 0x15C) && (dataLanes[1] == 0x15C))
                    status = true;
            }
            else //if (m_linkWidth == 4)
            {
                if ((dataLanes[0] == 0x15C) && (dataLanes[1] == 0x15C) && (dataLanes[2] == 0x15C) && (dataLanes[3] == 0x15C))
                    status = true;
            }


            return status;
        }


        /// <summary>
        /// Verify all data lanes contain the SS symbol value (0x15c)
        /// </summary>
        /// <param name="dataLanes"></param>
        /// <returns></returns>
        private bool dataLanesContainSymbol_SE(List<int> dataLanes)
        {
            bool status = false;

            if (m_linkWidth == 1)
            {
                if (dataLanes[0] == 0x1FD)
                    status = true;
            }
            else if (m_linkWidth == 2)
            {
                if ((dataLanes[0] == 0x1FD) && (dataLanes[1] == 0x1FD))
                    status = true;
            }
            else //if (m_linkWidth == 4)
            {
                if ((dataLanes[0] == 0x1FD) && (dataLanes[1] == 0x1FD) && (dataLanes[2] == 0x1FD) && (dataLanes[3] == 0x1FD))
                    status = true;
            }


            return status;
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


        /// <summary>
        /// Process the lane data for BS or BF control characters.
        /// </summary>
        /// <param name="laneName"></param>
        /// 
        /// 
        /// 
        ///   m_MSAStateIndex                                                m_MSAStateIndex                    m_MSAStateIndex 
        ///   |                                                              |                                  |
        ///   |   +-------------+-------------+------------+------------+    |   +------------+------------+    |   +------------+
        ///   |   |    SS       |     SS      |    SS      |    SS      |    |   |    SS      |     SS     |    |   |    SS      | 
        ///   |   |    SS       |     SS      |    SS      |    SS      |    |   |    SS      |     SS     |    |   |    SS      |
        ///   |   +-------------+-------------+------------+------------+    |   +------------+------------+    |   +------------+
        ///   1   | Mvid23:16   | Mvid23:16   | Mvid23:16  | Mvid23:16  |    1   | Mvid23:16  | Mvid23:16  |    1   | Mvid23:16  |
        ///   2   | Mvid15:8    | Mvid15:8    | Mvid15:8   | Mvid15:8   |    2   | Mvid15:8   | Mvid15:8   |    2   | Mvid15:8   |
        ///   3   | Mvid7:0     | Mvid7:0     | Mvid7:0    | Mvid7:0    |    3   | Mvid7:0    | Mvid7:0    |    3   | Mvid7:0    |
        ///   4   | Htotal15:8  | Hstart15:8  | Hwidth15:8 | Nvid23:16  |    4   | Htotal15:8 | Hstart15:8 |    4   | Htotal15:8 |
        ///   5   | Htotal7:0   | Hstart7:9   | Hwidth7:8  | Nvid15:8   |    5   | Htotal7:0  | Hstart7:9  |    5   | Htotal7:0  |
        ///   6   | Vtotal15:8  | Vstart15:8  | Hheight15:8| Nvid7:0    |    6   | Vtotal15:8 | Vstart15:8 |    6   | Vtotal15:8 |
        ///   7   | Vtotal7:0   | Vstart17:0  | Hheight7:0 | MISC0_7:0  |    7   | Vtotal7:0  | Vstart17:0 |    7   | Vtotal7:0  |
        ///   8   | HSP|HSW14:8 | VSP|VSW14:8 | All 0's    | MISC1_7:0  |    8   | HSP|HSW14:8| VSP|VSW14:8|    8   | HSP|HSW14:8|
        ///   9   | HSW7:0      | VSW7:0      | All 0's    | All 0's    |    9   | HSW7:0     | VSW7:0     |    9   | HSW7:0     |
        ///       +-------------+-------------+------------+------------+    10  | Mvid23:16  | Mvid23:16  |    10  | Mvid23:16  |
        ///       |    SE       |     SE      |    SE      |    SE      |    11  | Mvid15:8   | Mvid15:8   |    11  | Mvid15:8   |    
        ///       +-------------+-------------+------------+------------+    12  | Mvid7:0    | Mvid7:0    |    12  | Mvid7:0    |
        ///                                                                  13  | Hwidth15:8 | Nvid23:16  |    13  | Hstart15:8 |
        ///                                                                  14  | Hwidth7:8  | Nvid15:8   |    14  | Hstart7:9  |
        ///                                                                  15  | Hheight15:8| Nvid7:0    |    15  | Vstart15:8 |
        ///                                                                  16  | Hheight7:0 | MISC0_7:0  |    16  | Vstart17:0 | 
        ///                                                                  17  | All 0's    | MISC1_7:0  |    17  | VSP|VSW14:8|
        ///                                                                  18  | All 0's    | All 0's    |    18  | VSW7:0     |
        ///                                                                      +------------+------------+    19  | Mvid23:16  | 
        ///                                                                      |    SE      |    SE      |    20  | Mvid15:8   |
        ///                                                                      +------------+------------+    21  | Mvid7:0    |
        ///                                                                                                     22  | Hwidth15:8 |
        ///                                                                                                     23  | Hwidth7:8  |
        ///                                                                                                     24  | Hheight15:8|
        ///                                                                                                     25  | Hheight7:0 |
        ///                                                                                                     26  | All 0's    |
        ///                                                                                                     27  | All 0's    |
        ///                                                                                                     28  | Mvid23:16  |
        ///                                                                                                     29  | Mvid15:8   |
        ///                                                                                                     30  | Mvid7:0    |
        ///                                                                                                     31  | Nvid23:16  |
        ///                                                                                                     32  | Nvid15:8   |
        ///                                                                                                     33  | Nvid7:0    |
        ///                                                                                                     34  | MISC0_7:0  |
        ///                                                                                                     35  | MISC1_7:0  |
        ///                                                                                                     36  | All 0's    | 
        ///                                                                                                         +------------+
        ///                                                                                                         |    SE      | 
        ///                                                                                                         +------------+
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
                        if (laneIndex == 0)
                            m_MSACount += 1;
                    }
                    else
                    {
                        updateErrorMessages("Non-Sequencial MSA SS Symbols");
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
                    if (m_inMSA[laneIndex])
                    {
                        m_SE_Count[laneIndex] += 1;                                 // checks for multiple SE's
                        if (m_SE_Count[laneIndex] != 1)
                        {
                            updateErrorMessages("Multiple MSA SE Symbols");
                            m_inhibitChecking = true;
                        }
                    }
                }

                m_inhibitChecking = false;
                m_SS_Count[laneIndex] = 0;
                m_SE_Count[laneIndex] = 0;
                m_inMSA[laneIndex] = false;
            }
            else                                                                    // Middle of the MSA pkt or some other area of the listing...
            {
                if (m_previousEventCode[laneIndex] != m_currentEventCode[laneIndex])
                {
                    if (m_inMSA[laneIndex])
                    {
                        if (m_SE_Count[laneIndex] == 0)                             // checkes for missing SE's
                        {
                            updateErrorMessages("Missing SE Symbol");
                        }
                    }                   

                    m_inhibitChecking = false;
                    m_SS_Count[laneIndex] = 0;
                    m_SE_Count[laneIndex] = 0;
                    m_inMSA[laneIndex] = false;
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
            subStrings.Add("Number Of MSA SDPs Encountered: " + m_MSACount.ToString());

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

            description.Add(" This test verifies the Main Stream Attribute secondary data packet");
            description.Add(" begins with two sequencial states containing SS control characters in");
            description.Add(" all active data lanes, and ends with an SE control character in");
            description.Add(" all active data lanes.");

            return description;
        }
        #endregion // Public Methods
    }
} 