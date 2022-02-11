using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_AudioValidation
{

    /// <summary>
    /// This class verifies that the Audio Timestamp value does not changed in excess of 0.05%.
    /// 
    /// The class only processes lane 0; that is, it does not process any MAUD values 
    /// on lanes 1, 2, or 3.
    /// 
    /// The processing of the MAUD values is always taken from lane 0.  
    /// MAUD states that are encountered before an Audio Time Stamp Secondary Data Packet will be ingnored.
    /// 
    /// </summary>
    class AudioTS_Variance
    {
        #region Members

        private const int AUDIO_TS_HB0_ID = 0x01;

        private int MaxErrors { get; set; } = 10;
        private int m_linkWidth = -1;

        private bool m_errorEncountered = false;
        private List<string> m_errMsgs = new List<string>();
        private List<string> m_testInfo = new List<string>();

        private uint m_curStateIndex = 0x00;

        private ML_Common_Functions m_commonFuncs = null;
        private int m_ErrorCount = 0;
        private int m_SDPCount = 0;


        private bool m_inSDP = false;
        private bool m_inMSA = false;
        private bool m_inAudioTS = false;
        private bool m_inUnknownSDP = false;
        private bool m_inOtherSDP = false;
        private uint m_SDPIndex = 0;

        private int m_MaudValue = 0x00;
        private int m_NaudValue = 0x00;

        private float m_prevAudioTSValue = 0.0f;
        private float m_audioTSValue = 0.0f;

        private int[] m_laneData = new int[4];
        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// default Constructor
        /// </summary>
        public AudioTS_Variance()
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
            m_SDPCount = 0;
            m_errorEncountered = false;

            m_testInfo.Clear();
            m_errMsgs.Clear();


            m_inSDP = false;
            m_inMSA = false;
            m_inAudioTS = false;
            m_inUnknownSDP = false;
            m_inOtherSDP = false;
            m_SDPIndex = 0;

            m_MaudValue = 0x00;
            m_NaudValue = 0x00;

            m_prevAudioTSValue = 0.0f;
            m_audioTSValue = 0.0f;

            Array.Clear(m_laneData, 0, m_laneData.Length);
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
                status = "  Failed: One or more Maud value(s) did not match Audio TimeStamp";
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
        /// Get active lanes
        /// </summary>
        /// <param name="stateData"></param>
        /// <param name="linkWidth"></param>
        /// <returns></returns>
        private void getActiveLaneData(byte[] stateData, int linkWidth)
        {
            Array.Clear(m_laneData, 0, m_laneData.Length);

            m_laneData[0] = m_commonFuncs.GetFieldData(stateData, "Lane0");
            if (m_linkWidth >= 2)
            {
                m_laneData[1] = m_commonFuncs.GetFieldData(stateData, "Lane1");
            }
            if (m_linkWidth >= 4)
            {
                m_laneData[2] = m_commonFuncs.GetFieldData(stateData, "Lane2");
                m_laneData[3] = m_commonFuncs.GetFieldData(stateData, "Lane3");
            }
        }


        /// <summary>
        /// Move the current Audio TS value into the prevAudioTS value and 
        /// zero out the current Audio TS value.
        /// </summary>
        private void updateAudioTSValues()
        {
            m_prevAudioTSValue = m_audioTSValue;
            m_audioTSValue = 0.0f;
        }


        /// <summary>
        /// Compare the previous and current Audio TS values; reporting and deviation greater thatn .05%
        /// </summary>
        private void compareAudioTSValues()
        {
            if (m_prevAudioTSValue > 0.0f)
            {
                float margin = m_prevAudioTSValue * 0.05f;

                if (Math.Abs((m_audioTSValue - m_prevAudioTSValue)) > margin)
                {
                    updateErrorMessages("Audio TS change greater that .05% detected");
                }
            }
        }


        /// <summary>
        /// Calculate the TimeStamp value from the assembled Maud and Naud values.
        /// </summary>
        private void calcAudioTSValue()
        {
            m_audioTSValue = (float)((float)m_MaudValue / (float)m_NaudValue);
        }


        /// <summary>
        /// Evaluate the previous and current audio TS values.
        /// </summary>
        private void evalulateAudioTSValues()
        {
            calcAudioTSValue();
            compareAudioTSValues();
            updateAudioTSValues();
        }


        /// <summary>
        /// Get Maud Bytes for a 1 lane link
        /// </summary>
        /// <param name="laneData"></param>
        /// <param name="linkWidth"></param>
        private void getMaudBytes_1_Lane(int[] laneData, int linkWidth)
        {
            if (m_curStateIndex == m_SDPIndex + 9)
            {
                m_MaudValue = (int)(laneData[0] << 16);
            }
            else if (m_curStateIndex == m_SDPIndex + 10)
            {
                m_MaudValue |= (int)(laneData[0] << 8);
            }
            else if (m_curStateIndex == m_SDPIndex + 11)
            {
                m_MaudValue |= (int)(laneData[0]);
            }
        }

        /// <summary>
        /// Get Maud Bytes for a 2 or 4 lane link
        /// </summary>
        /// <param name="laneData"></param>
        /// <param name="linkWidth"></param>
        private void getMaudBytes_2_Lane(int[] laneData, int linkWidth)
        {
            if (m_curStateIndex == m_SDPIndex + 5)
            {
                m_MaudValue = (int)(laneData[0] << 16);
            }
            else if (m_curStateIndex == m_SDPIndex + 6)
            {
                m_MaudValue |= (int)(laneData[0] << 8);
            }
            else if (m_curStateIndex == m_SDPIndex + 7)
            {
                m_MaudValue |= (int)(laneData[0]);
            }
        }


        /// <summary>
        /// Get Maud Bytes for a 2 or 4 lane link
        /// </summary>
        /// <param name="laneData"></param>
        /// <param name="linkWidth"></param>
        private void getMaudBytes_4_Lane(int[] laneData, int linkWidth)
        {
            if (m_curStateIndex == m_SDPIndex + 3)
            {
                m_MaudValue = (int)(laneData[0] << 16);
            }
            else if (m_curStateIndex == m_SDPIndex + 4)
            {
                m_MaudValue |= (int)(laneData[0] << 8);
            }
            else if (m_curStateIndex == m_SDPIndex + 5)
            {
                m_MaudValue |= (int)(laneData[0]);
            }
        }


        /// <summary>
        /// Get Naud Bytes for a 1 lane link
        /// </summary>
        /// <param name="laneData"></param>
        /// <param name="linkWidth"></param>
        private void getNaudBytes_1_Lane(int[] laneData, int linkWidth)
        {
           if (m_curStateIndex == m_SDPIndex + 29)
            {
                m_NaudValue |= (int)(laneData[0] << 16);
            }
            else if (m_curStateIndex == m_SDPIndex + 30)
            {
                m_MaudValue |= (int)(laneData[0] << 8);
            }
            else if (m_curStateIndex == m_SDPIndex + 31)
            {
                m_MaudValue |= (int)(laneData[0]);
            }
        }


        /// <summary>
        /// Get Maud Bytes for a 2 or 4 lane link
        /// </summary>
        /// <param name="laneData"></param>
        /// <param name="linkWidth"></param>
        private void getNaudBytes_2_Lane(int[] laneData, int linkWidth)
        {
            if (m_curStateIndex == m_SDPIndex + 15)
            {
                m_NaudValue = (int)(laneData[0] << 16);
            }
            else if (m_curStateIndex == m_SDPIndex + 16)
            {
                m_NaudValue |= (int)(laneData[0] << 8);
            }
            else if (m_curStateIndex == m_SDPIndex + 17)
            {
                m_NaudValue |= (int)(laneData[0]);
            }
        }


        /// <summary>
        /// Get Maud Bytes for a 2 or 4 lane link
        /// </summary>
        /// <param name="laneData"></param>
        /// <param name="linkWidth"></param>
        private void getNaudBytes_4_Lane(int[] laneData, int linkWidth)
        {
            if (m_curStateIndex == m_SDPIndex + 8)
            {
                m_NaudValue = (int)(laneData[0] << 16);
            }
            else if (m_curStateIndex == m_SDPIndex + 9)
            {
                m_NaudValue |= (int)(laneData[0] << 8);
            }
            else if (m_curStateIndex == m_SDPIndex + 10)
            {
                m_NaudValue |= (int)(laneData[0]);
            }
        }


        /// <summary>
        /// Assemble the MAUD[23:0]  bytes
        /// </summary>
        /// <param name="laneData"></param>
        /// <param name="linkWidth"></param>
        private void getMaudBytes(int[] laneData, int linkWidth)
        {
            if (linkWidth == 1)
            {
                getMaudBytes_1_Lane(laneData, linkWidth);
            }
            else if (linkWidth == 2)
            {
                getMaudBytes_2_Lane(laneData, linkWidth);
            }
            else //if (linkWidth == 4)
            {
                getMaudBytes_4_Lane(laneData, linkWidth);
            }
        }


        /// <summary>
        /// Assemble the NAUD[23:0]  bytes
        /// </summary>
        /// <param name="laneData"></param>
        /// <param name="linkWidth"></param>
        private void getNaudBytes(int[] laneData, int linkWidth)
        {
            if (linkWidth == 1)
            {
                getNaudBytes_1_Lane(laneData, linkWidth);
            }
            else if (linkWidth == 2) 
            {
                getNaudBytes_2_Lane(laneData, linkWidth);
            } 
            else if (linkWidth == 4)
            {
                getNaudBytes_4_Lane(laneData, linkWidth);
            }
        }


        /// <summary>
        /// determine the SDP type by looking at HB1
        /// </summary>
        /// <param name="laneData"></param>
        /// <param name="linkWidth"></param>
        private void setSDPType(int[] laneData, int linkWidth)
        {
            if (m_linkWidth == 1)
            {
                if (m_curStateIndex == (m_SDPIndex + 3))                    // get the HB0 Byte Value
                {
                    if (laneData[1] == AUDIO_TS_HB0_ID)
                    {
                        m_inUnknownSDP = false;
                        m_inOtherSDP = false;
                        m_inAudioTS = true;
                    }
                    else
                    {
                        m_inUnknownSDP = false;
                        m_inOtherSDP = true;
                        m_inAudioTS = false;
                    }
                }
            }
            else // if ((m_linkWidth == 2) || (m_linkWidth == 4))
            {
                if (m_curStateIndex == (m_SDPIndex + 1))                    // get the HB0 Byte Value
                {
                    if (laneData[1] == AUDIO_TS_HB0_ID)
                    {
                        m_inUnknownSDP = false;
                        m_inOtherSDP = false;
                        m_inAudioTS = true;
                    }
                    else
                    {
                        m_inUnknownSDP = false;
                        m_inOtherSDP = true;
                        m_inAudioTS = false;
                    }
                }
            }
        }




        /// <summary>
        /// Process the lane data for Maud state values.
        /// </summary>
        ///         
        ///   Audio TS Index                                         Audio TS Index                  Audio TS Index
        ///   |                                                      |                               |
        ///   |   +----------+----------+----------+----------+      |  +----------+----------+      |  +----------+
        ///   |   | SS       | SS       | SS       | SS       |      |  | SS       | SS       |      |  | SS       |
        ///   |   +--------- +----------+----------+----------+      |  +----------+----------+      |  +----------+
        ///   1   | HB0      | HB1      | HB2      | HB3      |      1  | HB0      | HB1      |      1  | HB0      |
        ///   2   | PB0      | PB1      | PB2      | PB3      |      2  | PB0      | PB1      |      2  | PB0      |
        ///   3   | Maud23:16| Maud23:16| Maud23:16| Maud23:16|      3  | HB2      | HB3      |      3  | HB1      |
        ///   4   | Maud15:8 | Maud15:8 | Maud15:8 | Maud15:8 |      4  | PB2      | PB3      |      4  | PB1      |
        ///   5   | Maud7:0  | Maud7:0  | Maud7:0  | Maud7:0  |      5  | Maud23:16| Maud23:16|      5  | HB2      |
        ///   6   | All 0's  | All 0's  | All 0's  | All 0's  |      6  | Maud15:8 | Maud15:8 |      6  | PB2      |
        ///   7   | PB4      | PB5      | PB6      | PB7      |      7  | Maud7:0  | Maud7:0  |      7  | HB3      |
        ///   8   | Naud23:16| Naud23:16| Naud23:16| Naud23:16|      8  | All 0's  | All 0's  |      8  | PB3      |
        ///   9   | Naud15:8 | Naud15:8 | Naud15:8 | Naud15:8 |      9  | PB4      | PB5      |      9  | Maud23:16|
        ///   10  | Naud7:0  | Naud7:0  | Naud7:0  | Naud7:0  |      10 | Maud23:16| Maud23:16|      10 | Maud15:8 |
        ///   11  | All 0's  | All 0's  | All 0's  | All 0's  |      11 | Maud15:8 | Maud15:8 |      11 | Maud7:0  |
        ///   12  | PB8      | PB9      | PB10     | PB11     |      12 | Maud7:0  | Maud7:0  |      12 | All 0's  |
        ///       +--------- +----------+----------+----------+      13 | All 0's  | All 0's  |      13 | PB4      |
        ///       | SE       | SE       | SE       | SE       |      14 | PB6      | PB7      |      14 | Maud23:16|
        ///       +--------- +----------+----------+----------+      15 | Naud23:16| Naud23:16|      15 | Maud15:8 |
        ///                                                          16 | Naud15:8 | Naud15:8 |      16 | Maud7:0  |
        ///                                                          17 | Maud7:0  | Maud7:0  |      17 | All 0's  |
        ///                                                          18 | All 0's  | All 0's  |      18 | PB5      |
        ///                                                          19 | PB8      | PB9      |      19 | Maud23:16|      
        ///                                                          20 | Naud23:16| Naud23:16|      20 | Maud15:8 |       
        ///                                                          21 | Naud15:8 | Naud15:8 |      21 | Maud7:0  |       
        ///                                                          22 | Maud7:0  | Maud7:0  |      22 | All 0's  |        
        ///                                                          23 | All 0's  | All 0's  |      23 | PB6      |   
        ///                                                          24 | PB10     | PB11     |      24 | Maud23:16| 
        ///                                                             +----------+----------+      25 | Maud15:8 | 
        ///                                                             | SE       | SE       |      26 | Maud7:0  |  
        ///                                                             +----------+----------+      27 | All 0's  |        
        ///                                                                                          28 | PB7      | 
        ///                                                                                          29 | Naud23:16|       
        ///                                                                                          30 | Naud15:8 |        
        ///                                                                                          31 | Naud7:0  |        
        ///                                                                                          32 | All 0's  |       
        ///                                                                                          33 | PB8      |           
        ///                                                                                          34 | Naud23:16|       
        ///                                                                                          35 | Naud15:8 |        
        ///                                                                                          36 | Naud7:0  |        
        ///                                                                                          37 | All 0's  |  
        ///                                                                                          38 | PB9      |          
        ///                                                                                          39 | Naud23:16|       
        ///                                                                                          40 | Naud15:8 |        
        ///                                                                                          41 | Naud7:0  |        
        ///                                                                                          42 | All 0's  |  
        ///                                                                                          43 | PB10     |           
        ///                                                                                          44 | Naud23:16|       
        ///                                                                                          45 | Naud15:8 |        
        ///                                                                                          46 | Naud7:0  |        
        ///                                                                                          47 | All 0's  |  
        ///                                                                                          48 | PB11     |
        ///                                                                                             +----------+
        ///                                                                                             | SE       |
        ///                                                                                             +----------+                                                            
        /// 
        /// <param name="laneName"></param>
        private bool processLane(byte[] stateData, string laneName)
        {
            bool status = true;

            try
            {
                getActiveLaneData(stateData, m_linkWidth);
                if (m_laneData[0] == 0x15C)                                               // SS Ctrl Char
                {
                    m_MaudValue = 0x00;
                    m_NaudValue = 0x00;

                    if (!m_inSDP)
                    {
                        m_inSDP = true;
                        m_inUnknownSDP = true;
                        m_SDPIndex = m_curStateIndex;
                    }
                    else
                    {
                        if (m_curStateIndex == (m_SDPIndex + 1))
                        {
                            m_inMSA = true;
                            m_inSDP = false;
                            m_SDPCount += 1;
                        }
                        else
                        {
                            updateErrorMessages("Invalid SS Ctrl Character Encountered");
                            m_inMSA = false;
                            m_inSDP = false;
                        }
                    }
                }
                else if (m_laneData[0] == 0x1FD)                                             // SE Ctrl Char
                {
                    m_inMSA = false;
                    m_inSDP = false;
                    m_inOtherSDP = false;
                }
                else
                {
                    if (m_inSDP && !m_inOtherSDP)
                    {
                        if (m_inUnknownSDP == true)
                        {
                            setSDPType(m_laneData, m_linkWidth);                              // examine HB1 to get the SDP ID
                        }
                        else
                        {
                            if (m_inAudioTS)
                            {
                                // get the Maud/Naud bytes...
                                if (m_linkWidth == 1)
                                {
                                    if (m_SDPIndex < (m_curStateIndex + 31))
                                    {
                                        if ((m_curStateIndex >= 9) && (m_curStateIndex <= 11))
                                            getMaudBytes(m_laneData, m_linkWidth);
                                        else if ((m_curStateIndex >= 29) && (m_curStateIndex <= 31))
                                            getNaudBytes(m_laneData, m_linkWidth);

                                        if (m_curStateIndex == 31)
                                            evalulateAudioTSValues();                           // do the math!
                                    }
                                }
                                else //if ((m_linkWidth == 2) || (m_linkWidth == 4))
                                {
                                    if (m_SDPIndex < (m_curStateIndex + 11))
                                    {
                                        if ((m_curStateIndex >= (m_SDPIndex + 3)) && (m_curStateIndex <= (m_SDPIndex + 5)))
                                            getMaudBytes(m_laneData, m_linkWidth);
                                        else if ((m_curStateIndex >= (m_SDPIndex + 8)) && (m_curStateIndex <= (m_SDPIndex + 11)))
                                            getNaudBytes(m_laneData, m_linkWidth);

                                        if (m_curStateIndex == (m_SDPIndex + 11))
                                            evalulateAudioTSValues();                       // do the math!
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateErrorMessages("Error Processing State Data");
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
            subStrings.Add("Number of Audio TS SDPs Processed: " + m_SDPCount.ToString());

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

            description.Add(" This test verifies Audio TS Maud/Naud does not vary more ");
            description.Add(" than 0.05% between time stamps.");
            description.Add(" ");

            return description;
        }
        #endregion // Public Methods
    }
}
