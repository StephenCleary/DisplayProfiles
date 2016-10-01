/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace MonitorSwitcherGUI
{
    public class MonitorSwitcher
    {

        public static Boolean LoadDisplaySettings(String fileName)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine("Failed to load display settings because file does not exist: " + fileName);

                return false;
            }

            // Objects for DeSerialization of pathInfo and modeInfo classes
            System.Xml.Serialization.XmlSerializer readerPath = new System.Xml.Serialization.XmlSerializer(typeof(CCDWrapper.DisplayConfigPathInfo));
            System.Xml.Serialization.XmlSerializer readerModeTarget = new System.Xml.Serialization.XmlSerializer(typeof(CCDWrapper.DisplayConfigTargetMode));
            System.Xml.Serialization.XmlSerializer readerModeSource = new System.Xml.Serialization.XmlSerializer(typeof(CCDWrapper.DisplayConfigSourceMode));
            System.Xml.Serialization.XmlSerializer readerModeInfoType = new System.Xml.Serialization.XmlSerializer(typeof(CCDWrapper.DisplayConfigModeInfoType));
            System.Xml.Serialization.XmlSerializer readerModeAdapterID = new System.Xml.Serialization.XmlSerializer(typeof(long));

            // Lists for storing the results
            List<CCDWrapper.DisplayConfigPathInfo> pathInfoList = new List<CCDWrapper.DisplayConfigPathInfo>();
            List<CCDWrapper.DisplayConfigModeInfo> modeInfoList = new List<CCDWrapper.DisplayConfigModeInfo>();

            // Loading the xml file
            XmlReader xml = XmlReader.Create(fileName);
            xml.Read();
            while (true)
            {                
                if ((xml.Name.CompareTo("DisplayConfigPathInfo") == 0) && (xml.IsStartElement()))
                {                    
                    CCDWrapper.DisplayConfigPathInfo pathInfo = (CCDWrapper.DisplayConfigPathInfo)readerPath.Deserialize(xml);
                    pathInfoList.Add(pathInfo);
                    continue;
                }
                else if ((xml.Name.CompareTo("modeInfo") == 0) && (xml.IsStartElement()))
                {
                    CCDWrapper.DisplayConfigModeInfo modeInfo = new CCDWrapper.DisplayConfigModeInfo();
                    xml.Read();
                    xml.Read();
                    modeInfo.id = Convert.ToUInt32(xml.Value);
                    xml.Read();
                    xml.Read();
                    modeInfo.adapterId = (long)readerModeAdapterID.Deserialize(xml);
                    modeInfo.infoType = (CCDWrapper.DisplayConfigModeInfoType)readerModeInfoType.Deserialize(xml);
                    if (modeInfo.infoType == CCDWrapper.DisplayConfigModeInfoType.Target)
                    {
                        modeInfo.targetMode = (CCDWrapper.DisplayConfigTargetMode)readerModeTarget.Deserialize(xml);
                    }
                    else
                    {
                        modeInfo.sourceMode = (CCDWrapper.DisplayConfigSourceMode)readerModeSource.Deserialize(xml);
                    }

                    modeInfoList.Add(modeInfo);
                    continue;
                }

                if (!xml.Read())
                {
                    break;
                }
            }
            xml.Close();
                      
            // Convert C# lists to simply array
            var pathInfoArray = new CCDWrapper.DisplayConfigPathInfo[pathInfoList.Count];
            for (int iPathInfo = 0; iPathInfo < pathInfoList.Count; iPathInfo++)
            {
                pathInfoArray[iPathInfo] = pathInfoList[iPathInfo];
            }

            var modeInfoArray = new CCDWrapper.DisplayConfigModeInfo[modeInfoList.Count];
            for (int iModeInfo = 0; iModeInfo < modeInfoList.Count; iModeInfo++)
            {
                modeInfoArray[iModeInfo] = modeInfoList[iModeInfo];
            }

            // Get current display settings
            CCDWrapper.DisplayConfigPathInfo[] pathInfoArrayCurrent = new CCDWrapper.DisplayConfigPathInfo[0];
            CCDWrapper.DisplayConfigModeInfo[] modeInfoArrayCurrent = new CCDWrapper.DisplayConfigModeInfo[0];

            Boolean statusCurrent = GetDisplaySettings(ref pathInfoArrayCurrent, ref modeInfoArrayCurrent, false);
            if (statusCurrent)
            {
                // For some reason the adapterID parameter changes upon system restart, all other parameters however, especially the ID remain constant.
                // We check the loaded settings against the current settings replacing the adapaterID with the other parameters
                for (int iPathInfo = 0; iPathInfo < pathInfoArray.Length; iPathInfo++)
                {
                    for (int iPathInfoCurrent = 0; iPathInfoCurrent < pathInfoArrayCurrent.Length; iPathInfoCurrent++)
                    {
                        if ((pathInfoArray[iPathInfo].sourceInfo.id == pathInfoArrayCurrent[iPathInfoCurrent].sourceInfo.id) &&
                            (pathInfoArray[iPathInfo].targetInfo.id == pathInfoArrayCurrent[iPathInfoCurrent].targetInfo.id))
                        {
                            pathInfoArray[iPathInfo].sourceInfo.adapterId = pathInfoArrayCurrent[iPathInfoCurrent].sourceInfo.adapterId;
                            pathInfoArray[iPathInfo].targetInfo.adapterId = pathInfoArrayCurrent[iPathInfoCurrent].targetInfo.adapterId;
                            break;
                        }
                    }
                }
                
                // Same again for modeInfo, however we get the required adapterId information from the pathInfoArray
                for (int iModeInfo = 0; iModeInfo < modeInfoArray.Length; iModeInfo++)
                {
                    for (int iPathInfo = 0; iPathInfo < pathInfoArray.Length; iPathInfo++)
                    {
                        if ((modeInfoArray[iModeInfo].id == pathInfoArray[iPathInfo].targetInfo.id) &&
                            (modeInfoArray[iModeInfo].infoType == CCDWrapper.DisplayConfigModeInfoType.Target))
                        {
                            // We found target adapter id, now lets look for the source modeInfo and adapterID
                            for (int iModeInfoSource = 0; iModeInfoSource < modeInfoArray.Length; iModeInfoSource++)
                            {
                                if ((modeInfoArray[iModeInfoSource].id == pathInfoArray[iPathInfo].sourceInfo.id) &&
                                    (modeInfoArray[iModeInfoSource].adapterId == modeInfoArray[iModeInfo].adapterId) &&
                                    (modeInfoArray[iModeInfoSource].infoType == CCDWrapper.DisplayConfigModeInfoType.Source))
                                {
                                    modeInfoArray[iModeInfoSource].adapterId = pathInfoArray[iPathInfo].sourceInfo.adapterId;
                                    break;
                                }
                            }
                            modeInfoArray[iModeInfo].adapterId = pathInfoArray[iPathInfo].targetInfo.adapterId;
                            break;
                        }                       
                    }                    
                }

                // Set loaded display settings
                uint numPathArrayElements = (uint)pathInfoArray.Length;
                uint numModeInfoArrayElements = (uint)modeInfoArray.Length;
                long status = CCDWrapper.SetDisplayConfig(numPathArrayElements, pathInfoArray, numModeInfoArrayElements, modeInfoArray,
                                                          CCDWrapper.SdcFlags.Apply | CCDWrapper.SdcFlags.UseSuppliedDisplayConfig | CCDWrapper.SdcFlags.SaveToDatabase | CCDWrapper.SdcFlags.AllowChanges);
                if (status != 0)
                {
                    Console.WriteLine("Failed to set display settings, ERROR: " + status.ToString());

                    return false;
                }
                
                return true;
            }

            return false;
        }

        public static Boolean GetDisplaySettings(ref CCDWrapper.DisplayConfigPathInfo[] pathInfoArray, ref CCDWrapper.DisplayConfigModeInfo[] modeInfoArray, Boolean ActiveOnly)
        {
            uint numPathArrayElements;
            uint numModeInfoArrayElements;

            // query active paths from the current computer.
            CCDWrapper.QueryDisplayFlags queryFlags = CCDWrapper.QueryDisplayFlags.AllPaths;
            if (ActiveOnly)
            {
                queryFlags = CCDWrapper.QueryDisplayFlags.OnlyActivePaths;
            }

            var status = CCDWrapper.GetDisplayConfigBufferSizes(queryFlags, out numPathArrayElements, out numModeInfoArrayElements);
            if (status == 0)
            {
                pathInfoArray = new CCDWrapper.DisplayConfigPathInfo[numPathArrayElements];
                modeInfoArray = new CCDWrapper.DisplayConfigModeInfo[numModeInfoArrayElements];

                status = CCDWrapper.QueryDisplayConfig(queryFlags,
                                                       ref numPathArrayElements, pathInfoArray, ref numModeInfoArrayElements,
                                                       modeInfoArray, IntPtr.Zero);
                if (status == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static Boolean SaveDisplaySettings(String fileName)
        {
            CCDWrapper.DisplayConfigPathInfo[] pathInfoArray = new CCDWrapper.DisplayConfigPathInfo[0];
            CCDWrapper.DisplayConfigModeInfo[] modeInfoArray = new CCDWrapper.DisplayConfigModeInfo[0]; 

            Boolean status = GetDisplaySettings(ref pathInfoArray, ref modeInfoArray, true);
            if (status) 
            {
                System.Xml.Serialization.XmlSerializer writerPath = new System.Xml.Serialization.XmlSerializer(typeof(CCDWrapper.DisplayConfigPathInfo));
                System.Xml.Serialization.XmlSerializer writerModeTarget = new System.Xml.Serialization.XmlSerializer(typeof(CCDWrapper.DisplayConfigTargetMode));
                System.Xml.Serialization.XmlSerializer writerModeSource = new System.Xml.Serialization.XmlSerializer(typeof(CCDWrapper.DisplayConfigSourceMode));
                System.Xml.Serialization.XmlSerializer writerModeInfoType = new System.Xml.Serialization.XmlSerializer(typeof(CCDWrapper.DisplayConfigModeInfoType));
                System.Xml.Serialization.XmlSerializer writerModeAdapterID = new System.Xml.Serialization.XmlSerializer(typeof(long));
                XmlWriter xml = XmlWriter.Create(fileName);

                xml.WriteStartDocument();
                xml.WriteStartElement("displaySettings");
                xml.WriteStartElement("pathInfoArray");
                foreach (CCDWrapper.DisplayConfigPathInfo pathInfo in pathInfoArray)
                {
                    writerPath.Serialize(xml, pathInfo);
                }
                xml.WriteEndElement();

                xml.WriteStartElement("modeInfoArray");
                for (int iModeInfo = 0; iModeInfo < modeInfoArray.Length; iModeInfo++)
                {
                    xml.WriteStartElement("modeInfo");
                    CCDWrapper.DisplayConfigModeInfo modeInfo = modeInfoArray[iModeInfo];
                    xml.WriteElementString("id", modeInfo.id.ToString());
                    writerModeAdapterID.Serialize(xml, modeInfo.adapterId);
                    writerModeInfoType.Serialize(xml, modeInfo.infoType);
                    if (modeInfo.infoType == CCDWrapper.DisplayConfigModeInfoType.Target)
                    {
                        writerModeTarget.Serialize(xml, modeInfo.targetMode);
                    }
                    else
                    {
                        writerModeSource.Serialize(xml, modeInfo.sourceMode);
                    }
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndDocument();
                xml.Flush();
                xml.Close();

                return true;
            }
            else
            {                    
                Console.WriteLine("Failed to get display settings, ERROR: " + status.ToString());
            }                

            return false;
        }

        static void Main(string[] args)
        {
            Boolean validCommand = false;
            foreach (string iArg in args)
            {
                string[] argElements = iArg.Split(new char[] { ':' }, 2);

                switch (argElements[0].ToLower())
                {
                    case "-save":
                        SaveDisplaySettings(argElements[1]);
                        validCommand = true;
                        break;
                    case "-load":
                        LoadDisplaySettings(argElements[1]);
                        validCommand = true;
                        break;
                }
            }

            if (!validCommand)
            {
                Console.WriteLine("Monitor Profile Switcher command line utlility:\n");
                Console.WriteLine("Paremeters to MonitorSwitcher.exe:");
                Console.WriteLine("\t -save:{xmlfile} \t save the current monitor configuration to file");
                Console.WriteLine("\t -load:{xmlfile} \t load and apply monitor configuration from file");
                Console.WriteLine("");
                Console.WriteLine("Examples:");
                Console.WriteLine("\tMonitorSwitcher.exe -save:MyProfile.xml");
                Console.WriteLine("\tMonitorSwitcher.exe -load:MyProfile.xml");
                Console.ReadKey();
            }
        }
    }
}
