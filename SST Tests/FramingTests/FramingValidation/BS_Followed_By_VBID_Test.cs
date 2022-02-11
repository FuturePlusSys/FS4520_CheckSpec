using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_FramingValidation
{
    /// <summary>
    /// This class verifies that every BS/SR is followed by a VBID, MVID and MAUD
    /// on all active data lanes.  The class accounts for the different ordering of 
    /// the values based on the link width.
    /// 
    /// The class DOES NOT verify the value of the MVID and MAUD values; but the 
    /// does verify the VBID and MAUD are the same value in all four occurances in
    /// the current BS sequence.
    /// 
    /// </summary>
    public class BS_Followed_By_VBID_Test
    {
        #region Members

        private class FieldInfo
        {
            public string FldName { get; set; } = "";
            public int FldValue { get; set; } = -1;

            public FieldInfo(string fldName, int fldValue)
            {
                FldName = fldName;
                FldValue = fldValue;
            }
        }

        private int MaxErrors { get; set; } = 10;

        private bool m_errorEncountered = false;
        private bool m_inhibitSequenceProcessing = false;
        private List<string> m_errMsgs = new List<string>();

        private List<string> m_testInfo = new List<string>();
        private uint m_curStateIndex = 0x00;

        private int m_linkWidth = -1;

        private ML_Common_Functions m_commonFuncs = null;

        private int m_BSSequenceCount = 0;
        private bool m_inBSSequence = false;

        private string[] m_nextEC = new string[4];  // possiblity of four lanes.

        private byte[] m_VBID_Values = new byte[4];
        private byte[] m_MVID_Values = new byte[4];
        private byte[] m_MAUD_Values = new byte[4];

        private List<FieldInfo> m_Fields_L0 = new List<FieldInfo>();
        private List<FieldInfo> m_Fields_L1 = new List<FieldInfo>();
        private List<FieldInfo> m_Fields_L2 = new List<FieldInfo>();
        private List<FieldInfo> m_Fields_L3 = new List<FieldInfo>();

        private int m_ErrorCount = 0;

        private int m_modeID = 1;  // e.g. Test 17 of the excel spreadsheet.

        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public BS_Followed_By_VBID_Test(int ModeID)
        {
            m_commonFuncs = new ML_Common_Functions();

            if (ModeID == 1)
            {
                // Verify VBID, MVID and MAUD values  -- e.g. Test 17 in the excel spreadsheet
                m_modeID = ModeID;
            }
            else if (ModeID == 2)
            {
                // Verify four groups of VBID, MVid and MAud exist at the beginning of each blanking
                // secion... e.g. Test 25 in the excel spreadsheet
                m_modeID = ModeID;
            }
            else
            {
                // TBD
            }
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
                status = "  Passed: No errors were encountered.";
            }
            else
            {
                status = "  Failed: Invalid VB-ID Sequence(s) encountered..";
            }

            return status;
        }


        /// <summary>
        /// Reset class members to default values.
        /// </summary>
        private void resetMembers()
        {
            m_curStateIndex = 0;
            m_ErrorCount = 0;
            m_errorEncountered = false;
            m_inhibitSequenceProcessing = false;

            m_inBSSequence = false;
            Array.Clear(m_nextEC, 0, m_nextEC.Length);

            Array.Clear(m_VBID_Values, 0, m_VBID_Values.Length);
            Array.Clear(m_MVID_Values, 0, m_MVID_Values.Length);
            Array.Clear(m_MAUD_Values, 0, m_MAUD_Values.Length);

            m_Fields_L0 = new List<FieldInfo>();
            m_Fields_L1.Clear();
            m_Fields_L2.Clear();
            m_Fields_L3.Clear();

            m_testInfo.Clear();
            m_errMsgs.Clear();
        }


        /// <summary>
        /// Get the next expected state's event code that immediatelly follows BS's.
        /// </summary>
        /// <param name="ECName"></param>
        /// <param name="VBIDCount"></param>
        /// <returns></returns>
        private string getNextExpectedEventCodeName(string ECName, int ECCount)
        {
            string nextECName = "";

            if (ECName == "VBID")
            {
                nextECName = "MVID";
            }
            else if (ECName == "MVID")
            {
                nextECName = "MAUD";
            }
            else if (ECName == "MAUD")
            {
                if (m_linkWidth == 1)
                {
                    if (ECCount < 10)
                        nextECName = "VBID";
                    else
                        nextECName = "";
                }
                else if (m_linkWidth == 2)
                {
                    if (ECCount < 4)
                        nextECName = "VBID";
                    else
                        nextECName = "";
                }
                else // m_linkWidth == 4
                {
                    nextECName = "";
                }
            }
            return nextECName;
        }


        /// <summary>
        /// Update the error message and error count
        /// </summary>
        /// <param name="errorMsg"></param>
        private void updateErrorMessages(string errorMsg)
        {
            m_errMsgs.Add(errorMsg);
            m_ErrorCount += 1;

            m_inhibitSequenceProcessing = true;
        }

        /// <summary>
        /// Verify the latest VBID added to the list(s)
        /// </summary>
        /// <returns></returns>
        private string verfiyVBID()
        {
            string status = "";

            if (m_linkWidth == 1)
            {
                // One Lane List are in use!

                //     List_L0   List_L1  List_L2  List_3
                //=======================================
                // [0] VBID      ---      ---      ---
                // [1] MVID      ---      ---      ---
                // [2] MAUD      ---      ---      ---
                // [3] VBID      ---      ---      ---
                // [4] MVID      ---      ---      ---
                // [5] MAUD      ---      ---      ---
                // [6] VBID      ---      ---      ---
                // [7] MVID      ---      ---      ---
                // [8] MAUD      ---      ---      ---
                // [9] VBID      ---      ---      ---
                // [10] MVID     ---      ---      ---
                // [11] MAUD     ---      ---      ---

                if (m_Fields_L0.Count == 10)
                {
                    if (m_Fields_L0[9].FldValue != m_Fields_L0[6].FldValue)
                        status = "Fourth VBID Does Not Equal Third VBID";
                }
                else if (m_Fields_L0.Count == 7)
                {
                    if (m_Fields_L0[6].FldValue != m_Fields_L0[3].FldValue)
                        status = "Thrid VBID Does Not Equal Second VBID";
                }
                else if (m_Fields_L1.Count == 4)
                {
                    if (m_Fields_L1[3].FldValue != m_Fields_L0[0].FldValue)
                        status = "Second VBID Does Not Equal First VBID";
                }
                else
                {
                    // TBD
                }
            }
            else if (m_linkWidth == 2)
            {
                // Two Lane Lists are in use!

                //     List_L0  List_L1  List_L2  List_3
                //==================================
                // [0] VBID     VBID     ---      ---
                // [1] MVID     MVID     ---      ---
                // [2] MAUD     MAUD     ---      ---
                // [3] VBID     VBID     ---      ---
                // [4] MVID     MVID     ---      ---
                // [5] MAUD     MAUD     ---      ---

                if (m_Fields_L1.Count == 4)
                {
                    if (m_Fields_L1[3].FldValue != m_Fields_L0[3].FldValue)
                        status = "Fourth VBID Does Not Equal Third VBID";
                }
                else if (m_Fields_L0.Count == 4)
                {
                    if (m_Fields_L0[3].FldValue != m_Fields_L1[0].FldValue)
                        status = "Thrid VBID Does Not Equal Second VBID";
                }
                else if (m_Fields_L1.Count == 1)
                {
                    if (m_Fields_L1[0].FldValue != m_Fields_L0[0].FldValue)
                        status = "Second VBID Does Not Equal First VBID";
                }
                else
                {
                    // TBD
                }
            }
            else if (m_linkWidth == 4)
            {
                // all four Lane Lists are in use!

                //     List_L0  List_L1  List_L2  List_3
                //==================================
                // [0] VBID     VBID     VBID     VBID
                // [1] MVID     MVID     MVID     MVID
                // [2] MAUD     MAUD     MAUD     MAUD

                if (m_Fields_L3.Count >= 1)
                {
                    if (m_Fields_L3[0].FldValue != m_Fields_L2[0].FldValue)
                        status = "Fourth VBID Does Not Equal Third VBID";
                }
                else if (m_Fields_L2.Count >= 1)
                {
                    if (m_Fields_L2[0].FldValue != m_Fields_L1[0].FldValue)
                        status = "Third VBID Does Not Equal Second VBID";
                }
                else if (m_Fields_L1.Count >= 1)
                {
                    if (m_Fields_L1[0].FldValue != m_Fields_L0[0].FldValue)
                        status = "Second VBID Does Not Equal First VBID";
                }
                else
                {
                    // Don't do anthing as we only have L0 VBID
                }
            }
            else
            {
                // TBD
            }

            return status;
        }


        /// <summary>
        /// Verify the latest VBID added to the list(s)
        /// </summary>
        /// <returns></returns>
        private string verfiyMVID()
        {
            string status = "";

            if (m_linkWidth == 1)
            {
                // One Lane List are in use!

                //     List_L0   List_L1  List_L2  List_3
                //=======================================
                // [0] VBID      ---      ---      ---
                // [1] MVID      ---      ---      ---
                // [2] MAUD      ---      ---      ---
                // [3] VBID      ---      ---      ---
                // [4] MVID      ---      ---      ---
                // [5] MAUD      ---      ---      ---
                // [6] VBID      ---      ---      ---
                // [7] MVID      ---      ---      ---
                // [8] MAUD      ---      ---      ---
                // [9] VBID      ---      ---      ---
                // [10] MVID     ---      ---      ---
                // [11] MAUD     ---      ---      ---

                if (m_Fields_L0.Count == 11)
                {
                    if (m_Fields_L0[10].FldValue != m_Fields_L0[7].FldValue)
                        status = "Fourth VBID Does Not Equal Third MVID";
                }
                else if (m_Fields_L0.Count == 8)
                {
                    if (m_Fields_L0[7].FldValue != m_Fields_L0[4].FldValue)
                        status = "Thrid VBID Does Not Equal Second MVID";
                }
                else if (m_Fields_L1.Count == 5)
                {
                    if (m_Fields_L1[4].FldValue != m_Fields_L0[1].FldValue)
                        status = "Second VBID Does Not Equal First MVID";
                }
                else
                {
                    // TBD
                }
            }
            else if (m_linkWidth == 2)
            {
                // Two Lane Lists are in use!

                //     List_L0  List_L1  List_L2  List_3
                //==================================
                // [0] VBID     VBID     ---      ---
                // [1] MVID     MVID     ---      ---
                // [2] MAUD     MAUD     ---      ---
                // [3] VBID     VBID     ---      ---
                // [4] MVID     MVID     ---      ---
                // [5] MAUD     MAUD     ---      ---

                if (m_Fields_L1.Count == 5)
                {
                    if (m_Fields_L1[4].FldValue != m_Fields_L0[4].FldValue)
                        status = "Fourth VBID Does Not Equal Third MVID";
                }
                else if (m_Fields_L0.Count == 5)
                {
                    if (m_Fields_L0[4].FldValue != m_Fields_L1[1].FldValue)
                        status = "Thrid VBID Does Not Equal Second MVID";
                }
                else if (m_Fields_L1.Count == 2)
                {
                    if (m_Fields_L1[1].FldValue != m_Fields_L0[1].FldValue)
                        status = "Second VBID Does Not Equal First MVID";
                }
                else
                {
                    // TBD
                }
            }
            else if (m_linkWidth == 4)
            {
                // all four Lane Lists are in use!

                //     List_L0  List_L1  List_L2  List_3
                //==================================
                // [0] VBID     VBID     VBID     VBID
                // [1] MVID     MVID     MVID     MVID
                // [2] MAUD     MAUD     MAUD     MAUD

                if (m_Fields_L3.Count >= 2)
                {
                    if (m_Fields_L3[1].FldValue != m_Fields_L2[1].FldValue)
                        status = "Fourth VBID Does Not Equal Third MVID";
                }
                else if (m_Fields_L2.Count >= 2)
                {
                    if (m_Fields_L2[1].FldValue != m_Fields_L1[1].FldValue)
                        status = "Third VBID Does Not Equal Second MVID";
                }
                else if (m_Fields_L1.Count >= 2)
                {
                    if (m_Fields_L1[1].FldValue != m_Fields_L0[1].FldValue)
                        status = "Second VBID Does Not Equal First MVID";
                }
                else
                {
                    // Don't do anthing as we only have L0 VBID
                }
            }
            else
            {
                // TBD
            }

            return status;
        }



        /// <summary>
        /// Verify the latest VBID added to the list(s)
        /// </summary>
        /// <returns></returns>
        private string verfiyMAUD()
        {
            string status = "";

            if (m_linkWidth == 1)
            {
                // One Lane List are in use!

                //     List_L0   List_L1  List_L2  List_3
                //=======================================
                // [0] VBID      ---      ---      ---
                // [1] MVID      ---      ---      ---
                // [2] MAUD      ---      ---      ---
                // [3] VBID      ---      ---      ---
                // [4] MVID      ---      ---      ---
                // [5] MAUD      ---      ---      ---
                // [6] VBID      ---      ---      ---
                // [7] MVID      ---      ---      ---
                // [8] MAUD      ---      ---      ---
                // [9] VBID      ---      ---      ---
                // [10] MVID     ---      ---      ---
                // [11] MAUD     ---      ---      ---

                if (m_Fields_L0.Count == 12)
                {
                    if (m_Fields_L0[11].FldValue != m_Fields_L0[8].FldValue)
                        status = "Fourth VBID Does Not Equal Third MAUD";
                }
                else if (m_Fields_L0.Count == 9)
                {
                    if (m_Fields_L0[8].FldValue != m_Fields_L0[5].FldValue)
                        status = "Thrid VBID Does Not Equal Second MAUD";
                }
                else if (m_Fields_L1.Count == 6)
                {
                    if (m_Fields_L1[5].FldValue != m_Fields_L0[2].FldValue)
                        status = "Second VBID Does Not Equal First MAUD";
                }
                else
                {
                    // TBD
                }
            }
            else if (m_linkWidth == 2)
            {
                // Two Lane Lists are in use!

                //     List_L0  List_L1  List_L2  List_3
                //==================================
                // [0] VBID     VBID     ---      ---
                // [1] MVID     MVID     ---      ---
                // [2] MAUD     MAUD     ---      ---
                // [3] VBID     VBID     ---      ---
                // [4] MVID     MVID     ---      ---
                // [5] MAUD     MAUD     ---      ---

                if (m_Fields_L1.Count == 6)
                {
                    if (m_Fields_L1[5].FldValue != m_Fields_L0[5].FldValue)
                        status = "Fourth VBID Does Not Equal Third MAUD";
                }
                else if (m_Fields_L0.Count == 6)
                {
                    if (m_Fields_L0[5].FldValue != m_Fields_L1[2].FldValue)
                        status = "Thrid VBID Does Not Equal Second MAUD";
                }
                else if (m_Fields_L1.Count == 3)
                {
                    if (m_Fields_L1[2].FldValue != m_Fields_L0[2].FldValue)
                        status = "Second VBID Does Not Equal First MAUD";
                }
                else
                {
                    // TBD
                }
            }
            else if (m_linkWidth == 4)
            {
                // all four Lane Lists are in use!

                //     List_L0  List_L1  List_L2  List_3
                //==================================
                // [0] VBID     VBID     VBID     VBID
                // [1] MVID     MVID     MVID     MVID
                // [2] MAUD     MAUD     MAUD     MAUD

                if (m_Fields_L3.Count >= 3)
                {
                    if (m_Fields_L3[2].FldValue != m_Fields_L2[2].FldValue)
                        status = "Fourth VBID Does Not Equal Third MAUD";
                }
                else if (m_Fields_L2.Count >= 3)
                {
                    if (m_Fields_L2[2].FldValue != m_Fields_L1[2].FldValue)
                        status = "Third VBID Does Not Equal Second MAUD";
                }
                else if (m_Fields_L1.Count >= 3)
                {
                    if (m_Fields_L1[2].FldValue != m_Fields_L0[2].FldValue)
                        status = "Second VBID Does Not Equal First MAUD";
                }
                else
                {
                    // Don't do anthing as we only have L0 VBID
                }
            }
            else
            {
                // TBD
            }

            return status;
        }


        /// <summary>
        /// Verfiy the total count of states is 12!  Four groups of VBID, MVID and MAUD.
        /// </summary>
        /// <returns></returns>
        private string verifyGrouping()
        {
            string status = "";

            if ((m_Fields_L0.Count + m_Fields_L1.Count + m_Fields_L2.Count + m_Fields_L3.Count) < 12)
            {
                status = "Mising at least 1 state in the VBID, MVid and MAud grouping";
            }

            return status;
        }

        /// <summary>
        /// Process the lane data for BS or BF control characters.
        /// </summary>
        /// <param name="laneName"></param>
        /// This method assembles the four groups of VBID, MVID and MAUD states into 
        /// groups that reflect the DP 1.4a specification; and then verifies the values
        /// are identical in each group.   That is all four VBID values are the same, as 
        /// well as the MVID and MAUD values.
        /// 
        ///
        ///               Four Lanes                          Two Lanes            One Lane
        /// 
        /// +---------+---------+---------+---------+    +---------+---------+    +---------+
        /// |   BS    |   BS    |   BS    |   BS    |    |   BS    |   BS    |    |   BS    |
        /// |  VB-ID  |  VB-ID  |  VB-ID  |  VB-ID  |    |  VB-ID  |  VB-ID  |    |  VB-ID  |
        /// |  Mvid   |  MvidD  |  Mvid   |  Mvid   |    |  Mvid   |  MvidD  |    |  Mvid   |
        /// |  Maud   |  Maud   |  Maud   |  Maud   |    |  Maud   |  Maud   |    |  Maud   |
        /// +---------+---------+---------+---------+    |  VB-ID  |  VB-ID  |    |  VB-ID  |
        ///                                              |  Mvid   |  MvidD  |    |  Mvid   | 
        ///                                              |  Maud   |  Maud   |    |  Maud   |  
        ///                                              +---------+---------+    |  VB-ID  |
        ///                                                                       |  Mvid   |
        ///                                                                       |  Maud   |
        ///                                                                       |  VB-ID  |
        ///                                                                       |  Mvid   |
        ///                                                                       |  Maud   |
        ///                                                                       +---------+
        ///
        private bool processLane(byte[] stateData, string laneName)
        {
            bool status = true;
            int laneIndex = getLaneIndex(laneName);
            string verifyStatus = "";

            try
            {
                int laneData = m_commonFuncs.GetFieldData(stateData, laneName);
                if (laneData > -1)
                {
                    if ((laneData == 0x11C) || (laneData == 0x1BC) || (laneData == 0x17C))
                    {
                        //
                        //NOTE:  This is invoked consecuatively; once for each active lane. 
                        //

                        m_nextEC[laneIndex] = "VBID";

                        m_Fields_L0.Clear();
                        m_Fields_L1.Clear();
                        m_Fields_L2.Clear();
                        m_Fields_L3.Clear();

                        m_inBSSequence = true;
                        m_inhibitSequenceProcessing = false;
                    }
                    else
                    {
                        if (m_inBSSequence == true)
                        {
                            //
                            // condition is exited when processing lane 0... because higher function processes lane 0 - lane 4 sequencially.
                            //
                            m_BSSequenceCount += 1;
                            m_inBSSequence = false;
                        }


                        if ((m_nextEC[laneIndex] == "VBID") && (m_inhibitSequenceProcessing == false))
                        {
                            string ECName = m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode"));
                            if (ECName.ToUpper().EndsWith("VBID")) // Vertical or Horizontal
                            {
                                if (laneName == "Lane0")
                                {
                                    m_Fields_L0.Add(new FieldInfo("VBID", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L0.Count);
                                }
                                else if (laneName == "Lane1")
                                {
                                    m_Fields_L1.Add(new FieldInfo("VBID", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L1.Count);
                                }
                                else if (laneName == "Lane2")
                                {
                                    m_Fields_L2.Add(new FieldInfo("VBID", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L2.Count);
                                }
                                else if (laneName == "Lane3")
                                {
                                    m_Fields_L3.Add(new FieldInfo("VBID", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L3.Count);
                                }

                                if (m_modeID == 1)
                                {
                                    verifyStatus = verfiyVBID();
                                    if (verifyStatus != "")
                                        updateErrorMessages(verifyStatus);
                                }
                            }
                            else
                            {
                                // Error
                                updateErrorMessages(laneName + " - Invalid VBID Sequence");
                                m_inhibitSequenceProcessing = true;
                            }
                        }
                        else if ((m_nextEC[laneIndex] == "MVID") && (m_inhibitSequenceProcessing == false))
                        {
                            string ECName = m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode"));
                            if (ECName.ToUpper().EndsWith("MVID")) // Vertical or Horizontal
                            {
                                if (laneName == "Lane0")
                                {
                                    m_Fields_L0.Add(new FieldInfo("MVID", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L0.Count);
                                }
                                else if (laneName == "Lane1")
                                {
                                    m_Fields_L1.Add(new FieldInfo("MVID", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L1.Count);
                                }
                                else if (laneName == "Lane2")
                                {
                                    m_Fields_L2.Add(new FieldInfo("MVID", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L2.Count);
                                }
                                else if (laneName == "Lane3")
                                {
                                    m_Fields_L3.Add(new FieldInfo("MVID", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L3.Count);
                                }


                                if (m_modeID == 1)
                                {
                                    verifyStatus = verfiyMVID();
                                    if (verifyStatus != "")
                                        updateErrorMessages(verifyStatus);
                                }
                            }
                            else
                            {
                                // Error
                                updateErrorMessages(laneName + " - Invalid MVID Sequence");
                                m_inhibitSequenceProcessing = true;
                            }
                        }
                        else if ((m_nextEC[laneIndex] == "MAUD") && (m_inhibitSequenceProcessing == false))
                        {
                            string ECName = m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode"));
                            if (ECName.ToUpper().EndsWith("MAUD")) // Vertical or Horizontal
                            {
                                if (laneName == "Lane0")
                                {
                                    m_Fields_L0.Add(new FieldInfo("MAUD", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L0.Count);
                                }
                                else if (laneName == "Lane1")
                                {
                                    m_Fields_L1.Add(new FieldInfo("MAUD", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L1.Count);
                                }
                                else if (laneName == "Lane2")
                                {
                                    m_Fields_L2.Add(new FieldInfo("MAUD", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L2.Count);
                                }
                                else if (laneName == "Lane3")
                                {
                                    m_Fields_L3.Add(new FieldInfo("MAUD", m_commonFuncs.GetFieldData(stateData, laneName)));
                                    m_nextEC[laneIndex] = getNextExpectedEventCodeName(m_nextEC[laneIndex], m_Fields_L3.Count);
                                }

                                if (m_modeID == 1)
                                {
                                    verifyStatus = verfiyMAUD();
                                    if (verifyStatus != "")
                                        updateErrorMessages(verifyStatus);
                                }
                            }
                            else
                            {
                                // Error
                                updateErrorMessages(laneName + " - Invalid MAUD Sequence");
                                m_inhibitSequenceProcessing = true;
                            }
                        }
                        else if (m_nextEC[laneIndex] == "")
                        {
                            if (m_modeID == 2)
                            {
                                verifyStatus = verifyGrouping();
                                if (verifyStatus != "")
                                    updateErrorMessages(verifyStatus);
                            }
                        }
                    }
                }
                else
                {
                    updateErrorMessages(laneName + " - Get Lane Data Failure");
                    m_ErrorCount += 1;
                }
            }
            catch (Exception ex)
            {
                updateErrorMessages(ex.Message);
            }

            return status;
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
            subStrings.Add("Number of BS Sequences Processed: " + m_BSSequenceCount.ToString());

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

            description.Add(" This test verifies that every BS/SR is followed by four sets of ");
            description.Add(" VBID, MVID and MAUD values.  The class accounts for the different");
            description.Add(" ordering of the values based on the link width.");
            description.Add(" ");
            description.Add(" The test verifies the VBID and MAUD are the same value in all four occurances in");
            description.Add(" the current BS sequence.");
            description.Add(" ");
            description.Add(" The test DOES NOT verify the value of the MVID and MAUD values.");

            return description;
        }

    }
    #endregion // Public Methods
}
