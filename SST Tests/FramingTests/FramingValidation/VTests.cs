using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DP14ValidationTestsInterface;
using DP14ValidationAttributes_ClassLibrary;

namespace FS4500_ML_FramingValidation
{
    [DP14ValidationAttributes("Framing Tests", "SST", 1)]
    public class VTests : IFS4500_VTests
    {

        #region Members

        private BS_Start_Symbols_Test BS_Start_Test = null;                         // Test 1
        private SR_Symbol_Is_Every_512th_BS SR_512th_Test = null;                   // Test 2
        private BS_Followed_By_VBID_Test Verify_VBID_Values_Test = null;            // Test 3
        private BS_Followed_By_VBID_Test Verify_VBID_Grouping = null;               // Test 7
        private VerifySDPs Verify_SDP_Delimitors = null;                            // Test 12
        private MSADelimitors_Test MSA_Delimitors_Test = null;                      // Test 13
        private IssuedOncePerFrame InfoFrame_OncePerFrame = null;                   // Test 14
        private IssuedOncePerFrame MSA_OncePerFrame = null;                         // Test 15

        private VerifyTimeTicks TimeTicks_Test = null;                              // Test 16 -- FPS use only!


        #endregion // Members

        #region Constructor(s)

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testID"></param>
        public VTests()
        {
        }


        #endregion // Constructor(s)

        #region Private Methods
        #endregion // Private Method

        #region Public Method

        /// <summary>
        /// Public method in which the FS4500 Probe Manager will invoke prior to testing.
        /// </summary>
        /// <param name="testTitle"></param>
        public void Initialize(int testID, List<string> ConfigParameters, int maxReportedErrors)
        {

            switch (testID)
            {
                case 1:
                    BS_Start_Test = new BS_Start_Symbols_Test();
                    BS_Start_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 2:
                    SR_512th_Test = new SR_Symbol_Is_Every_512th_BS();
                    SR_512th_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 3:
                    Verify_VBID_Values_Test = new BS_Followed_By_VBID_Test(1);
                    Verify_VBID_Values_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 4:
                    Verify_VBID_Grouping = new BS_Followed_By_VBID_Test(2);
                    Verify_VBID_Grouping.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 5:
                    Verify_SDP_Delimitors = new VerifySDPs();
                    Verify_SDP_Delimitors.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 6:
                    MSA_Delimitors_Test = new MSADelimitors_Test();
                    MSA_Delimitors_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 7:
                    InfoFrame_OncePerFrame = new IssuedOncePerFrame();
                    InfoFrame_OncePerFrame.SDP_ECName = "Info Frame";
                    InfoFrame_OncePerFrame.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 8:
                    MSA_OncePerFrame = new IssuedOncePerFrame();
                    MSA_OncePerFrame.SDP_ECName = "MSA";
                    MSA_OncePerFrame.Initialize(ConfigParameters, maxReportedErrors);
                    break;


                case 100:
                    TimeTicks_Test = new VerifyTimeTicks();
                    TimeTicks_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;

                default:
                    break;
            }

            return;
        }


        /// <summary>
        /// get descriptions
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestDescriptions()
        {
            List<string> descriptions = new List<string>();

            descriptions.Add("Test Number:1 $$ Test Name:1) BS Start Symbol Sequence $$ Description: Verifies All Active Lanes Begin With The Correct BS Symbols (DP1.4a 2.2.1.1, 2.2.1.2).");
            descriptions.Add("Test Number:2 $$ Test Name:2) SR is Every 512th BS $$ Description: Verifies every 512th BS is an SR (DP1.4a 2.2.5.3.7).");
            descriptions.Add("Test Number:3 $$ Test Name:3) BS Followed By VBID $$ Description: Verifies BS is followed on all lanes by VBID, MVID and MAUD (DP1.4a 2.2.1.3).");
            descriptions.Add("Test Number:4 $$ Test Name:4) VB-ID Packet Sequence. $$ Description: Verifies VBID, MVID and MAUD is transported four times, reguardless of link width (DP1.4a 2.2.1.3).");
            descriptions.Add("Test Number:5 $$ Test Name:5) Secondary data shall be de-multiplexed using SS and SE as the separator. $$ Description: Verifies all secondary data packets contain SS and SE. (DP1.4a 2.2.5)");
            descriptions.Add("Test Number:6 $$ Test Name:6) MSA Delimitors $$ Description:  Verifies MSA Packets contain two SS symbols. (DP1.4a 2.2.4)");
            descriptions.Add("Test Number:7 $$ Test Name:7) Info Frame Count $$ Description:  Verifies Info Frame SDP packets occur once per frame. (DP1.4a 2.2.5.3)");
            descriptions.Add("Test Number:8 $$ Test Name:8) MSA Count $$ Description:  Verifies MSA SDP packets occur once per frame. (DP1.4a 2.2.4)");


            descriptions.Add("Test Number:100 $$ Test Name:100) Time Gaps $$ Description: Verifies all time tick values are consecutive.");

            return descriptions;
        }


