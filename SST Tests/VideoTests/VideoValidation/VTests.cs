using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DP14ValidationTestsInterface;
using DP14ValidationAttributes_ClassLibrary;

namespace FS4500_ML_VideoValidation
{

    [DP14ValidationAttributes("Video Tests", "SST", 2)]
    public class VTests : IFS4500_VTests
    {
        #region Members

        private VBID_NoVideo_Blanking_Tests VBID_NoVideo_Test = null;               // Test 1
        private MVID_LSByte_Test MVid_LSByte_Test = null;                           // Test 2
        private MVID_ClearedToZeroWhenNoVideo MVID_Cleared_NoVideo_Test = null;     // Test 3
        private ActiveVideoStuffingSymbolFraming ActiveVideoStuffing_Test = null;   // Test 4
        private ActiveVideoTransferUnitLength ActiveTU_Test = null;                 // Test 5
        private FirstPixelDataPlacement PixelFollowsBE_Test = null;                 // Test 6

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
                    VBID_NoVideo_Test = new VBID_NoVideo_Blanking_Tests();
                    VBID_NoVideo_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 2:
                    MVid_LSByte_Test = new MVID_LSByte_Test();
                    MVid_LSByte_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 3:
                    MVID_Cleared_NoVideo_Test = new MVID_ClearedToZeroWhenNoVideo();
                    MVID_Cleared_NoVideo_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 4:
                    ActiveVideoStuffing_Test = new ActiveVideoStuffingSymbolFraming();
                    ActiveVideoStuffing_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 5:
                    ActiveTU_Test = new ActiveVideoTransferUnitLength();
                    ActiveTU_Test.Initialize(ConfigParameters, maxReportedErrors);
                    break;
                case 6:
                    PixelFollowsBE_Test = new FirstPixelDataPlacement();
                    PixelFollowsBE_Test.Initialize(ConfigParameters, maxReportedErrors);
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

            descriptions.Add("Test Number:1 $$ Test Name:1) Blank Line $$ Description: Verifies Blank Lines do not contain any Pixels (DP1.4a 2.1.4)");
            descriptions.Add("Test Number:2 $$ Test Name:2) MVID LSByte Verification $$ Description: Verifies MVID equals MSA's MVID LSByte (DP1.4a 2.2.4).");
            descriptions.Add("Test Number:3 $$ Test Name:3) MVID is Set To Zero When No Video Stream. $$ Description: Veriifes Mvid is cleared to zero when no video stream is being transported (DP1.4a 2.2.1.3).");
            descriptions.Add("Test Number:4 $$ Test Name:4) Active Video Stuffing Symbols Framing. $$ Description: Veriifes Active Video Stuffing Symbols are framed with FS/FE control characters (DP1.4a 2.2.1.4).");
            descriptions.Add("Test Number:5 $$ Test Name:5) Active Video TU Length. $$ Description: Veriifes Active Video Transfer Units lengths are 32-64 Link Symbols per lane (DP1.4a 2.2.1.4).");
            descriptions.Add("Test Number:6 $$ Test Name:6) First Line Pixel Location. $$ Description: Verifies First Pixel of a line is located immediately after BE (DP1.4a 2.2.1.4).");

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
                        errMsg.AddRange(VBID_NoVideo_Test.ProcessState(stateData));
                        break;
                    case 2:
                        errMsg.AddRange(MVid_LSByte_Test.ProcessState(stateData));
                        break;
                    case 3:
                        errMsg.AddRange(MVID_Cleared_NoVideo_Test.ProcessState(stateData));
                        break;
                    case 4:
                        errMsg.AddRange(ActiveVideoStuffing_Test.ProcessState(stateData));
                        break;
                    case 5:
                        errMsg.AddRange(ActiveTU_Test.ProcessState(stateData));
                        break;
                    case 6:
                        errMsg.AddRange(PixelFollowsBE_Test.ProcessState(stateData));
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
                        stats.AddRange(VBID_NoVideo_Test.GetTestResultsSummary());
                        break;
                    case 2:
                        stats.AddRange(MVid_LSByte_Test.GetTestResultsSummary());
                        break;
                    case 3:
                        stats.AddRange(MVID_Cleared_NoVideo_Test.GetTestResultsSummary());
                        break;
                    case 4:
                        stats.AddRange(ActiveVideoStuffing_Test.GetTestResultsSummary());
                        break;
                    case 5:
                        stats.AddRange(ActiveTU_Test.GetTestResultsSummary());
                        break;

                    case 6:
                        stats.AddRange(PixelFollowsBE_Test.GetTestResultsSummary());
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
                        stats.AddRange(VBID_NoVideo_Test.GetTestResultsSummary());
                        break;
                    case 2:
                        stats.AddRange(MVid_LSByte_Test.GetTestResultsSummary());
                        break;
                    case 3:
                        stats.AddRange(MVID_Cleared_NoVideo_Test.GetTestResultsSummary());
                        break;
                    case 4:
                        stats.AddRange(ActiveVideoStuffing_Test.GetTestResultsSummary());
                        break;
                    case 5:
                        stats.AddRange(ActiveTU_Test.GetTestResultsSummary());
                        break;
                    case 6:
                        stats.AddRange(PixelFollowsBE_Test.GetTestResultsSummary());
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
                        description.AddRange(VBID_NoVideo_Test.GetTestDescription());
                        break;
                    case 2:
                        description.AddRange(MVid_LSByte_Test.GetTestDescription());
                        break;
                    case 3:
                        description.AddRange(MVID_Cleared_NoVideo_Test.GetTestDescription());
                        break;
                    case 4:
                        description.AddRange(ActiveVideoStuffing_Test.GetTestDescription());
                        break;
                    case 5:
                        description.AddRange(ActiveTU_Test.GetTestDescription());
                        break;
                    case 6:
                        description.AddRange(PixelFollowsBE_Test.GetTestDescription());
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
