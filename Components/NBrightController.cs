﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Resources;
using System.Web;
using System.Xml;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content.Taxonomy;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Search;
using DotNetNuke.Services.Search.Entities;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using NBrightMod.common;

namespace Nevoweb.DNN.NBrightMod.Components
{

    public class NBrightModController : ModuleSearchBase, IPortable
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
                var objCtrl = new NBrightDataController();
                var l = objCtrl.GetList(portalId, ModuleId, "NBrightModDATA");
                foreach (var nbi in l)
                {
                    nbi.GUIDKey = nbi.ItemID.ToString(""); // set the GUIDKey to the current itemid, so we can relink lang record on import.
                    xmlOut += nbi.ToXmlItem();
                }
                var l2 = objCtrl.GetList(portalId, ModuleId, "NBrightModDATALANG");
                foreach (var nbi in l2)
                {
                            xmlOut += nbi.ToXmlItem();
                }

                var settingsInfo = LocalUtils.GetSettings(ModuleId.ToString());
                xmlOut += settingsInfo.ToXmlItem();

                var themefolder = settingsInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
                // get portal level AppTheme templates
                var rtnfile = LocalUtils.ExportPortalTheme(themefolder);
                if (rtnfile != "" || File.Exists(rtnfile))
                {
                    System.IO.FileStream inFile = new System.IO.FileStream(rtnfile,System.IO.FileMode.Open,System.IO.FileAccess.Read);
                    byte[] binaryData = new Byte[inFile.Length];
                    long bytesRead = inFile.Read(binaryData, 0,(int)inFile.Length);
                    inFile.Close();
                    string base64String = System.Convert.ToBase64String(binaryData,0,binaryData.Length);

                    xmlOut += "<exportzip>";
                    xmlOut += base64String;
                    xmlOut += "</exportzip>";
                }

                //theme.ascx.resx files
                xmlOut += LocalUtils.ExportResxXml(themefolder);

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
                        nbi.ItemID = -1; // new item
                        nbi.PortalId = objModInfo.PortalID;
                        nbi.ModuleId = moduleId;

                        var objPortal = PortalController.Instance.GetPortal(objModInfo.PortalID);

                        //realign images
                        if (nbi.XMLDoc != null)
                        {

                            var nodl = nbi.XMLDoc.SelectNodes("genxml/imgs/genxml");
                            if (nodl != null)
                            {
                                var lp = 1;
                                foreach (XmlNode xNod in nodl)
                                {
                                    // image url
                                    var imgurlnod = xNod.SelectSingleNode("genxml/hidden/imageurl");
                                    if (imgurlnod != null && imgurlnod.InnerText != "")
                                    {
                                        var imgurl = imgurlnod.InnerText;
                                        imgurl = imgurl.Replace("NBrightUpload", "*");
                                        var imgsplit = imgurl.Split('*');
                                        if (imgsplit.Length == 2)
                                        {
                                            imgurl = objPortal.HomeDirectory.TrimEnd('/') + "/" + imgsplit[1];
                                            var imgpath = System.Web.Hosting.HostingEnvironment.MapPath(imgurl);
                                            nbi.SetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imageurl", imgurl);
                                            nbi.SetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imagepath",
                                                imgpath);
                                        }
                                    }
                                    lp += 1;
                                }
                            }

