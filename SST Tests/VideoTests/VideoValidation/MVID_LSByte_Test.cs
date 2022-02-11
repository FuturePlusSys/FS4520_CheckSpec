using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_VideoValidation
{

    /// <summary>
    /// This class verifies that the MVID state contains the same value as the 
    /// previous MSA MVid sub-field value when in synchronous clock mode.
    /// 
    /// The processed MVID value processes is always located in lane 0; as that
    /// sub-field always falls in this lane, regardless of the link width.  The 3 other
    /// MSBytes if the MVID sub-fields will be ingnored.
    /// 
    /// The processed MISC0, used to get sync/async mode, is the maximum data lane
    /// used in the link.  That is, in four lane mode, MISC0 is located in lane 3, 
    /// two lane mode, MISC0 is in lane 1 and 1 lane mode, MISC0 is in lane 0.
    ///
    /// The test reports the number of MSA seen in both async and sync modes.
    /// 
    /// The processing of the MVID values is always taken from lane 0.  
    /// MVID states that are encountered before an MSA Secondary Data Packet will be ingnored.
    /// 
    /// </summary>
    class MVID_LSByte_Test
    {
        #region Members

        private ML_Common_Functions m_commonFuncs = null;

        private int MaxErrors { get; set; } = 10;
        private int m_linkWidth = -1;


        private uint m_curStateIndex = 0x00;

        private int m_ErrorCount = 0;
        private List<string> m_errMsgs = new List<string>();
        private List<string> m_testInfo = new List<string>();

        private int m_MSACount = 0;
        private int m_MVidCount = 0;
        private int m_BSSequenceCount = 0;
        private uint m_BSStateIndex = 0;
        private int m_MSA_MVID_Value = -1;
        private int m_MISC0_Value = -1;
        private int m_synchronousClkModeBit = 0x00;


        private uint[] m_BSSequenceStartStateIndex = new uint[4]; 


        private bool[] m_inMSA = new bool[4];
        private int[] m_SS_CtrlChar_Count = new int[4]; 
        private uint[] m_SS_1_StateIndex = new uint[4]; 
        private uint[] m_SS_2_StateIndex = new uint[4]; 
        private bool m_inhibitChecking = false;
        private uint[] m_MSAStateIndex = new uint[4]; 
        private bool m_BeginBlankingStartSection = false;
       // private bool m_SS_CtrlCharVerified = false;


        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// default Constructor
        /// </summary>
        public MVID_LSByte_Test()
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
            m_MSACount = 0;
            m_MVidCount = 0;
            m_BSSequenceCount = 0;
            m_BSStateIndex = 0;

            m_errMsgs.Clear();
            m_testInfo.Clear();
            m_inhibitChecking = false;

            m_MSA_MVID_Value = -1;
            m_MISC0_Value = -1;
            m_synchronousClkModeBit = 0x00;

            //m_BeginBlankingStartSection = false;
            //m_SS_CtrlCharVerified = false;
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
                status = "  Failed: One or more errors were encountered.";
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
        /// Get the MVID[7:0] and MISC0 values...
        /// </summary>
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
        /// 
        /// <param name="stateData"></param>
        /// <returns></returns>
        private bool getMSASubFields(byte[] stateData, string laneName, int laneIndex)
        {
            bool status = true;


            // Mvid[7:0] is always in the same place, regardless of the link width... it just is!
            if (m_MSAStateIndex[laneIndex] == 3)
            {
                m_MSA_MVID_Value = m_commonFuncs.GetFieldData(stateData, "Lane0");
            }



            // MISC0 is always in the maximum active lane...
            switch (m_linkWidth)
            {
                case 1:
                    if (m_MSAStateIndex[laneIndex] == 34)
                    {
                        m_MISC0_Value = m_commonFuncs.GetFieldData(stateData, "Lane0");
                        m_synchronousClkModeBit = (m_MISC0_Value & 0x01);
                    }
                    break;

                case 2:
                    if ((m_MSAStateIndex[laneIndex] == 16) && (laneName == "Lane1"))
                    {
                        m_MISC0_Value = m_commonFuncs.GetFieldData(stateData, "Lane1");
                        m_synchronousClkModeBit = (m_MISC0_Value & 0x01);
                    }
                    break;

                case 4:
                    if ((m_MSAStateIndex[laneIndex] == 7) && (laneName == "Lane3"))
                    {
                        m_MISC0_Value = m_commonFuncs.GetFieldData(stateData, "Lane3");
                        m_synchronousClkModeBit = (m_MISC0_Value & 0x01);
                    }
                    break;

                default:
                    break;
            }

            return status;
        }


        /// <summary>
        /// Process the lane data for MVid state values.
        /// </summary>
        /// <param name="laneName"></param>
        private bool processLane(byte[] stateData, string laneName)
        {
            bool status = true;
            int laneData = m_commonFuncs.GetFieldData(stateData, laneName);
            int laneIndex = getLaneIndex(laneName);


            if (laneData == 0x15C)  // SS Ctrl Char
            {
                // no matter what, reset the checking capability -- until we see another SS control character.
                m_inhibitChecking = false;
                m_MSAStateIndex[laneIndex] = 0;
                m_SS_CtrlChar_Count[laneIndex] += 1;

                // remember the state index of each SS Ctrl Char
                if (m_SS_CtrlChar_Count[laneIndex] == 1)
                {
                    m_SS_1_StateIndex[laneIndex] = m_curStateIndex;
                }
                else if (m_SS_CtrlChar_Count[laneIndex] == 2)
                {
                    m_SS_2_StateIndex[laneIndex] = m_curStateIndex;
                    if (laneIndex == 0)
                        m_MSACount += 1;
                }

                if (m_SS_CtrlChar_Count[laneIndex] == 2)
                {
                    if (m_SS_2_StateIndex[laneIndex] == (m_SS_1_StateIndex[laneIndex] + 1))
                    {
                        m_inMSA[laneIndex] = true;
                    }
                    else
                    {
                        updateErrorMessages("SS Control Chars are not consecutive");
                        m_inhibitChecking = true;
                    }
                }
            }
            else if (laneData == 0x1FD)  // SE Ctrl Char
            {
                // indicate we are exiting the Main Stream Attribute SDP
                m_SS_CtrlChar_Count[laneIndex] = 0;
                m_SS_1_StateIndex[laneIndex] = 0;
                m_SS_2_StateIndex[laneIndex] = 0;
                m_MSAStateIndex[laneIndex] = 0;
                //m_SS_CtrlCharVerified = false;
                m_inhibitChecking = false;
                m_inMSA[laneIndex] = false;
                m_BSSequenceStartStateIndex[laneIndex] = 0x00;
            }
            else if (m_inMSA[laneIndex]) 
            {
                // NOTE: we are counting the MSA state located AFTER the SS ctrl chars!!!
                m_MSAStateIndex[laneIndex] += 1;

                // get the Mvid[7:0] and MISC0 sub-fields   
                getMSASubFields(stateData, laneName, laneIndex);
            }
            else  // we are processing the MSA SDP state occurring AFTER the SS ctlr chars
            {
                // NOTE:  processes states occurring outside of the MSA 
                if (m_MSACount > 0)
                {
                    if ((laneData == 0x1BC) || (laneData == 0x11C) || (laneData == 0x17C))  // BS, BF or SR control char
                    {
                        //m_BSSequenceCount = 0;
                        m_BSStateIndex = m_curStateIndex;
                        //m_BeginBlankingStartSection = true;
                    }
                    else
                    {
                        m_BSSequenceCount += 1;
                        if (m_curStateIndex == (m_BSStateIndex + 2))   // this will always process the 3rd state after a BS/SR.... 
                        {
                            m_MVidCount += 1;

                            // verify the Mvid is not changing in sync clk mode
                            if ((!m_inhibitChecking) && (m_synchronousClkModeBit == 0x01))
                            {
                                if (laneData != m_MSA_MVID_Value)
                                {
                                    updateErrorMessages("MVid value differs from MSA's Mvid[7:0] value");
                                    m_inhibitChecking = true;
                                }
                            }

                            // Reset the flag, once we've process the 1st state of the blanking section, 
                            // event though we are techniquelly still in the blanking section... 
                            // This allow us to only look at the 1st state in the blanking section.
                            //m_BeginBlankingStartSection = false;
                        }
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
            subStrings.Add("Number of BS Sequences Processed: " + String.Format("{0:n0}", m_MVidCount));
            subStrings.Add("Number of MSA SDPs Processed: " + String.Format("{0:n0}", m_MSACount));

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

            description.Add("  This test verifies that the MVID state contains the same value as the ");
            description.Add("  previous MSA MVid sub-field value when in synchronous clock mode.");
            description.Add("  ");
            description.Add("  The processed MVID value processes is always located in lane 0; as that");
            description.Add("  sub-field always falls in this lane, regardless of the link width.  The 3 other");
            description.Add("  MSBytes if the MVID sub-fields will be ingnored.");
            description.Add("  ");
            description.Add("  The processed MISC0, used to get sync/async mode, is the maximum data lane");
            description.Add("  used in the link.  That is, in four lane mode, MISC0 is located in lane 3, ");
            description.Add("  two lane mode, MISC0 is in lane 1 and 1 lane mode, MISC0 is in lane 0.");
            description.Add(" ");
            description.Add("  The test reports the number of MSA seen in both async and sync modes.");
            description.Add("  ");
            description.Add("  The processing of the MVID values is always taken from lane 0.");
            description.Add("  MVID states that are encountered before an MSA Secondary Data Packet will be ingnored.");
            return description;

            #endregion // Public Methods
        }
    }
}
