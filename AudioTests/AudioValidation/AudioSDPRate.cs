using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_AudioValidation
{
    //
    // Verifies transmitted audio samples packets are issued at a calcuated rate.
    //
    class AudioSDPRate
    {
        #region Members
        private int MaxErrors { get; set; } = 10;
        private int m_linkWidth = -1;
        private float m_LS_CLK = 0.0f;
        private float m_audioSamplingFreq = 0.0f;
        private float m_clkPeriod = 0.0f;

        private bool m_errorEncountered = false;
        private List<string> m_errMsgs = new List<string>();
        private List<string> m_testInfo = new List<string>();

        private uint m_curStateIndex = 0x00;

        private ML_Common_Functions m_commonFuncs = null;
        private int m_ErrorCount = 0;

        private int[] m_laneData = new int[4];
        private int m_prevSDPStateValue = 0x00;
        private int m_Htotal = 0x00;
        private int m_Vtotal = 0x00;
        private int m_Maud = 0x00;
        private int m_Naud = 0x00;

        private int m_horBSCount = 0;
        private uint m_VerticalBS_StartIndex = 0x00;

        private bool m_unknownSDP = true;
        private bool m_inAudioInfoFrame = false;
        private bool m_inAudioTimeStamp = false;
        private bool m_inAudioSDP = false;
        private bool m_inOtherSDP = false;
        private int m_SamplingFreqID = 0x00;

        private bool m_inBSSymbolSequence = false;
        private bool m_inSDP = false;
        private bool m_inMSA = false;

        //private int m_inBSSectionCount = 0x00;
        //private bool m_prevBSSectionIsVertical = false;
        private bool m_curBSSectionIsVertical = false;

        private int m_frameCount = 0;
        //private int m_verticalBSSequenceCount = 0x00;
        private int m_SDPIndex = 0x00;
        private int m_audioSampleCount = 0x00;
        private int m_audioSampleCount_Total = 0x00;

        private int m_MSACount = 0x00;
        private int m_AudioTSCount = 0x00;
        private int m_audioInfoFrameCount = 0x00;
        private int m_BSsymbolSequencesCount = 0x00;
        private List<int> m_FrameLineCounts = new List<int>();
        private List<long> m_VerticalBlankingStart_TimeTicks = new List<long>();
        private List<int> m_AudioSDPCounts = new List<int>();

        // test/debugging variables
        //List<string> MSASubFields = new List<string>();
        List<string> SamplingFreqFields = new List<string>();

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Default Constructor
        /// </summary>
        public AudioSDPRate()
        {
            m_commonFuncs = new ML_Common_Functions();
        }

        #endregion  // Constructor(s)

        #region Private Methods

        /// <summary>
        /// Reset class members to default values.
        /// </summary>
        private void resetMembers()
        {
            m_curStateIndex = 0;
            m_ErrorCount = 0;
            m_errorEncountered = false;

            m_testInfo.Clear();
            m_errMsgs.Clear();

            Array.Clear(m_laneData, 0, m_laneData.Length);

            m_VerticalBlankingStart_TimeTicks.Clear();
            m_FrameLineCounts.Clear();
            m_AudioSDPCounts.Clear();

            m_MSACount = 0x00;
            m_AudioTSCount = 0x00;
            m_audioInfoFrameCount = 0x00;
            m_audioSampleCount = 0x00;
            m_audioSampleCount_Total = 0x00;
            m_audioSamplingFreq = 0.0f;

            m_LS_CLK = 0.0f;
            m_clkPeriod = 0.0f;

            m_horBSCount = 0x00;
            //m_verticalBSSequenceCount = 0x00;
        }


        /// <summary>
        /// reset variables...
        /// </summary>
        private void resetSDPVariables()
        {
            m_SDPIndex = 0x00;
            m_inSDP = false;
            m_inMSA = false;
            m_unknownSDP = true;
            m_inAudioInfoFrame = false;
            m_inAudioTimeStamp = false;
            m_inAudioSDP = false;
            m_inOtherSDP = false;
        }


        /// <summary>
        /// Reset!
        /// </summary>
        private void resetSDPSubFields()
        {
            m_Htotal = 0x00;
            m_Vtotal = 0x00;

            m_Maud = 0x00;
            m_Naud = 0x00;

            m_SamplingFreqID = 0x00;
        }



        /// <summary>
        /// Get a numeric value for the main link clock.
        /// </summary>
        /// <param name="speedID"></param>
        /// <returns></returns>
        private int getMainLinkClk(string speedID)
        {
            int clkValue = 0x00;

            switch (speedID)
            {
                case "1.67G":
                    clkValue = 167000000;
                    break;
                case "2.7G":
                    clkValue = 270000000;
                    break;
                case "5.4G":
                    clkValue = 540000000;
                    break;
                case "8.1G":
                    clkValue = 810000000;
                    break;

                default:
                    break;
            }


            return clkValue;
        }


        /// <summary>
        /// Calculate the clock period given the main link speed.
        /// </summary>
        /// <param name="speedID"></param>
        /// <returns></returns>
        private float getClkPeriod(string speedID)
        {
            float clkperiod = 0.0f;

            switch (speedID)
            {
                case "1.67G":
                    clkperiod = 6.172f;  // nSecs
                    break;
                case "2.7G":
                    clkperiod = 3.703f;  // nSecs
                    break;
                case "5.4G":
                    clkperiod = 1.851f;  // nSecs
                    break;
                case "8.1G":
                    clkperiod = 1.234f;  // nSecs
                    break;

                default:
                    break;
            }


            return clkperiod;
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
        /// Get the data bytes based on m_linkWidth value.
        /// </summary>
        /// <param name="stateData"></param>
        /// <param name="linkWidth"></param>
        /// <returns></returns>
        private void getStateData(byte[] stateData, int linkWidth)
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
        /// Extract the 50 bit time value (ticks) from the current state.
        /// </summary>
        /// <returns></returns>
        private long getTimeTicks(byte[] stateData)
        {
            long ticks = 0x00;

            //ticks = ((((stateData[2] & 0x1F) << 45) | (stateData[3] << 37) | (stateData[4] << 29) | (stateData[5] << 21) | (stateData[6] << 13) | (stateData[7] << 5) | (stateData[8]) >> 3));
            //long thisByte = ((long)stateData[4] << 29);
            //ticks |= thisByte;

            //// this is the wrong format... it is for DP 1.1a SST
            //ticks = (long)((long)(stateData[8] & 0xF8)) >> 3;
            //ticks |= (long)(stateData[7] << 5);
            //ticks |= (long)(stateData[6] << 13);
            //ticks |= (long)(stateData[5] << 21);
            //ticks |= (long)stateData[4] << 29;
            //ticks |= (long)stateData[3] << 37;
            //ticks |= (long)((long)(stateData[2] & 0x1F) << 45);


            // this is the wrong format... it is for DP 1.1a SST
            ticks = (long)((long)(stateData[7] & 0xFE)) >> 1;
            ticks |= (long)(stateData[6] << 7);
            ticks |= (long)(stateData[5] << 15);
            ticks |= (long)(stateData[4] << 23);
            ticks |= (long)(stateData[3] << 31);
            ticks |= (long)(stateData[2] << 39);
            ticks |= (long)((long)(stateData[1] & 0x07)) << 47;


            return ticks;
        }

        /// <summary>
        /// Update the total count of processed Audio Stream SDPs
        /// </summary>
        private void updateTotalAudioStreamSDPCount()
        {
            if (m_linkWidth == 1)
            {
                if (m_SDPIndex == 4)   // must process HB1 before we know the type of SDP we have... so 4 works for all link widths
                    m_audioSampleCount_Total += 1;
            }
            else // widht == 2 or 4
            {
                if (m_SDPIndex == 4)
                    m_audioSampleCount_Total += 1;
            }
        }


        /// <summary>
        /// Calculate the Audio Sampling Frequency using Mand and Naud variables... DP_v1.4_mem.pdf section 2.2.5.2
        /// </summary>
        /// 
        /// f_LS_CLK variable is taken from the Main Link Speed, e.g. 8.1G == 810,000,000 ...
        /// Maud is extracted from the Audio Timestamp SDP
        /// Naud is extracted from the Audio Timestamp SDP
        /// 
        ///      Maud             Fs
        ///     ----- = 512 * ----------
        ///      Naud           f_LS_CLK
        ///      
        ///   where:  1) Fs is the Sampling Frequecy             e.g  44.1K  --> 44100
        ///           2) f_LS_CLK is the main link frequest .... e.g 8.1G --> 810,000,000
        ///           3) Maud is extracted from the Audio Timestamp SDP
        ///           4) Naud is extracted from the Audio Timestamp SDP
        ///           
        ///           
        ///  the equations is sovled for sampling frequency (Fs)...
        ///  
        /// 
        ///                         Maud
        ///             f_LS_CLK (  ----- )
        ///                         Naud
        ///     Fs =   -------------------------
        ///                      512
        /// 
        /// <returns></returns>
        private float calculate_SamplingFreq()
        {
            float Fs = 0.0f;

            // use the m_Maud and m_Naud values to calculate the main link clock speed.

            Fs = (float)(((float)m_LS_CLK * (float)((float)m_Maud / (float)m_Naud)) / (float)512);

            return Fs;
        }


        /// <summary>
        /// Determine the refresh rate by examining the time tick delta between last two Vertical Blanking Symbol sets.
        /// </summary>
        /// <returns></returns>
        private float getRefreshRate()
        {
            //int delta = 0x00;
            float lineRate = 0.0f;

            // always use the last two time ticks...
            if (m_VerticalBlankingStart_TimeTicks.Count >= 2)
            {
                int delta = (int)(m_VerticalBlankingStart_TimeTicks[m_VerticalBlankingStart_TimeTicks.Count - 1] - m_VerticalBlankingStart_TimeTicks[m_VerticalBlankingStart_TimeTicks.Count - 2]);

                // 1,000,000 nSecs == 1 second...  
                //    how many frames will be painted in one second is the question being asked here.
                //       1,000,000,000: represents one second in terms of nSecs
                //       delta: represents the time between two vertical blanking starts, in nSecs
                //       m_clkPeriod: is the main link clock periond in nSecs
                //       
                lineRate = (float)1000000000 / ((float)delta * (float)m_clkPeriod);
            }

            // delta should be in terms of nano seconds...
            return lineRate;
        }


        /// <summary>
        /// calculate the expected audio sample rate... DP_1.4a_Link_CTS.pdf; Section 4.4.4.5 (page 136)... Step 4.
        /// </summary>
        /// 
        ///            n Fs Htotal                                   n Fs Htotal   
        ///    floor( -----------   ) - 4   LGT  Stx  LGT   floor ( -------------) + 4
        ///               Fp                                            Fp
        ///               
        /// 
        ///    where   
        ///        n = number of lines in a frame
        ///        Fs is nominal audio sampling rate e.g. 44.1K
        ///        Htotal is horizontal total pixels (from MSA)
        ///        Fp is pixel rate (Htotal * Vtotal * Frames per second)
        ///        Stx is number of audio samples received from the source DUT over n consecutive lines
        /// 
        private double calculateAudioSampleRate(int numOfLines, float audioSamplingRate, int totalPixels, float pixelRate)
        {
            return Math.Floor((numOfLines * audioSamplingRate * totalPixels) / pixelRate);
        }


        /// <summary>
        /// Do the equation!
        /// </summary>
        private void calculateAudioSampleVariance()
        {
            if (m_FrameLineCounts[m_FrameLineCounts.Count - 1] != m_Vtotal)
                updateErrorMessages("MSA Vtotal field value is different that the number of actual lines in the frame");



            // calculate the m_LS_CLK value... to be used later in this test  -- refers to the DP1.4a section 2.2.5.2 (DP_V1.4_Mem.pdf)
            m_audioSamplingFreq = calculate_SamplingFreq();

            // calculate the refresh rate (frames per second), 30, 60 or some other rate... 
            float refreshRate = getRefreshRate();

            // calculate the pixel rate.... DP1.4_mem.pdf; Section 2.7.3.2;  Pg 310
            float pixelRate = m_Htotal * m_Vtotal * refreshRate;

            // calculate the expected audio sample rate... DP_1.4a_Link_CTS.pdf; Section 4.4.4.5 (page 136)... Step 4.
            int audioSampleRate = (int)calculateAudioSampleRate(m_BSsymbolSequencesCount, m_audioSamplingFreq, m_Htotal, pixelRate );

            if ((m_audioSampleCount < (audioSampleRate - 4)) || (m_audioSampleCount > (audioSampleRate + 4)))
                updateErrorMessages("Invalid audio Sample Rate Detected: Expected (+/- 4): " + audioSampleRate.ToString() + ", Recevied: " + m_audioSampleCount.ToString());


            //else
            //{
            //    updateErrorMessages("MSA Vtotal field value is different that the number of actual lines in the frame");
            //}
        }


        /// <summary>
        /// Get the Sampling Freq ID sub-field value.
        /// </summary>
        /// <param name="dataByte"></param>
        private void getSamplingFreqID(int dataByte)
        {
            m_SamplingFreqID = ((m_laneData[0] & 0x1C) >> 2);

            // Debug Code
            SamplingFreqFields.Add(m_SamplingFreqID.ToString());
        }


        /// <summary>
        /// Process an Audio Info Frame by getting the sampling frequency value at the appropriate state.
        /// </summary>
        private void processAudioInfoFrameSDP()
        {            
            // get the Sampling frequency value at the appropriate time...
            if (m_linkWidth == 1)
            {
                if (m_SDPIndex == 10)
                {
                    getSamplingFreqID(m_laneData[0]);
                    m_audioInfoFrameCount += 1;
                }
            }
            else if ((m_linkWidth == 2) || (m_linkWidth == 4))
            {
                if(m_SDPIndex == 4)
                {
                    getSamplingFreqID(m_laneData[0]);
                    m_audioInfoFrameCount += 1;
                }
            }
        }


        /// <summary>
        /// Extract the Maud and Naud sub-fields.
        /// </summary>
        /// 
        ///       Index                                                    Index                              Index 
        ///   |                                                            |                                  |
        ///   |   +------------+-----------+-----------+-----------+       |  +------------+-----------+      |  +------------+
        ///   |   |  SS        |  SS       |  SS       |  SS       |       |  |  SS        |  SS       |      |  |  SS        |
        ///       +------------+-----------+-----------+-----------+          +------------+------------+        +------------+
        ///   1   |  HB0       |  HB1      |  HB2      |  HB3      |       1  |  HB0       |  HB1      |      1  |  HB0       |
        ///   2   |  PB0       |  PB1      |  PB2      |  PB3      |       2  |  PB0       |  PB1      |      2  |  PB0       |
        ///   3   |  Maud23:16 | Maud23:16 | Maud23:16 | Maud23:16 |       3  |  HB2       |  HB3      |      3  |  HB1       | 
        ///   4   |  Maud15:08 | Maud15:08 | Maud15:08 | Maud15:08 |       4  |  PB2       |  PB3      |      4  |  PB1       | 
        ///   5   |  Maud07:00 | Maud07:00 | Maud07:00 | Maud07:00 |       5  |  Maud23:16 | Maud23:16 |      5  |  HB2       | 
        ///   6   |  0's       | 0's       | 0's       | 0's       |       6  |  Maud15:08 | Maud15:08 |      6  |  PB2       |  
        ///   7   |  PB4       |  PB5      |  PB6      |  PB7      |       7  |  Maud07:00 | Maud07:00 |      7  |  HB3       |
        ///   8   |  Naud23:16 | Naud23:16 | Naud23:16 | Naud23:16 |       8  |  0's       | 0's       |      8  |  PB3       | 
        ///   9   |  Naud15:08 | Naud15:08 | Naud15:08 | Naud15:08 |       9  |  PB4       |  PB5      |      9  |  Maud23:16 | 
        ///   10  |  Naud07:0  | Naud07:0  | Naud07:0  | Naud07:0  |       10 |  Maud23:16 | Maud23:16 |      10 |  Maud15:08 | 
        ///   11  |  0's       | 0's       | 0's       | 0's       |       11 |  Maud15:08 | Maud15:08 |      11 |  Maud07:00 |   
        ///   12  |  PB8       |  PB9      |  PB10     |  PB11     |       12 |  Maud07:00 | Maud07:00 |      12 |  0's       |
        ///       +------------+-----------+-----------+-----------+       13 |  0's       | 0's       |      13 |  PB4       |
        ///       |  SS        |  SS       |  SS       |  SS       |       14 |  PB6       | PB7       |      14 |  Maud23:16 |
        ///       +------------+-----------+-----------+-----------+       15 |  Naud21:16 | Naud21:16 |      15 |  Maud15:08 |
        ///                                                                16 |  Naud15:00 | Naud15:00 |      16 |  Maud07:00 |
        ///                                                                17 |  Naud07:00 | Naud07:00 |      17 |  0's       |
        ///                                                                18 |  0's       | 0's       |      18 |  PB5       |
        ///                                                                19 |  PB8       | PB9       |      19 |  Maud21:16 |
        ///                                                                20 |  Naud21:16 | Naud21:16 |      20 |  Maud15:08 |
        ///                                                                21 |  Naud15:08 | Naud15:08 |      21 |  Maud07:00 |
        ///                                                                22 |  Naud07:00 | Naud07:00 |      22 |  0's       |
        ///                                                                23 |  0's       | 0's       |      23 |  PB6       |
        ///                                                                24 |  PB10      | PB11      |      24 |  Maud21:16 |
        ///                                                                   +------------+-----------+      25 |  Maud15:08 |
        ///                                                                   | SE         |  SE       |      26 |  Maud07:00 |
        ///                                                                   +------------+-----------+      27 |  0's       |
        ///                                                                                                   28 |  PB7       |
        ///                                                                                                   29 |  Naud21:16 |
        ///                                                                                                   30 |  Naud15:08 |
        ///                                                                                                   31 |  Naud07:00 |
        ///                                                                                                   32 |  0's       |
        ///                                                                                                   33 |  PB8       |
        ///                                                                                                   34 |  Naud21:16 |
        ///                                                                                                   35 |  Naud15:08 |
        ///                                                                                                   36 |  Naud07:00 |
        ///                                                                                                   37 |  0's       |
        ///                                                                                                   38 |  PB9       |
        ///                                                                                                   39 |  Naud21:16 |
        ///                                                                                                   40 |  Naud15:08 |
        ///                                                                                                   41 |  Naud07:00 |
        ///                                                                                                   42 |  0's       |
        ///                                                                                                   43 |  PB10      |
        ///                                                                                                   44 |  Naud21:16 |
        ///                                                                                                   45 |  Naud15:08 |
        ///                                                                                                   46 |  Naud07:00 |
        ///                                                                                                   47 |  0's       |
        ///                                                                                                   48 |  PB11      |
        ///                                                                                                      +------------+ 
        ///                                                                                                      |  SE        |
        ///                                                                                                      +------------+ 
        private void processAudioTimeStamp()
        {

            if (m_SDPIndex == 1)
            {
                m_AudioTSCount += 1;
            }
            else
            {
                if (m_linkWidth == 1)
                {
                    if (m_SDPIndex == 9)
                        m_Maud = m_Htotal = m_laneData[0] << 16;
                    else if (m_SDPIndex == 10)
                        m_Maud = m_Htotal |= m_laneData[0] << 8;
                    else if (m_SDPIndex == 11)
                        m_Maud = m_Htotal |= m_laneData[0];


                    else if (m_SDPIndex == 29)
                        m_Naud = m_Htotal = m_laneData[0] << 16;
                    else if (m_SDPIndex == 30)
                        m_Naud = m_Htotal |= m_laneData[0] << 8;
                    else if (m_SDPIndex == 31)
                        m_Naud = m_Htotal |= m_laneData[0];

                }
                else if (m_linkWidth == 2)
                {
                    if (m_SDPIndex == 5)
                        m_Maud = m_Htotal = m_laneData[0] << 16;
                    else if (m_SDPIndex == 6)
                        m_Maud = m_Htotal |= m_laneData[0] << 8;
                    else if (m_SDPIndex == 7)
                        m_Maud = m_Htotal |= m_laneData[0];


                    else if (m_SDPIndex == 15)
                        m_Naud = m_Htotal = m_laneData[0] << 16;
                    else if (m_SDPIndex == 16)
                        m_Naud = m_Htotal |= m_laneData[0] << 8;
                    else if (m_SDPIndex == 17)
                        m_Naud = m_Htotal |= m_laneData[0];
                }
                else    // if (m_linkWidth == 4)
                {
                    if (m_SDPIndex == 3)
                        m_Maud = m_Htotal = m_laneData[0] << 16;
                    else if (m_SDPIndex == 4)
                        m_Maud = m_Htotal |= m_laneData[0] << 8;
                    else if (m_SDPIndex == 5)
                        m_Maud = m_Htotal |= m_laneData[0];


                    else if (m_SDPIndex == 8)
                        m_Naud = m_Htotal = m_laneData[0] << 16;
                    else if (m_SDPIndex == 9)
                        m_Naud = m_Htotal |= m_laneData[0] << 8;
                    else if (m_SDPIndex == 10)
                        m_Naud = m_Htotal |= m_laneData[0];
                }
            }
        }


        /// <summary>
        /// Increment the Audio SPD count (once per SDP)...
        /// </summary>
        private void processAudioSDP()
        {
            if (m_frameCount >= 0)   // the '>=' was added to count to Audio Stream SDPs BEFORE the first Vertical BS.
            {
                if (m_linkWidth == 1)
                {
                    if (m_SDPIndex == 4)
                    {
                        m_audioSampleCount += 1;
                    }
                }
                else if ((m_linkWidth == 2) || (m_linkWidth == 4))
                {
                    if (m_SDPIndex == 2)
                    {
                        m_audioSampleCount += 1;
                    }
                }
            }
        }


        /// <summary>
        /// Extract HB1 sub-field value
        /// </summary>
        /// 
        ///   m_SDPIndex                                  m_SDPIndex                m_SDPIndex 
        ///   |                                           |                         |        
        ///   |   +-------+-------+-------+-------+       |  +-------+-------+      |  +-------+
        ///   |   |  SS   |  SS   |  SS   |  SS   |       |  |  SS   |  SS   |      |  |  SS   |
        ///       +-------+-------+-------+-------+          +-------+-------+         +-------+
        ///   1   |  HB0  |  HB1  |  HB2  |  HB3  |       1  |  HB0  |  HB1  |      1  |  HB0  |
        ///   2   |  PB0  |  PB1  |  PB2  |  PB3  |       2  |  PB0  |  PB1  |      2  |  PB0  |
        ///   3   |  DB0  |  DB4  |  DB8  |  DB12 |       3  |  DB0  |  DB4  |      3  |  HB1  | 
        ///   4   |  DB1  |  DB5  |  DB9  |  DB13 |       4  |  DB1  |  DB5  |      4  |  PB1  | 
        ///   5   |  DB2  |  DB6  |  DB10 |  DB14 |       5  |  DB2  |  DB6  |      5  |  HB2  | 
        ///   6   |  DB3  |  DB7  |  DB11 |  DB15 |       6  |  DB3  |  DB7  |      6  |  PB2  |  
        ///   7   |  PB4  |  PB5  |  PB6  |  PB7  |       7  |  PB4  |  PB5  |      7  |  HB3  |
        ///   8   |  DB16 |  DB20 |  DB24 |  DB28 |       8  |  DB8  |  DB12 |      8  |  PB3  | 
        ///   9   |  DB17 |  DB21 |  DB25 |  DB29 |       9  |  DB9  |  DB13 |      9  |  DB0  | 
        ///   10  |  DB18 |  DB22 |  DB26 |  0's  |       10 |  DB10 |  DB14 |      10 |  DB1  | 
        ///   11  |  DB19 |  DB23 |  DB27 |  0's  |       11 |  DB11 |  DB15 |      11 |  DB2  |   
        ///   12  |  PB8  |  PB9  |  PB10 |  PB11 |       12 |  PB6  |  PB7  |      12 |  DB3  |
        ///       +-------+-------+-------+-------+       13 |  DB16 |  DB20 |      13 |  PB4  |
        ///       |  SE   |  SE   |  SE   |  SE   |       14 |  DB17 |  DB21 |      14 |  DB4  |
        ///       +-------+-------+-------+-------+       15 |  DB18 |  DB22 |      15 |  DB5  |
        ///                                               16 |  DB19 |  DB23 |      16 |  DB6  |
        ///                                               17 |  PB8  |  PB9  |      17 |  DB7  |
        ///                                               18 |  DB24 |  DB28 |      18 |  PB5  |
        ///                                               19 |  DB25 |  DB29 |      19 |  DB8  |
        ///                                               20 |  DB26 |  0's  |      20 |  DB9  |
        ///                                               21 |  DB27 |  0's  |      21 |  DB10 |
        ///                                               22 |  PB10 |  PB11 |      22 |  DB11 |
        ///                                                  +-------+-------+      23 |  PB6  |
        ///                                                  |  SE   |  SE   |      24 |  DB12 |
        ///                                                  +-------+-------+      25 |  DB13 |
        ///                                                                         26 |  DB14 |
        ///                                                                         27 |  DB15 |
        ///                                                                         28 |  PB7  |
        ///                                                                         29 |  DB16 |
        ///                                                                         30 |  DB17 |
        ///                                                                         31 |  DB18 |
        ///                                                                         32 |  DB19 |
        ///                                                                         33 |  PB8  |
        ///                                                                         34 |  DB20 |
        ///                                                                         35 |  DB21 |
        ///                                                                         36 |  DB22 |
        ///                                                                         37 |  DB23 |
        ///                                                                         38 |  PB9  |
        ///                                                                         39 |  DB24 |
        ///                                                                         40 |  DB25 |
        ///                                                                         41 |  DB26 |
        ///                                                                         42 |  DB27 |
        ///                                                                         43 |  PB10 |
        ///                                                                         44 |  DB28 |
        ///                                                                         45 |  DB29 |
        ///                                                                         46 |  0's  |
        ///                                                                         47 |  0's  |
        ///                                                                         48 |  PB11 |
        ///                                                                            +-------+
        ///                                                                            |  SE   |
        ///                                                                            +-------+
        /// </summary>
        private int getHdrByte1Value()
        {
            int HB1_Value = 0x00;
            if (m_linkWidth == 1)
            {
                if (m_SDPIndex == 3)
                {
                    HB1_Value = m_laneData[0];
                }
            }
            else if ((m_linkWidth == 2) || (m_linkWidth == 4))
            {
                if (m_SDPIndex == 1)
                {
                    HB1_Value = m_laneData[1];
                }
            }

            return HB1_Value;
        }


        /// <summary>
        /// Get the HB1 byte value 
        /// </summary>
        private void processSDPHeaderBytes(byte[] stateData)
        {
            int HB1Value = getHdrByte1Value();

            if (HB1Value > 0x00)
            {
                if (HB1Value == 0x84)
                {
                    m_inAudioInfoFrame = true;
                }
                else if (HB1Value == 0x01)
                {
                    m_inAudioTimeStamp = true;
                }
                else if (HB1Value == 0x02)
                {
                    m_inAudioSDP = true;
                }
                else
                {
                    m_inOtherSDP = true;
                }

                m_unknownSDP = false;
            } 
        }


        /// <summary>
        ///  Process MSA SDP
        /// </summary>
        ///
        ///   m_SDPIndex                                                     m_SDPIndex                         m_SDPIndex 
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
        private void processMSAState()
        {
            if (m_SDPIndex == 1)
            {
                m_MSACount += 1;
            }
            else
            {
                //if (m_linkWidth == 1)
                //{
                //    if (m_SDPIndex == 4)
                //        m_Htotal = m_laneData[0] << 8;

                //    else if (m_SDPIndex == 5)
                //        m_Htotal |= m_laneData[0];

                //    else if (m_SDPIndex == 6)
                //        m_Vtotal = m_laneData[0] << 8;

                //    else if (m_SDPIndex == 7)
                //        m_Vtotal |= m_laneData[0];
                //}
                //else if (m_linkWidth == 2)
                //{
                //    if (m_SDPIndex == 4)
                //        m_Htotal = m_laneData[0] << 8;

                //    else if (m_SDPIndex == 5)
                //        m_Htotal |= m_laneData[0];

                //    else if (m_SDPIndex == 6)
                //        m_Vtotal = m_laneData[0] << 8;

                //    else if (m_SDPIndex == 7)
                //        m_Vtotal |= m_laneData[0];
                //}
                //if (m_linkWidth == 4)
                //{
                    if ((m_SDPIndex >= 4) && (m_SDPIndex <= 7))
                    {
                        if (m_SDPIndex == 4)
                            m_Htotal = m_laneData[0] << 8;

                        else if (m_SDPIndex == 5)
                            m_Htotal |= m_laneData[0];

                        else if (m_SDPIndex == 6)
                            m_Vtotal = m_laneData[0] << 8;

                        else if (m_SDPIndex == 7)
                            m_Vtotal |= m_laneData[0];

                        //// for testing...
                        //if (m_SDPIndex == 7)
                        //    MSASubFields.Add(m_Htotal.ToString() + "," + m_Vtotal.ToString());
                    }
                //}
            }
        }


        /// <summary>
        /// Process the Secondary Data Packets... extracting information that we need!
        /// </summary>
        private void processSDPState(byte[] stateData)
        {
            if (m_inMSA)
            {
                processMSAState();
            }
            else
            {
                if (m_unknownSDP)
                {
                    processSDPHeaderBytes(stateData); 
                }
                else
                {
                    if (m_inAudioInfoFrame)
                    {
                        processAudioInfoFrameSDP();
                    }
                    else if (m_inAudioTimeStamp)
                    {
                        processAudioTimeStamp();
                    }
                    else if (m_inAudioSDP)
                    {
                        updateTotalAudioStreamSDPCount();
                        processAudioSDP();
                   }
                }
            }

            m_prevSDPStateValue = m_laneData[0];
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateData"></param>
        /// 
        /// Assumes 
        ///     1 - a well formatted state capture with no missing states.
        ///     2 - BS sequence will always be four states... BS - BF - BF - BS... followed by VBID...
        /// <returns></returns>
        private bool processLane(byte[] stateData) //, string laneName)
        {
            bool status = true;

            try
            {
                getStateData(stateData, m_linkWidth);
                if ((m_laneData[0] == 0x1BC) || (m_laneData[0] == 0x11C) || (m_laneData[0] == 0x17C))       // BS or SR, BF Ctrl Chars
                {
                    if (!m_inBSSymbolSequence)
                    {
                        m_inBSSymbolSequence = true;
                    }
                }
                else if (m_inBSSymbolSequence)
                {
                    //
                    // process the first state AFTER the BS symbol sequence... which is the VB-ID state.
                    //
                    if ((m_laneData[0] & 0x01) == 0x01)                 // if the vertical blanking flag is set in the VB-ID state.
                    {
                        if (!m_curBSSectionIsVertical)
                        {
                            // store the time for the vertical blanking start --- used for calculating refresh rate...
                            m_VerticalBlankingStart_TimeTicks.Add((long)getTimeTicks(stateData));

                            // increment the count of vertical blanking symbol sequences.
                            m_frameCount += 1;

                            if (m_frameCount == 1)
                                m_VerticalBS_StartIndex = m_curStateIndex;

                            // only want to increment once per vertical blanking sections(s)
                            if ((m_frameCount > 1) && (m_horBSCount > 0))//&& (m_verticalBSSequenceCount == 0x01))
                            {
                                // for debugging...
                                m_FrameLineCounts.Add(m_BSsymbolSequencesCount);
                                m_AudioSDPCounts.Add(m_audioSampleCount);

                                // do the +/- 4 variance rate equation on the previous frame...
                                calculateAudioSampleVariance();

                                // get ready for the next frame's Audio Info Frame and MSA...
                                resetSDPSubFields();

                                //// reset the count for the next frame!
                                m_audioSampleCount = 0x00;
                                m_horBSCount = 0x00;
                            }
                            else
                            {
                                m_audioSampleCount = 0x00;
                            }

                            m_curBSSectionIsVertical = true;
                            m_BSsymbolSequencesCount = 0;

                            //// FOR DEBUGGING ONLY
                            //updateErrorMessages("Frame " + m_frameCount.ToString() +  ":" + m_BSsymbolSequencesCount.ToString());
                        }
                    }
                    else
                    {
                        if (m_curBSSectionIsVertical)
                            m_curBSSectionIsVertical = false;

                        m_horBSCount += 1;
                    }

                    if (m_frameCount > 0)
                        m_BSsymbolSequencesCount += 1;                      // only count lanes after we've seen the first vertical blanking section start...
                    m_inBSSymbolSequence = false;                       // We are no longer in the BS Symbol sequence
                }
                else
                {
                    if (m_laneData[0] == 0x15C)                         // SS Ctrl Character
                    {
                        if (!m_inSDP)
                            m_inSDP = true;
                        else
                            m_inMSA = true;
                    }
                    else if (m_laneData[0] == 0x1FD)                    // SE Ctrl Character
                    {
                        resetSDPVariables();
                    }
                    else
                    {
                        if (m_inSDP)
                        {
                            m_SDPIndex += 1;
                            processSDPState(stateData);
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
            resetSDPVariables();
            resetSDPSubFields();

            m_linkWidth = m_commonFuncs.GetConfigParameterValue(ConfigParameters, "WIDTH");
            string speedID = m_commonFuncs.GetConfigParameterValue_String(ConfigParameters, "SPEED");
            m_LS_CLK = getMainLinkClk(speedID); //m_commonFuncs.GetConfigParameterValue(ConfigParameters, "Speed"));
            m_clkPeriod = getClkPeriod(speedID);


            m_horBSCount = 0;
            //m_verticalBSSequenceCount = 0;
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
                    status = processLane(stateData);
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
            subStrings.Add("Number of MSA SDPs Processed: " + m_MSACount.ToString());
            subStrings.Add("Number of Audio Info Frame SDPs Processed: " + m_audioInfoFrameCount.ToString());
            subStrings.Add("Number of Audio SDPs Processed: " + m_audioSampleCount_Total.ToString());
            subStrings.Add("1st Vertical Frame Offset: " + m_VerticalBS_StartIndex.ToString());
            //subStrings.Add("Number of BS Symbol Sequences Processed: " + m_BSsymbolSequencesCount.ToString());




            //
            // Debugging Code!
            //

            StringBuilder SamplingFreqFields_SB = new StringBuilder();
            //StringBuilder MSA_SubFields = new StringBuilder();

            int i = 0;
            SamplingFreqFields_SB.Append("Freq Sampling IDs: ");
            foreach (string s in SamplingFreqFields)
                SamplingFreqFields_SB.Append(i++.ToString() +": " + s + ", ");
            SamplingFreqFields_SB.Length -= 2;
            subStrings.Add(SamplingFreqFields_SB.ToString());

            i = 0;
            StringBuilder FrameCounts_SB = new StringBuilder();
            FrameCounts_SB.Append("Frame Line Counts: ");
            foreach (int cnt in m_FrameLineCounts)
                FrameCounts_SB.Append(i++.ToString() + ": " + cnt.ToString() + ", ");
            FrameCounts_SB.Length -= 2;
            subStrings.Add(FrameCounts_SB.ToString());

            
            i = 0;
            StringBuilder AudioSDPCounts_SB = new StringBuilder();
            AudioSDPCounts_SB.Append("Audio Stream SDP Counts: ");
            foreach (int cnt in m_AudioSDPCounts)
                AudioSDPCounts_SB.Append(i++.ToString() + ": " + cnt.ToString() + ", ");
            AudioSDPCounts_SB.Length -= 2;
            subStrings.Add(AudioSDPCounts_SB.ToString());



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

            description.Add(" This test verifies Audio Samples are received at an anticapted rate. ");
            description.Add(" ");

            return description;
        }

        #endregion // Public Methods
    }
}
