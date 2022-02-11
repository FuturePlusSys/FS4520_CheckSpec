using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS4500_VTests_ML_Functions;

namespace FS4500_ML_VideoValidation
{

    enum Trace_Section_ID { Active, Blanking, Unknown }
    enum ActiveVideo_Section_ID { TransmitUnit, Stuffing, Unknown }

    /// <summary>
    ///  This class verifies that Active Video Stuffing framing begins with 
    ///  an FS control characters and ends with an FS control character on all 
    ///  active data lanes.  All data states between the two control characters 
    ///  are verified of containing a value of 0x00.
    ///  
    ///  The class detects state sequences where the FS is encountered but the FE
    ///  is missing, The class also detects the oppisite condition in which an FE 
    ///  is encountered without first an accopanying FS control character.
    ///  
    ///  The class checks the number of lanes per the setup configuration (1, 2 or 4) for having 
    ///  all lanes containing the same value of its adjacent lanes.
    ///  
    ///  This class does not check for short data stuffs (one 1 state) in which only the 
    ///  FS control character is contained in the data stuffing segment.
    ///  
    /// </summary>
    class ActiveVideoStuffingSymbolFraming
    {
        #region Members

        private int MaxErrors { get; set; } = 10;

        private int m_ErrorCount = 0;
        private List<string> m_errMsgs = new List<string>();

        private List<string> m_testInfo = new List<string>();
        private uint m_curStateIndex = 0x00;

        private int m_linkWidth = -1;

        private ML_Common_Functions m_commonFuncs = null;

        private int[] m_StartSymbol = new int[4]; // max of four lanes;
        private int[] m_EndSymbol = new int[4];   // max of four lanes;
        private int[] m_StuffSymbolValue = new int[4]; // max of four lanes;
        private bool[] m_inStuffingSection = new bool[4]; //false;
        //private ActiveVideo_Section_ID[] m_VideoSection = new ActiveVideo_Section_ID[4];

        private int[] m_laneValues = new int[4];

        private int[] m_stuffingSegmentCount = new int[4];
        private Trace_Section_ID[] m_sectionID = new Trace_Section_ID[4]; //.Unknown;
        private bool m_inhibitChecking = false;

        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public ActiveVideoStuffingSymbolFraming()
        {
            m_commonFuncs = new ML_Common_Functions();
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
                status = "Passed: No errors were encountered.";
            }
            else
            {
                status = "Failed: Errors Encountered";
            }

            return status;
        }

        /// <summary>
        /// Update the error message and error count
        /// </summary>
        /// <param name="errorMsg"></param>
        private void updateErrorMessages(string errorMsg)
        {
            if (!m_inhibitChecking)
            {
                m_errMsgs.Add(errorMsg);
                m_ErrorCount += 1;
            }
        }


        /// <summary>
        /// TBD
        /// </summary> 
        private void resetTestingVariables()
        {
            Array.Clear(m_StartSymbol, 0, m_StartSymbol.Length);
            Array.Clear(m_EndSymbol, 0, m_EndSymbol.Length);
            Array.Clear(m_StuffSymbolValue, 0, m_StuffSymbolValue.Length);
            Array.Clear(m_inStuffingSection, 0, m_inStuffingSection.Length); // = false;
            Array.Clear(m_stuffingSegmentCount, 0, m_stuffingSegmentCount.Length);

            Array.Clear(m_laneValues, 0, m_laneValues.Length);
            m_inhibitChecking = false;
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

            resetTestingVariables();

            for(int i=0; i<4; i++)
                m_sectionID[i] = Trace_Section_ID.Unknown;
        }


