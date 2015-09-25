using System;
using System.Web;
using System.Xml;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Search;
using NBrightCore.common;
using NBrightDNN;
using NBrightMod.common;

namespace Nevoweb.DNN.NBrightMod.Components
{

    public class NBrightModController : IPortable
    {

        #region Optional Interfaces

        #region IPortable Members

        /// -----------------------------------------------------------------------------
        /// <summary>
        ///   ExportModule implements the IPortable ExportModule Interface
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name = "moduleId">The Id of the module to be exported</param>
        /// <history>
        /// </history>
        /// -----------------------------------------------------------------------------
        public string ExportModule(int ModuleId)
        {
            var objModCtrl = new ModuleController();
            var xmlOut = "<root>";

            var objModInfo = objModCtrl.GetModule(ModuleId, Null.NullInteger, true);

            if (objModInfo != null)
            {
                var portalId = objModInfo.PortalID;
                var objTabCtrl = new TabController();
                var objCtrl = new NBrightDataController();
                var l = objCtrl.GetList(portalId, ModuleId, "NBrightModDATA");
                foreach (var nbi in l)
                {
                    if (Utils.IsNumeric(nbi.GUIDKey))
                    {
                        var tabInfo = objTabCtrl.GetTab(Convert.ToInt32(nbi.GUIDKey), portalId, true);
                        if (tabInfo != null)
                        {
                            xmlOut += nbi.ToXmlItem();
                        }
                    }
                }
                var l2 = objCtrl.GetList(portalId, ModuleId, "NBrightModDATALANG");
                foreach (var nbi in l2)
                {
                    if (Utils.IsNumeric(nbi.GUIDKey))
                    {
                        var tabInfo = objTabCtrl.GetTab(Convert.ToInt32(nbi.GUIDKey), portalId, true);
                        if (tabInfo != null)
                        {
                            xmlOut += nbi.ToXmlItem();
                        }
                    }
                }

                var settingsInfo = LocalUtils.GetSettings(ModuleId.ToString());
                xmlOut += settingsInfo.ToXmlItem();

            }
            xmlOut += "</root>";

            return xmlOut;
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        ///   ImportModule implements the IPortable ImportModule Interface
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name = "moduleId">The ID of the Module being imported</param>
        /// <param name = "content">The Content being imported</param>
        /// <param name = "version">The Version of the Module Content being imported</param>
        /// <param name = "userId">The UserID of the User importing the Content</param>
        /// <history>
        /// </history>
        /// -----------------------------------------------------------------------------

        public void ImportModule(int moduleId, string content, string version, int userId)
        {
            var xmlDoc = new XmlDocument();
            var objModCtrl = new ModuleController();
            var objCtrl = new NBrightDataController();
            var objModInfo = objModCtrl.GetModule(moduleId, Null.NullInteger, true);
            if (objModInfo != null)
            {
                // import All records
                xmlDoc.LoadXml(content);

                var xmlNodList = xmlDoc.SelectNodes("root/item");
                if (xmlNodList != null)
                {
                    foreach (XmlNode xmlNod1 in xmlNodList)
                    {
                        var nbi = new NBrightInfo();
                        nbi.FromXmlItem(xmlNod1.OuterXml);
                        nbi.PortalId = objModInfo.PortalID;
                        nbi.ModuleId = moduleId;
                        objCtrl.Update(nbi);
                    }
                }

            }

        }

        #endregion


        #region ISearchable Members

        /// -----------------------------------------------------------------------------
        /// <summary>
        ///   GetSearchItems implements the ISearchable Interface
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name = "modInfo">The ModuleInfo for the module to be Indexed</param>
        /// <history>
        /// </history>
        /// -----------------------------------------------------------------------------
        public SearchItemInfoCollection GetSearchItems(ModuleInfo modInfo)
        {
            var searchItemCollection = new SearchItemInfoCollection();
            var settingsInfo = LocalUtils.GetSettings(modInfo.ModuleID.ToString());
            if (settingsInfo != null)
            {
                var stopIndex = settingsInfo.GetXmlProperty("genxml/checkbox/stopdnnindex");
                if (stopIndex.ToLower() != "true")
                {
                    // Get all the non-langauge data records.
                    var objCtrl = new NBrightDataController();

                    var culturecodeList = DnnUtils.GetCultureCodeList(modInfo.PortalID);
                    foreach (var lang in culturecodeList)
                    {
                        var lstData = objCtrl.GetList(modInfo.PortalID, modInfo.ModuleID, "NBrightModDATA"); // reset search list objects to non-langauge ones.
                        string strContent = "";
                        var modifiedDate = DateTime.Now;
                        var searchTitle = modInfo.ParentTab.TabName;

                        // add lanaguge records to list to be indexed
                        var lstDataLang = objCtrl.GetList(modInfo.PortalID, modInfo.ModuleID, "NBrightModDATALANG");
                        foreach (var obj in lstDataLang)
                        {
                            lstData.Add(obj);
                        }

                        foreach (var objContent in lstData)
                        {
                            //content is encoded in the Database so Decode before Indexing
                            if (objContent.XMLDoc != null)
                            {
                                var xmlNods = objContent.XMLDoc.SelectNodes("genxml/edt/*");
                                if (xmlNods != null)
                                {
                                    foreach (XmlNode xmlNod in xmlNods)
                                    {
                                        strContent += HttpUtility.HtmlDecode(xmlNod.InnerText) + " ";
                                    }
                                }
                                xmlNods = objContent.XMLDoc.SelectNodes("genxml/textbox/*");
                                if (xmlNods != null)
                                {
                                    foreach (XmlNode xmlNod in xmlNods)
                                    {
                                        strContent += xmlNod.InnerText + " ";
                                    }
                                }
                                var xNod = objContent.XMLDoc.SelectSingleNode("genxml/textbox/searchtitle");
                                if (xNod == null || xNod.InnerText.Trim() == "")
                                {
                                    xNod = objContent.XMLDoc.SelectSingleNode("genxml/textbox/title");
                                }
                                if (xNod != null && xNod.InnerText.Trim() != "") searchTitle = xNod.InnerText;
                                if (objContent.ModifiedDate < modifiedDate) modifiedDate = objContent.ModifiedDate;
                            }
                        }

                        if (strContent != "")
                        {
                            //Get the description string
                            var searchDescriptionLength = "100";
                            string strDescription = HtmlUtils.Shorten(HtmlUtils.Clean(strContent, false), Convert.ToInt32(searchDescriptionLength), "...");

                            var searchItem = new SearchItemInfo(searchTitle,
                                                                strDescription,
                                                                -1,
                                                                modifiedDate,
                                                                modInfo.ModuleID,
                                                                lang,
                                                                strContent,
                                                                "",
                                                                Null.NullInteger);
                            searchItemCollection.Add(searchItem);
                        }
                    }

                }
            }

            return searchItemCollection;

        }

        #endregion


        #endregion

    }

}
