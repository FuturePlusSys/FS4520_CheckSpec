using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS4500_MainLink_MST
{
    enum StateID { UnknownStart, Unknown, BS, SR, VBID, MVID, MAUD, MSA, SDP, Blanking_Stuff, BE, Pixel1, Pixel, Video_Stuff, VCPF, Training1, Training2, Training3, Training4, MTP_HDR, UncompressedVC }

    internal class Test_8
    {
        #region Members
        List<string> m_errMsgs = new List<string>();

        private int m_curStateIndex = -1;
        private StateID m_prevStateID = StateID.Unknown;
        private StateID m_curStateID = StateID.Unknown;

        private int m_linkWidth = 4;
        private int m_VCPFSequenceCount = 0;

        private bool m_BSProcessed = false;
        private bool m_VBIDProcessed = false;
        private bool m_MVIDProcessed = false;
        private bool m_MAUDProcessed = false;


        UtilityFunctions m_utilFuncs = new UtilityFunctions();
        #endregion // Members

        #region Constructor(s)

        public Test_8()
        {
        }

        #endregion // Constructor(s)

        #region Private Methods

        /// <summary>
        /// Update the current state ID member of the first 'usable' state
        /// </summary>
        /// <param name="eventCode"></param>
        private void updateIntialCurrentStateID(int eventCode)
        {
            StateID startStateID = StateID.UnknownStart;

            startStateID = getSMState(m_curStateID, eventCode);
            if ((startStateID == StateID.BE) || (startStateID == StateID.BS))
                m_curStateID = startStateID;
        }


        /// <summary>
        /// Returns a boolean value indicating if the specified event code is for a VCPF
        /// </summary>
        /// <param name="eventCode"></param>
        /// <returns></returns>
        private bool isVCPF(int eventCode)
        {
            bool status = false;

            if (eventCode == 0x38) // if ((eventCode == 0xB8 ) ||  (eventCode == 0xF8 ))
            {
                status = true;
            }

            return status;
        }


        /// <summary>
        /// Get the state machine state ID for the current state
        /// </summary>
        /// <param name="listingStateID"></param>
        /// <param name="eventCode"></param>
        /// <returns></returns>
        private StateID getSMState(StateID prevListingStateID, int eventCode)
        {
            StateID nextState = StateID.Unknown;

            switch (eventCode)
            {
                case 0x88:
                case 0xC8:
                    nextState = StateID.Pixel;
                    break;

                case 0x4A:
                case 0x0A:
                    nextState = StateID.BS;
                    break;

                case 0x55:
                case 0x15:
                    nextState = StateID.BE;
                    break;

                case 0x49:
                case 0x09:
                    nextState = StateID.VBID;
                    break;

                case 0x4C:
                case 0x0C:
                    nextState = StateID.MVID;
                    break;

                case 0x51:
                case 0x11:
                    nextState = StateID.MAUD;
                    break;

                case 0x5C:
                case 0x1C:
                    nextState = StateID.MSA;
                    break;

                case 0x60:
                case 0x20:
                    nextState = StateID.SDP;  // Audio Stream
                    break;

                case 0x64:
                case 0x24:
                    nextState = StateID.SDP;  // Audio TS 
                    break;

                case 0x6B:
                case 0x2B:
                    nextState = StateID.SDP;  // Audio Copy Mgmt SDP
                    break;

                case 0x72:
                case 0x32:
                    nextState = StateID.SDP;  // ISRC SDP
                    break;

                case 0x52:
                case 0x12:
                    nextState = StateID.SDP;  // VSC SDP
                    break;

                case 0x7C:
                case 0x3C:
                    nextState = StateID.SDP;  // Extension SDP
                    break;

                case 0x54:
                case 0x14:
                    nextState = StateID.SDP;  // Info Frame SDP
                    break;

                case 0x69:
                case 0x29:
                    nextState = StateID.SDP;  // Camera SDP
                    break;

                case 0x61:
                case 0x21:
                    nextState = StateID.SDP;  // PPS SDP
                    break;

                case 0x62:
                case 0x22:
                    nextState = StateID.SDP;  // VCS EXT VESA SDP
                    break;

                case 0x65:
                case 0x25:
                    nextState = StateID.SDP;  // VCS EXT CEA SDP
                    break;

                case 0x73:
                case 0x33:
                    nextState = StateID.Blanking_Stuff;  // Stream Fill 
                    break;

                case 0xB3:
                case 0xF3:
                    nextState = StateID.Video_Stuff;  // Stream Fill SF during Video SDP
                    break;

                case 0x78:
                case 0x38:
                    nextState = StateID.VCPF;  // VCPF during Video
                    break;

                case 0xF8:
                case 0xB8:
                    nextState = StateID.SDP;  // VCPF/RG during Video
                    break;

                case 0x0B:
                    nextState = StateID.SR;  // SR
                    break;

                case 0x01:
                    nextState = StateID.Training1;  // TPS1
                    break;

                case 0x02:
                    nextState = StateID.Training2;  // TPS2
                    break;

                case 0x03:
                    nextState = StateID.Training3;  // TPS3
                    break;

                case 0x04:
                    nextState = StateID.Training4;  // TPS4
                    break;

                case 0x3F:
                    nextState = StateID.MTP_HDR;  // MTP Header = 0
                    break;

                case 0x34:
                    nextState = StateID.MTP_HDR;  // MTP Header = NOT equal to SR, 0 OR ACT
                    break;

                case 0x31:
                    nextState = StateID.MTP_HDR;  // MTP Header not = ACT
                    break;

                case 0x07:
                    nextState = StateID.UncompressedVC;  // Uncompressed VC
                    break;

                default:
                    nextState = StateID.Unknown;
                    break;
            }

            return nextState;
        }


        /// <summary>
        /// Validate the BS state is in an expected state listing location.
        /// </summary>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_VCPF(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            m_VCPFSequenceCount += 1;
            if (m_linkWidth == 1)
            {
                if ((prevStateID == StateID.VCPF) && (m_VCPFSequenceCount > 4))
                    errMsg.Add("Invalid number of VCPF Symbols Encountered.");
            }
            else if (m_linkWidth == 2)
            {
                if ((prevStateID == StateID.VCPF) && (m_VCPFSequenceCount >= 2))
                    errMsg.Add("Invalid number of VCPF Symbols Encountered.");
            }
            else //if (m_linkWidth == 4)
            {
                if ((prevStateID == StateID.VCPF) && (m_VCPFSequenceCount >= 1))
                    errMsg.Add("Invalid number of VCPF Symbols Encountered.");
            }

            return errMsg;
        }


        /// <summary>
        /// Validate the BS state is in an expected state listing location.
        /// </summary>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// NOTE:  This one state is different in that two Blank Sections can be 
        ///        sequencially located.
        /// <returns></returns>
        private List<string> validate_BS(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            //if ((m_prevStateID == StateID.Pixel) || (m_prevStateID == StateID.MSA) || (m_prevStateID == StateID.SDP) || (m_prevStateID == StateID.Video_Stuff) || (prevStateID == StateID.Unknown))
            if ((m_prevStateID == StateID.Pixel) || (m_prevStateID == StateID.MSA) || (m_prevStateID == StateID.SDP) || (prevStateID == StateID.VCPF) || (m_prevStateID == StateID.Video_Stuff) || (m_prevStateID == StateID.Blanking_Stuff) || (prevStateID == StateID.Unknown))
            {
                // TBD...
            }
            else
            {
                errMsg.Add("Unexpected Blanking Start State Sequence");
            }
            return errMsg;
        }


        /// <summary>
        /// Validate the MVID state is in an expected state listing location.
        /// </summary>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_VBID(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            if ((prevStateID == StateID.BS) || (prevStateID == StateID.VCPF))
            {
                // TBD...
            }
            else
            {
                errMsg.Add("Unexpected MVID State Sequence");
            }
            return errMsg;
        }


        /// <summary>
        /// Validate the MVID state is in an expected state listing location.
        /// </summary>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_MVID(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            if ((prevStateID == StateID.VBID) || (prevStateID == StateID.VCPF))
            {
                // TBD...
            }
            else
            {
                errMsg.Add("Unexpected VBID State Sequence");
            }
            return errMsg;
        }


        /// <summary>
        /// Validate the MVID state is in an expected state listing location.
        /// </summary>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_MAUD(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            if ((prevStateID == StateID.MVID) || (prevStateID == StateID.VCPF))
            {
                // TBD...
            }
            else
            {
                errMsg.Add("Unexpected MAUD State Sequence");
            }
            return errMsg;
        }


        /// <summary>
        /// Validate the Main Stream Attribute  state is in an expected state listing location.
        /// </summary>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_MSA(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            if ((prevStateID == StateID.MSA) || (prevStateID == StateID.MAUD) || (prevStateID == StateID.SDP) || (prevStateID == StateID.Blanking_Stuff) || (prevStateID == StateID.VCPF))
            {
                if ((m_BSProcessed == true) && (m_VBIDProcessed == true) && (m_MVIDProcessed == true) && (m_MAUDProcessed == true))
                {
                    // TBD  -- check the MSA starts with a double SS in each lane, and ends with SE in each lane...
                }
                else
                {
                    errMsg.Add("Invalid MSA Packet location; VBID, MVID or MAUD were not processed prior to MSA Packet");
                }
            }
            else
            {
                errMsg.Add("Invalid MSA Packet location.");
            }

            return errMsg;
        }


        /// <summary>
        /// Validate the Secondary Data Packet (SDP) state is in an expected state listing location.
        /// </summary>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_SDP(int eventCode, StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            if ((prevStateID == StateID.MAUD) || (prevStateID == StateID.MSA) || (prevStateID == StateID.Video_Stuff) || (prevStateID == StateID.VCPF))
            {
                if ((m_BSProcessed == true) && (m_VBIDProcessed == true) && (m_MVIDProcessed == true) && (m_MAUDProcessed == true))
                {
                    // TBD  -- check the SDP starts with a single SS in each lane, and ends with SE in each lane...

                    // use the event code to do more specific SDP checking???
                }
                else
                {
                    errMsg.Add("Invalid SDP Packet location; VBID, MVID or MAUD were not processed prior to MSA Packet");
                }
            }
            else
            {
                errMsg.Add("Invalid SDP Packet location.");
            }

            return errMsg;
        }


        /// <summary>
        /// Validate a blanking section Stuff packet.
        /// </summary>
        /// <param name="eventCode"></param>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_BS_Stuff(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            if ((prevStateID == StateID.Blanking_Stuff) || (prevStateID == StateID.MAUD) || (prevStateID == StateID.MSA) || (prevStateID == StateID.SDP) || (prevStateID == StateID.VCPF))
            {
                // TBD  -- check the SDP starts with a single SS in each lane, and ends with SE in each lane...
            }
            else
            {
                errMsg.Add("Invalid Blanking Section Stuff Packet location.");
            }

            return errMsg;
        }


        /// <summary>
        /// Validate the first state in a video section.
        /// </summary>
        /// <param name="eventCode"></param>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_Pixel1(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            if ((prevStateID == StateID.BE) || (prevStateID == StateID.VCPF))
            {
                // TBD  -- check the SDP starts with a single SS in each lane, and ends with SE in each lane...
            }
            else
            {
                errMsg.Add("Invalid Blanking Section Stuff Packet location.");
            }

            return errMsg;
        }

        /// <summary>
        /// Validate the 2nd (or later) Pixel state in a video section.
        /// </summary>
        /// <param name="eventCode"></param>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_Pixel(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            if ((prevStateID == StateID.Pixel1) || (prevStateID == StateID.Pixel) || (prevStateID == StateID.Video_Stuff) || (prevStateID == StateID.VCPF))
            {
                // TBD  -- check the SDP starts with a single SS in each lane, and ends with SE in each lane...
            }
            else
            {
                errMsg.Add("Invalid Blanking Section Stuff Packet location.");
            }

            return errMsg;
        }


        /// <summary>
        /// Validate the video section stuff.
        /// </summary>
        /// <param name="eventCode"></param>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        private List<string> validate_VideoStuff(StateID prevStateID, StateID curStateID, byte[] stateData)
        {
            List<string> errMsg = new List<string>();

            if ((prevStateID == StateID.Pixel) || (prevStateID == StateID.Video_Stuff) || (prevStateID == StateID.VCPF))
            {
                // TBD  -- check the SDP starts with a single SS in each lane, and ends with SE in each lane...
            }
            else
            {
                errMsg.Add("Invalid Blanking Section Stuff Packet location.");
            }

            return errMsg;
        }

        /// <summary>
        /// Validate the current state based on what the previous state was.
        /// </summary>
        /// <param name="prevStateID"></param>
        /// <param name="curStateID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        List<string> valididateState(StateID prevStateID, StateID curStateID, byte[] stateData, int eventCode)
        {
            List<string> errMsgs = new List<string>();

            switch (curStateID)
            {
                case StateID.UnknownStart:
                    // TBD: do nothing...
                    break;

                case StateID.VCPF:
                    errMsgs.AddRange(validate_VCPF(prevStateID, curStateID, stateData));
                    break;

                case StateID.BS:
                    m_BSProcessed = true;
                    m_VBIDProcessed = false;
                    m_MVIDProcessed = false;
                    m_MAUDProcessed = false;

                    errMsgs.AddRange(validate_BS(prevStateID, curStateID, stateData));
                    break;

                case StateID.VBID:
                    m_VBIDProcessed = true;
                    errMsgs.AddRange(validate_VBID(prevStateID, curStateID, stateData));
                    break;

                case StateID.MVID:
                    m_MVIDProcessed = true;
                    errMsgs.AddRange(validate_MVID(prevStateID, curStateID, stateData));
                    break;

                case StateID.MAUD:
                    m_MAUDProcessed = true;
                    errMsgs.AddRange(validate_MAUD(prevStateID, curStateID, stateData));
                    break;

                case StateID.MSA:
                    errMsgs.AddRange(validate_MSA(prevStateID, curStateID, stateData));
                    break;

                case StateID.SDP:
                    errMsgs.AddRange(validate_SDP(eventCode, prevStateID, curStateID, stateData));
                    break;

                case StateID.Blanking_Stuff:
                    errMsgs.AddRange(validate_BS_Stuff(prevStateID, curStateID, stateData));
                    break;



                case StateID.BE:
                    m_BSProcessed = false;
                    break;

                case StateID.Pixel1:
                    errMsgs.AddRange(validate_Pixel1(prevStateID, curStateID, stateData));
                    break;

                case StateID.Pixel:
                    errMsgs.AddRange(validate_Pixel(prevStateID, curStateID, stateData));
                    break;

                case StateID.Video_Stuff:

                    errMsgs.AddRange(validate_VideoStuff(prevStateID, curStateID, stateData));
                    break;

                default:
                    // don't change a thing... some of these like TPS1,2,3,4 happen before the link is up... 
                    break;
            }

            // reset the symbol count
            if (curStateID != StateID.VCPF)
                m_VCPFSequenceCount = 0;

            return errMsgs;
        }


        #endregion // Private Methods

        #region Public Methods
        /// <summary>
        /// Initialize variables used for test 1
        /// </summary>
        public bool Initialize(List<string> ConfigParameters)
        {
            // Test 8 Variables
            m_errMsgs = new List<string>();
            m_curStateIndex = -1;

            foreach (string param in ConfigParameters)
            {
                if (param.StartsWith("Width:"))
                {
                    string[] comps = param.Split(new char[] { ':' });
                    if (comps.Length == 2)
                        m_linkWidth = int.Parse(comps[1]);
                    break;
                }
            }

            m_VCPFSequenceCount = 0;

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
            m_prevStateID = m_curStateID;
            m_curStateIndex += 1;


            //int eventCode =  (byte)(((stateData[8] & 0x03)<<6)  | stateData[9] >> 2);
            int eventCode = (byte)(stateData[9] >> 2);


            if (m_curStateID == StateID.Unknown)
                updateIntialCurrentStateID(eventCode);   // updates the m_curStateID if state is BS or BE
            else
                m_curStateID = getSMState(m_curStateID, eventCode);


            List<string> errMsgs = valididateState(m_prevStateID, m_curStateID, stateData, eventCode);
            if (errMsgs.Count > 0)
                m_errMsgs.AddRange(errMsgs);

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