        /// <summary>
        /// Verifys adjacent active lanes contain the same value.
        /// </summary>
        /// <param name="laneIndex"></param>
        /// <param name="laneData"></param>
        /// <param name="laneValues"></param>
        /// <returns></returns>
        private bool verifyLaneData(int laneIndex, int laneData, int[] laneValues)
        {
            bool status = true;
            if (laneIndex == m_linkWidth)
            {
                for (int i = 0; i < m_linkWidth; i++)
                {
                    if (i > 0)
                    {
                        if (laneValues[i - 1] != laneValues[i])
                        {
                            updateErrorMessages("Adjacent lane values are not equal");
                            m_inhibitChecking = true;
                            status = false;
                            break;
                        }
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Update the local member indicate we are either in Blanking or Active sections of the trace data
        /// </summary>
        /// <param name="ECName"></param>
        private void updateSectionID(int laneIndex, int laneData, string ECName)
        {
            if ((laneData == 0x1BC) || (laneData == 0x11C) || (laneData == 0x17C)) // BS, SR  or BF ctrl char     
            {
                m_sectionID[laneIndex] = Trace_Section_ID.Blanking;
            }
            if (laneData == 0x1FB)  // BE ctrl char   
            {
                m_sectionID[laneIndex] = Trace_Section_ID.Active;
                Array.Clear(m_inStuffingSection, 0, m_inStuffingSection.Length);
                m_inhibitChecking = false;
            }
        }


        /// <summary>
        /// Process the lane data for Stuffing Sumbols Framing
        /// </summary>
        /// Assumption/Limitations:  The test will not verify a partial FS/FE section 
        ///                          at the very beginning of the captured data (if the
        ///                          FS symbol is not located in the captured data).
        /// <param name="laneName"></param>
        private bool processLane(byte[] stateData, string laneName)
        {
            bool status = true;

            int laneData = m_commonFuncs.GetFieldData(stateData, laneName);
            int laneIndex = m_commonFuncs.GetLaneIndex(laneName);
            string ECName = m_commonFuncs.GetEventCodeName("SST", m_commonFuncs.GetFieldData(stateData, "EventCode"));

            if (laneData > -1)
            {
                m_laneValues[laneIndex] = laneData;            // retain the adjacent lane values.
                updateSectionID(laneIndex, laneData, ECName);  // changes m_sectionID variable with Event Codes of Blanking Start or End          

                if (m_sectionID[laneIndex] == Trace_Section_ID.Active)
                {
                    if (laneData == 0x1FE)              // FS ctrl char
                    {
                        if (m_inStuffingSection[laneIndex] == true)
                        {
                            updateErrorMessages("Missing FE Ctrl Char..");       // we are entering into a stuffing sub-section
                            if (m_inhibitChecking == true)
                                m_inhibitChecking = false;                      // reset checking on encountering FS ctrl char
                        }

                        m_inStuffingSection[laneIndex] = true;
                    }
                    else if (laneData == 0x1F7)         // FE ctrl char
                    {
                        if (m_inStuffingSection[laneIndex] == false)
                        {
                            updateErrorMessages("Missing FS Ctrl Char..");       // we are entering into a stuffing sub-section
                        }

                        m_inStuffingSection[laneIndex] = false;
                    }
                    else
                    {
                        if (m_inStuffingSection[laneIndex])
                        {
                            if (!m_inhibitChecking)
                            {
                                if (laneData != 0x00)                   // verify the lane contains 0x00 values (while in stuffing mode)
                                {
                                    updateErrorMessages("Stuffing State data Not Equal to Zero.");       // we are entering into a stuffing sub-section
                                    m_inhibitChecking = true;
                                }
                            }
                        }
                    }
                }
            }

            return status;
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
                    // coded to minimize the number of checks of link width....
                    // fewer checks should result in faster execution...

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
            subStrings.Add("Number of Active Video Stuffing Segments Processed: " + String.Format("{0:n0}", m_stuffingSegmentCount[0]));

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

            description.Add(" This test verifies that Active Video Stuffing framing begins with");
            description.Add(" an FS control character and ends with an FE control character on all");
            description.Add(" active data lanes.");
            description.Add(" ");
            description.Add(" The test detects state sequences where the FS is encountered but the FE");
            description.Add(" is missing and vice vera.");
            description.Add(" ");
            description.Add(" The test verifyies all stuffed states contain a data value of 0x00.");
            description.Add(" ");
            description.Add(" This test does not check for short data stuffs (one 1 state) in which only the ");
            description.Add(" FS control character is contained in the data stuffing segment.");

            return description;
        }
    }

    #endregion // Public Methods
}
