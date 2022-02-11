using System;
using System.Collections.Generic;

using System.IO;
using System.Xml;

namespace FS4500_MainLink_MST
{
    internal class UtilityFunctions
    {
        #region Members
        private int BYTES_PER_STATE { get; set; } = 16;

        #endregion //Members

        #region Constructor(s)
        #endregion // Constructor(s)

        #region Private Methods

        /// <summary>
        /// Generate a list of event code names.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private List<EventCodeID> getECNameIDPairs(XmlReader reader)
        {
            List<EventCodeID> ecIDs = new List<EventCodeID>();
            bool done = false;

            while (reader.Read() && !done)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "EventCode")
                        {
                            ecIDs.Add(new EventCodeID(reader.GetAttribute("Name"), int.Parse(reader.GetAttribute("Value"), System.Globalization.NumberStyles.HexNumber)));
                        }
                        break;
                    case XmlNodeType.Text:
                        break;
                    case XmlNodeType.CDATA:
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        break;
                    case XmlNodeType.Document:
                        break;
                    case XmlNodeType.DocumentType:
                        break;
                    case XmlNodeType.EntityReference:
                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == "ProtocolID")
                        {
                            done = true;  // exit the for loop....
                        }
                        break;
                }
            }

            return ecIDs;
        }


        /// <summary>
        /// Generate a list of event code names.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private List<string> getECNames(XmlReader reader)
        {
            List<string> ecNames = new List<string>();
            bool done = false;

            while (reader.Read() && !done)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "EventCode")
                        {
                            ecNames.Add(reader.GetAttribute("Name"));
                        }
                        break;
                    case XmlNodeType.Text:
                        break;
                    case XmlNodeType.CDATA:
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        break;
                    case XmlNodeType.Document:
                        break;
                    case XmlNodeType.DocumentType:
                        break;
                    case XmlNodeType.EntityReference:
                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == "ProtocolID")
                        {
                            done = true;  // exit the for loop....
                        }
                        break;
                }
            }

            return ecNames;
        }


        /// <summary>
        /// Generate a list containing meta data of the sub-fields contained in the 16 byte state data.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private List<StateDataFieldInfo> getStateDataFieldMetaData(XmlReader reader)
        {
            List<StateDataFieldInfo> fldInfo = new List<StateDataFieldInfo>();
            bool done = false;

            while (reader.Read() && !done)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "StateField")
                        {
                            fldInfo.Add(new StateDataFieldInfo(reader.GetAttribute("Name"),
                                                                int.Parse(reader.GetAttribute("Offset")),
                                                                int.Parse(reader.GetAttribute("Width"))));
                        }
                        break;
                    case XmlNodeType.Text:
                        break;
                    case XmlNodeType.CDATA:
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        break;
                    case XmlNodeType.Document:
                        break;
                    case XmlNodeType.DocumentType:
                        break;
                    case XmlNodeType.EntityReference:
                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == "ProtocolID")
                        {
                            done = true;  // exit the for loop....
                        }
                        break;
                }
            }

            return fldInfo;
        }


        /// <summary>
        /// Get the field location in terms of byteID and the MSBit of the field within the byte. 
        /// </summary>
        /// <param name="filterBitID"></param>
        /// <param name="byteID"></param>
        /// <param name="bitID"></param>
        /// <returns></returns>
        /// Assumptions 
        ///                                                                            byte[0]                byte[1]  ...
        ///     OffSet is the bitID starting from the left side and counting up... 7,6,5,4,3,2,1,0  ||  7,6,5,4,3,12,1,0  || ...
        ///                                                                        | | |                | | |
        ///                                                                        | | |                | | |
        ///                                                                        | | offset:2         | | offset:10
        ///                                                                        | offset:1           | offset:9
        ///                                                                        offset:0             offset:8
        private bool getFieldLocation(int offset, ref int byteID, ref int bitID)
        {
            bool status = true;
            bitID = 0;

            // this value represents the byte in linear order from the beginning of the array (starting with byte 0, 1, etc)
            float byteBitIDs = ((float)(offset) / (float)8);

            // this value represents the byte in the array i.e. byte[0]  byte[1]  byte[2]...
            byteID = (int)byteBitIDs;

            // strip off the integer and keep the remainder 
            float bitValue = (float)byteBitIDs - (int)byteBitIDs;

            // identify the bit assoicated with the remainder
            if (bitValue == 0)
                bitID = 7;
            else if (bitValue == 0.125)
                bitID = 6;
            else if (bitValue == 0.25)
                bitID = 5;
            else if (bitValue == 0.375)
                bitID = 4;
            else if (bitValue == 0.50)
                bitID = 3;
            else if (bitValue == 0.625)
                bitID = 2;
            else if (bitValue == 0.75)
                bitID = 1;
            else if (bitValue == 0.875)
                bitID = 0;
            else
            {
                status = false;
                bitID = -1;
            }

            return status;
        }


        /// <summary>
        /// Extract the number of specified consecutive bits from a given byte array
        /// </summary>
        /// <param name="byteID"></param>
        /// <param name="bitID"></param>
        /// <returns></returns>
        private uint getFieldValue(int byteID, int bitID, int width, byte[] data)
        {
            uint fldValue = 0x00;
            int bitCount = 0;

            // How this works:
            //  'For' loop accumulates the field in chunks of bytes..
            //   The first byte being extracted lops off the MSBits not contained in the field
            //   Thus the bits used in the MSBytes begin with the specified field byte # and Bit #
            //   the loop gets whole bytes for all subsequent bytes being extracted from the data
            //   Once enough bits are accumulated, the loop is exited.  The field value is shifted
            //   to the rights for the number of 'Extra' bits of extracted data.

            // assemble the bytes of data containing the field.
            for (int i = byteID; (width - bitCount) > 0; i++)
            {
                if (i == byteID)
                {
                    // lop off the leading bits...
                    fldValue = data[i] & (uint)(Math.Pow(2, bitID + 1) - 1);  // <<== this may be a speed issue!
                    bitCount += bitID + 1;
                }
                else
                {
                    fldValue = (fldValue << 8) | data[i];
                    bitCount += 8;
                }
            }

            // lop off the trailing bits that are not part of the field.
            if (bitCount > width)
                fldValue = fldValue >> (bitCount - width);

            return fldValue;
        }

        #endregion // Private Methods

        #region Public Methods



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// https://docs.microsoft.com/en-us/dotnet/api/system.xml.xmlreader.read?view=netframework-4.7.2
        public List<string> GetEventCodeNames(string pID)
        {
            List<string> ecNames = new List<string>();
            bool inSection = false;

            // NOTE:  The XML file is a project embedded resource that is compiled into the executable...
            string myString = FS4500_MainLink_MST.Properties.Resources.StateDataInfo;

            XmlReader reader = XmlReader.Create(new StringReader(myString));
            reader.MoveToContent();

            // Parse the file and display each of the nodes.
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "ProtocolID")
                        {
                            if (pID == reader.GetAttribute("ID"))
                            {
                                inSection = true;
                            }
                            else
                            {
                                inSection = false;
                            }
                        }

                        else if ((reader.Name == "EventCode") && (inSection == true))
                        {
                            ecNames = getECNames(reader);
                        }
                        break;
                    case XmlNodeType.Text:
                        break;
                    case XmlNodeType.CDATA:
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        break;
                    case XmlNodeType.Document:
                        break;
                    case XmlNodeType.DocumentType:
                        break;
                    case XmlNodeType.EntityReference:
                        break;
                    case XmlNodeType.EndElement:
                        break;
                }
            }

            return ecNames;
        }

        /// <summary>
        /// Get a list of event code Name/NumberID pairs for the specified protocol
        /// </summary>
        /// <returns></returns>
        public List<EventCodeID> GetEventCodeIDs(string pID)
        {
            List<EventCodeID> ecPairs = new List<EventCodeID>();
            bool inSection = false;

            // NOTE:  The XML file is a project embedded resource that is compiled into the executable...
            string myString = FS4500_MainLink_MST.Properties.Resources.StateDataInfo;

            XmlReader reader = XmlReader.Create(new StringReader(myString));
            reader.MoveToContent();

            // Parse the file and display each of the nodes.
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "ProtocolID")
                        {
                            if (pID == reader.GetAttribute("ID"))
                            {
                                inSection = true;
                            }
                            else
                            {
                                inSection = false;
                            }
                        }
                        else if ((reader.Name == "EventCode") && (inSection == true))
                        {
                            ecPairs = getECNameIDPairs(reader);
                        }
                        break;
                    case XmlNodeType.Text:
                        break;
                    case XmlNodeType.CDATA:
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        break;
                    case XmlNodeType.Document:
                        break;
                    case XmlNodeType.DocumentType:
                        break;
                    case XmlNodeType.EntityReference:
                        break;
                    case XmlNodeType.EndElement:
                        break;
                }
            }

            return ecPairs;
        }


        /// <summary>
        /// Get a specific event code numeric value from the list of event code Name / numeric value list.
        /// </summary>
        /// <param name="eventCodeIDs"></param>
        /// <param name="ecName"></param>
        /// <returns></returns>
        public int GetECValue(List<EventCodeID> eventCodeIDs, string ecName)
        {
            int ecCode = -1;

            foreach (EventCodeID id in eventCodeIDs)
            {
                if (ecName == id.Name)
                {
                    ecCode = id.NumericValue;
                    break; // exit the foreach loop
                }
            }

            return ecCode;
        }



        /// <summary>
        /// Generate a list of state data (sub)fields meta data
        /// </summary>
        /// <returns></returns>
        public List<StateDataFieldInfo> GetStateDataFieldsInfo()
        {
            List<StateDataFieldInfo> fldMetaDataList = new List<StateDataFieldInfo>();

            // NOTE:  The XML file is a project embedded resource that is compiled into the executable...
            string myString = FS4500_MainLink_MST.Properties.Resources.StateDataInfo;

            XmlReader reader = XmlReader.Create(new StringReader(myString));
            reader.MoveToContent();

            // Parse the file and display each of the nodes.
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        if (reader.Name == "StateDataFields")
                        {
                            fldMetaDataList = getStateDataFieldMetaData(reader);
                        }
                        break;
                    case XmlNodeType.Text:
                        break;
                    case XmlNodeType.CDATA:
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        break;
                    case XmlNodeType.Document:
                        break;
                    case XmlNodeType.DocumentType:
                        break;
                    case XmlNodeType.EntityReference:
                        break;
                    case XmlNodeType.EndElement:
                        break;
                }
            }

            return fldMetaDataList;
        }


        /// <summary>
        /// Get the sub-field numeric value from the current state.
        /// </summary>
        /// <param name="fieldsList"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public int GetStateDataFieldValue(List<StateDataFieldInfo> fieldsList, string fieldName, byte[] stateData)
        {
            int fldValue = -1;

            foreach (StateDataFieldInfo fieldInfo in fieldsList)
            {
                if (fieldName == fieldInfo.Name)
                {
                    bool status = true;
                    int byteID = -1;
                    int bitID = -1;

                    status = getFieldLocation(fieldInfo.Offset, ref byteID, ref bitID);
                    if (status)
                    {
                        fldValue = (int)getFieldValue(byteID, bitID, fieldInfo.Width, stateData);
                    }
                    break;  // exit the 'foreach' loop
                }
            }

            return fldValue;
        }


        /// <summary>
        /// extract on of 4 10 bit values.
        /// </summary>
        /// <param name="laneID"></param>
        /// <param name="stateData"></param>
        /// <returns></returns>
        //private bool getDataLane(int stateIndex, int laneID, byte[] stateData, ref int Ln_INV, ref int Ln_K, ref byte Ln_Data)
        public bool GetDataLane(int laneID, byte[] stateData, ref int Ln_INV, ref int Ln_K, ref byte Ln_Data)
        {
            bool status = true;

            if (stateData.Length == BYTES_PER_STATE)
            {
                switch (laneID)
                {
                    case 0:
                        Ln_INV = (stateData[11] & 0x80) >> 7;
                        Ln_K = (stateData[11] & 0x40) >> 6;
                        Ln_Data = (byte)(((stateData[11] & 0x3F) << 2) | ((stateData[12] & 0xC0) >> 6));
                        break;

                    case 1:
                        Ln_INV = (stateData[12] & 0x20) >> 5;
                        Ln_K = (stateData[12] & 0x10) >> 4;
                        Ln_Data = (byte)(((stateData[12] & 0x0F) << 4) | ((stateData[13] & 0xF0) >> 4));
                        break;

                    case 2:
                        Ln_INV = (stateData[13] & 0x08) >> 3;
                        Ln_K = (stateData[13] & 0x04) >> 2;
                        Ln_Data = (byte)(((stateData[13] & 0x03) << 6) | ((stateData[14] & 0xFC) >> 2));
                        break;

                    case 3:
                        Ln_INV = (stateData[14] & 0x02) >> 1;
                        Ln_K = (stateData[14] & 0x01);
                        Ln_Data = (byte)(stateData[15] & 0xFF);
                        break;

                    default:
                        status = false;
                        break;
                }
            }
            else
            {
                throw new Exception("Invalid GetDataLane() input data array length!");
            }

            return status;
        }


        #endregion // Public Methods
    }
}