                            //realign files
                            var nodfl = nbi.XMLDoc.SelectNodes("genxml/files/genxml");
                            if (nodfl != null)
                            {
                                var lp = 1;
                                foreach (XmlNode xNod in nodfl)
                                {
                                    // image url
                                    var fileurlnod = xNod.SelectSingleNode("genxml/hidden/fileurl");
                                    if (fileurlnod != null && fileurlnod.InnerText != "")
                                    {
                                        var fileurl = fileurlnod.InnerText;
                                        fileurl = fileurl.Replace("NBrightUpload", "*");
                                        var filesplit = fileurl.Split('*');
                                        if (filesplit.Length == 2)
                                        {
                                            fileurl = objPortal.HomeDirectory.TrimEnd('/') + "/" + filesplit[1];
                                            var filepath = System.Web.Hosting.HostingEnvironment.MapPath(fileurl);
                                            nbi.SetXmlProperty("genxml/files/genxml[" + lp + "]/hidden/fileurl", fileurl);
                                            nbi.SetXmlProperty("genxml/files/genxml[" + lp + "]/hidden/filepath",
                                                filepath);
                                        }
                                    }
                                    lp += 1;
                                }
                            }

                        }
                        // get new GUIDKey for settings records
                        if (nbi.TypeCode == "SETTINGS")
                        {
                            nbi.UserId = -1;
                                // flag to indicate a import has been done, used to trigger LocalUtils.ValidateModuleData
                            nbi.SetXmlProperty("genxml/hidden/singlepageitemid", "");
                                // clear singlepageitemid, this will always change on import and needs to be reset.

                            // set new upload paths
                            nbi = LocalUtils.CreateRequiredUploadFolders(nbi);

                        }

                        objCtrl.Update(nbi);
                    }

                }

                // realign the language records
                var link1 = objCtrl.GetList(objModInfo.PortalID, moduleId, "NBrightModDATA");
                foreach (var nbi in link1)
                {
                    var link2 = objCtrl.GetList(objModInfo.PortalID, moduleId, "NBrightModDATALANG",
                        " and NB1.parentitemid = " + nbi.GUIDKey);
                    foreach (var nbi2 in link2)
                    {
                        nbi2.ParentItemId = nbi.ItemID;
                        objCtrl.Update(nbi2);
                    }
                    nbi.GUIDKey = "";
                    objCtrl.Update(nbi);
                }

                // reset setting data for import
                var settings = LocalUtils.GetSettings(moduleId.ToString(""), false);
                if (settings != null)
                {
                    var newsinglepageflag = settings.GetXmlPropertyBool("genxml/hidden/singlepageedit");
                    if (newsinglepageflag && link1.Count > 0)
                    {
                        // set single item to first item in list.
                        settings.SetXmlProperty("genxml/hidden/singlepageitemid", link1[0].ItemID.ToString(""));
                        objCtrl.Update(settings);
                    }

                    var extractFolder = Utils.GetUniqueKey();
                    var extractMapPath = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp\\" + extractFolder;

                    var theme = settings.GetXmlProperty("genxml/dropdownlist/themefolder");
                    if (theme != "")
                    {

                        // import theme
                        var xmlNod = xmlDoc.SelectSingleNode("root/exportzip");
                        if (xmlNod != null)
                        {
                            var zipFile = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp\\NBrightMod_Theme_" + theme + ".zip";
                            if (!Directory.Exists(extractMapPath))
                            {
                                Directory.CreateDirectory(extractMapPath);
                            }
                            Utils.SaveBase64ToFile(zipFile, xmlNod.InnerText);
                            LocalUtils.ImportPortalTheme(zipFile,extractMapPath);
                        }

                        // merge/save resx files
                        var nodfl = xmlDoc.SelectNodes("genxml/resxfiles/resxfile");
                        LocalUtils.ImportResxXml(nodfl, theme);
                    }

                    // check if we have imported into same portal, if so reset the moduleref so it's still unique.
                    var oldmodref = settings.GUIDKey;
                    var checklistcount = objCtrl.GetListCount(PortalSettings.Current.PortalId, -1, "SETTINGS", " and NB1.GUIDKey = '" + settings.GUIDKey + "' ");
                    if (checklistcount >= 2)
                    {
                        // we have multiple module with this modref, so reset to a unique one.
                        var newref = Utils.GetUniqueKey(10);
                        settings.SetXmlProperty("genxml/hidden/modref", newref);
                        if (settings.GetXmlProperty("genxml/dropdownlist/datasourceref") == settings.GUIDKey)
                        {
                            settings.SetXmlProperty("genxml/dropdownlist/datasourceref", newref);
                        }
                        var objModule = DnnUtils.GetModuleinfo(moduleId);
                        settings.SetXmlProperty("genxml/ident", settings.GetXmlProperty("genxml/dropdownlist/themefolder") + ": " + objModule.ParentTab.TabName + " " + objModule.PaneName + " [" + newref + "]");
                        settings.GUIDKey = newref;
                        objCtrl.Update(settings);
                    }

                    // move extracted theme files to theme folder
                    var themeFolderName = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + theme;
                    if (!Directory.Exists(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod"))
                    {
                        Directory.CreateDirectory(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod");
                        Directory.CreateDirectory(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\");
                    }
                    var flist = Directory.GetFiles(extractMapPath, "*.*", SearchOption.AllDirectories);
                    foreach (var f in flist)
                    {
                        var dest = f.Replace(extractMapPath, themeFolderName).Replace(oldmodref, settings.GUIDKey);
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(f,dest);                        
                    }

                    if (Directory.Exists(extractMapPath))
                    {
                        Directory.Delete(extractMapPath, true);
                    }

                }

            }

        }

        #endregion

        #region ModuleSearchBase

        public override IList<SearchDocument> GetModifiedSearchDocuments(ModuleInfo modInfo, DateTime beginDate)
        {
            var searchDocuments = new List<SearchDocument>();

            var lastindexflag = false;
            var objcache = Utils.GetCache("dnnsearchindexflag" + modInfo.ModuleID.ToString(""));
            if (objcache != null) lastindexflag = (Boolean)objcache;

            if (!lastindexflag)
            {


                // Get all the non-langauge data records.
                var objCtrl = new NBrightDataController();

                var culturecodeList = DnnUtils.GetCultureCodeList(modInfo.PortalID);
                foreach (var lang in culturecodeList)
                {
                    var lstData = objCtrl.GetList(modInfo.PortalID, modInfo.ModuleID, "NBrightModDATA"); // reset search list objects to non-langauge ones.
                    string strContent = "";
                    var modifiedDate = DateTime.Now;
                    var searchTitle = modInfo.ModuleTitle;
                    if (searchTitle == "") searchTitle = modInfo.ParentTab.TabName;

                    // add lanaguge records to list to be indexed
                    var lstDataLang = objCtrl.GetList(modInfo.PortalID, modInfo.ModuleID, "NBrightModDATALANG", " and NB1.Lang = '" + lang + "' ");
                    foreach (var obj in lstDataLang)
                    {
                        lstData.Add(obj);
                    }

                    foreach (var objContent in lstData)
                    {
                        //content is encoded in the Database so Decode before Indexing
                        if (objContent.XMLDoc != null)
                        {

                            var xmlNods = objContent.XMLDoc.SelectNodes("genxml/textbox/*");
                            if (xmlNods != null)
                            {
                                foreach (XmlNode xmlNod in xmlNods)
                                {
                                    if (xmlNod.Attributes != null && xmlNod.Attributes["datatype"] != null && xmlNod.Attributes["datatype"].InnerText == "html")
                                        strContent += HttpUtility.HtmlDecode(xmlNod.InnerText) + " ";
                                    else
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
                        var description = strContent.Length <= 100 ? strContent : HtmlUtils.Shorten(strContent, 100, "...");

                        var searchDoc = new SearchDocument
                        {
                            UniqueKey = modInfo.ModuleID.ToString() + "*" + lang,
                            PortalId = modInfo.PortalID,
                            Title = searchTitle,
                            Description = description,
                            Body = strContent,
                            ModifiedTimeUtc = modifiedDate.ToUniversalTime(),
                            CultureCode = lang
                        };

                        if (modInfo.Terms != null && modInfo.Terms.Count > 0)
                        {
                            searchDoc.Tags = CollectHierarchicalTags(modInfo.Terms);
                        }

                        searchDocuments.Add(searchDoc);
                    }
                }
                Utils.SetCache("dnnsearchindexflag" + modInfo.ModuleID.ToString(""), true);
            }


            return searchDocuments;
        }

        private static List<string> CollectHierarchicalTags(List<Term> terms)
        {
            Func<List<Term>, List<string>, List<string>> collectTagsFunc = null;
            collectTagsFunc = (ts, tags) =>
            {
                if (ts != null && ts.Count > 0)
                {
                    foreach (var t in ts)
                    {
                        tags.Add(t.Name);
                        tags.AddRange(collectTagsFunc(t.ChildTerms, new List<string>()));
                    }
                }
                return tags;
            };

            return collectTagsFunc(terms, new List<string>());
        }

        #endregion


        #endregion

    }

}