        /// <summary>
        /// Execute/run a specified test
        /// </summary>
        /// <param name="testNumber"></param>
        public List<string> doTest(int testNumber, byte[] stateData)
        {
            List<string> errMsg = new List<string>();
            try
            {
                switch (testNumber)
                {
                    case 1:
                        errMsg.AddRange(BS_Start_Test.ProcessState(stateData));
                        break;
                    case 2:
                        errMsg.AddRange(SR_512th_Test.ProcessState(stateData));
                        break;
                    case 3:
                        errMsg.AddRange(Verify_VBID_Values_Test.ProcessState(stateData));
                        break;
                    case 4:
                        errMsg.AddRange(Verify_VBID_Grouping.ProcessState(stateData));
                        break;
                    case 5:
                        errMsg.AddRange(Verify_SDP_Delimitors.ProcessState(stateData));
                        break;
                    case 6:
                        errMsg.AddRange(MSA_Delimitors_Test.ProcessState(stateData));
                        break;
                    case 7:
                        errMsg.AddRange(InfoFrame_OncePerFrame.ProcessState(stateData));
                        break;
                    case 8:
                        errMsg.AddRange(MSA_OncePerFrame.ProcessState(stateData));
                        break;

                    case 9:  // test 6b...
                        break;

                    case 100:  // Time Ticks 
                        errMsg.AddRange(TimeTicks_Test.ProcessState(stateData));
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                errMsg.Add("Processing Error: " + ex.Message);
            }

            return errMsg;
        }


        /// <summary>
        /// Returns a list of strings providing the invoking program the ability to display test statistics.
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestResultsSummary(int testNumber)
        {
            List<string> stats = new List<string>();

            try
            {
                switch (testNumber)
                {
                    case 1:
                        stats.AddRange(BS_Start_Test.GetTestResultsSummary());
                        break;
                    case 2:
                        stats.AddRange(SR_512th_Test.GetTestResultsSummary());
                        break;
                    case 3:
                        stats.AddRange(Verify_VBID_Values_Test.GetTestResultsSummary());
                        break;
                    case 4:
                        stats.AddRange(Verify_VBID_Grouping.GetTestResultsSummary());
                        break;
                    case 5:
                        stats.AddRange(Verify_SDP_Delimitors.GetTestResultsSummary());
                        break;
                    case 6:
                        stats.AddRange(MSA_Delimitors_Test.GetTestResultsSummary());
                        break;
                    case 7:
                        stats.AddRange(InfoFrame_OncePerFrame.GetTestResultsSummary());
                        break;
                    case 8:
                        stats.AddRange(MSA_OncePerFrame.GetTestResultsSummary());
                        break;

                    case 9:  // test 6b...
                        break;

                    case 100:  // Time Ticks
                        stats.AddRange(TimeTicks_Test.GetTestResultsSummary());
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                stats.Add("Processing Error: " + ex.Message);
            }

            return stats;
        }


        /// <summary>
        /// Returns a list of strings providing the invoking program the ability to display test statistics.
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestResultsDetailed(int testNumber)
        {
            List<string> stats = null;

            List<string> errMsg = new List<string>();
            try
            {
                switch (testNumber)
                {
                    case 1:
                        stats.AddRange(BS_Start_Test.GetTestResultsSummary());
                        break;
                    case 2:
                        stats.AddRange(SR_512th_Test.GetTestResultsSummary());
                        break;
                    case 3:
                        stats.AddRange(Verify_VBID_Values_Test.GetTestResultsSummary());
                        break;
                    case 4:
                        stats.AddRange(Verify_VBID_Grouping.GetTestResultsSummary());
                        break;
                    case 5:
                        errMsg.AddRange(Verify_SDP_Delimitors.GetTestResultsSummary());
                        break;
                    case 6:
                        stats.AddRange(MSA_Delimitors_Test.GetTestResultsSummary());
                        break;
                    case 7:
                        stats.AddRange(InfoFrame_OncePerFrame.GetTestResultsSummary());
                        break;
                    case 8:
                        stats.AddRange(MSA_OncePerFrame.GetTestResultsSummary());
                        break;

                    case 9:  // test 6b...
                        break;

                    case 100:  // Time Ticks
                        stats.AddRange(TimeTicks_Test.GetTestResultsSummary());
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                errMsg.Add("Processing Error: " + ex.Message);
            }

            return stats;
        }


        /// <summary>
        /// Returns a list of strings providing the invoking program the ability to display test statistics.
        /// </summary>
        /// <returns></returns>
        public List<string> GetTestOverview(int testNumber)
        {
            List<string> description = new List<string>();

            List<string> errMsg = new List<string>();
            try
            {
                switch (testNumber)
                {
                    case 1:
                        description.AddRange(BS_Start_Test.GetTestDescription());
                        break;
                    case 2:
                        description.AddRange(SR_512th_Test.GetTestDescription());
                        break;
                    case 3:
                        description.AddRange(Verify_VBID_Values_Test.GetTestDescription());
                        break;
                    case 4:
                        description.AddRange(Verify_VBID_Grouping.GetTestDescription());
                        break;
                    case 5:
                        description.AddRange(Verify_SDP_Delimitors.GetTestDescription());
                        break;
                    case 6:
                        description.AddRange(MSA_Delimitors_Test.GetTestDescription());
                        break;
                    case 7:
                        description.AddRange(InfoFrame_OncePerFrame.GetTestDescription());
                        break;
                    case 8:
                        description.AddRange(MSA_OncePerFrame.GetTestDescription());
                        break;

                    case 100:  // timeTicks...
                        description.AddRange(TimeTicks_Test.GetTestDescription());
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                errMsg.Add("Processing Error: " + ex.Message);
            }

            return description;
        } 
        #endregion // Public Methods
    }
}
