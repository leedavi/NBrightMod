using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Resources;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content.Taxonomy;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Search;
using DotNetNuke.Services.Search.Entities;
using NBrightCore;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using NBrightMod.common;

namespace Nevoweb.DNN.NBrightMod.Components
{

    public class NBrightModController : ModuleSearchBase, IPortable, IUpgradeable
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

                //DATA
                var l = objCtrl.GetList(portalId, ModuleId, "NBrightModDATA");
                foreach (var nbi in l)
                {
                    nbi.GUIDKey = nbi.ItemID.ToString(""); // set the GUIDKey to the current itemid, so we can relink lang record on import.
                    xmlOut += nbi.ToXmlItem();

                    //EXPORT Images
                    var imgNodList = nbi.XMLDoc.SelectNodes("genxml/imgs/genxml");
                    foreach (XmlNode imgNod in imgNodList)
                    {
                        var imgMapPath = "";
                        var iNod = imgNod.SelectSingleNode("hidden/imagepath");
                        if (iNod != null) imgMapPath = iNod.InnerText;
                        if (File.Exists(imgMapPath))
                        {
                            var iNod2 = imgNod.SelectSingleNode("hidden/imageurl");
                            if (iNod2 != null)
                            {
                                var filerelpath = iNod2.InnerText;
                                var imgByte = File.ReadAllBytes(imgMapPath);
                                var imgBase64 = Convert.ToBase64String(imgByte, Base64FormattingOptions.None);
                                xmlOut += "<imgbase64 filerelpath='" + filerelpath + "'>";
                                xmlOut += imgBase64;
                                xmlOut += "</imgbase64>";
                            }

                        }
                    }
                    //EXPORT File
                    var docsNodList = nbi.XMLDoc.SelectNodes("genxml/docs/genxml");
                    foreach (XmlNode docsNod in docsNodList)
                    {
                        var docMapPath = "";
                        var iNod = docsNod.SelectSingleNode("hidden/docpath");
                        if (iNod != null) docMapPath = iNod.InnerText;
                        if (File.Exists(docMapPath))
                        {
                            var filenameNod = docsNod.SelectSingleNode("hidden/filename");
                            var iNod2 = docsNod.SelectSingleNode("hidden/docurl");
                            if (iNod2 != null && filenameNod != null)
                            {
                                var filerelpath = iNod2.InnerText;
                                var imgByte = File.ReadAllBytes(docMapPath);
                                var imgBase64 = Convert.ToBase64String(imgByte, Base64FormattingOptions.None);
                                xmlOut += "<docbase64 filerelpath='" + filenameNod.InnerText + "'>";
                                xmlOut += imgBase64;
                                xmlOut += "</docbase64>";
                            }

                        }
                    }
                }
                //DATALANG
                var l2 = objCtrl.GetList(portalId, ModuleId, "NBrightModDATALANG");
                foreach (var nbi in l2)
                {
                            xmlOut += nbi.ToXmlItem();
                }
                // SETTINGS
                var settingsInfo = LocalUtils.GetSettings(ModuleId.ToString());
                xmlOut += settingsInfo.ToXmlItem();

                // EXPORT THEME
                var themefolder = settingsInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
                xmlOut += LocalUtils.ExportTheme(portalId, themefolder, settingsInfo.GUIDKey);
                
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
                    // delete records (for multiple imports)
                    var modList = objCtrl.GetList(objModInfo.PortalID, moduleId, "NBrightModDATA");
                    foreach (var nbi in modList)
                    {
                        objCtrl.Delete(nbi.ItemID);
                    }
                    modList = objCtrl.GetList(objModInfo.PortalID, moduleId, "SETTINGS");
                    foreach (var nbi in modList)
                    {
                        objCtrl.Delete(nbi.ItemID);
                    }


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
                                    var imgurlnod = xNod.SelectSingleNode("hidden/imageurl");
                                    if (imgurlnod != null && imgurlnod.InnerText != "")
                                    {
                                        var imgurl = imgurlnod.InnerText;
                                        imgurl = imgurl.Replace("NBrightUpload", "*");
                                        var imgsplit = imgurl.Split('*');
                                        if (imgsplit.Length == 2)
                                        {
                                            imgurl = "/" + objPortal.HomeDirectory.TrimEnd('/').TrimStart('/') + "/NBrightUpload/" + imgsplit[1].TrimStart('/');
                                            var imgpath = LocalUtils.GetHomePortalMapPath(objModInfo.PortalID).TrimEnd('\\') + "\\NBrightUpload\\" + imgsplit[1].Replace("/","\\").TrimStart('\\');
                                            nbi.SetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imageurl", imgurl);
                                            nbi.SetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imagepath", imgpath);

                                            try
                                            {
                                                var base64Node = xmlDoc.SelectSingleNode("root/imgbase64[@filerelpath='" + imgurlnod.InnerText + "']");
                                                if (base64Node != null)
                                                {
                                                    var base64String = base64Node.InnerText;
                                                    if (base64String != "")
                                                    {
                                                        File.WriteAllBytes(imgpath, Convert.FromBase64String(base64String));
                                                    }
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                // ignore
                                            }

                                        }
                                    }
                                    lp += 1;
                                }
                            }

                            //realign files
                            var nodfl = nbi.XMLDoc.SelectNodes("genxml/docs/genxml");
                            if (nodfl != null)
                            {
                                var lp = 1;
                                foreach (XmlNode xNod in nodfl)
                                {
                                    // image url
                                    var fileurlnod = xNod.SelectSingleNode("hidden/docurl");
                                    var filenamenod = xNod.SelectSingleNode("hidden/filename");
                                    if (fileurlnod != null && fileurlnod.InnerText != "" && filenamenod != null && filenamenod.InnerText != "")
                                    {
                                        var fileurl = fileurlnod.InnerText;
                                        fileurl = fileurl.Replace("NBrightUpload", "*");
                                        var filesplit = fileurl.Split('*');
                                        if (filesplit.Length == 2)
                                        {
                                            fileurl = "/" + objPortal.HomeDirectory.TrimEnd('/').TrimStart('/') + "/NBrightUpload/" + filesplit[1].TrimStart('/');
                                            var filepath = LocalUtils.GetHomePortalMapPath(objModInfo.PortalID).TrimEnd('\\') + "\\NBrightUpload\\" + filesplit[1].Replace("/", "\\").TrimStart('\\');
                                            nbi.SetXmlProperty("genxml/docs/genxml[" + lp + "]/hidden/docurl", fileurl);
                                            nbi.SetXmlProperty("genxml/docs/genxml[" + lp + "]/hidden/docpath", filepath);

                                            try
                                            {
                                                var base64Node = xmlDoc.SelectSingleNode("root/docbase64[@filename='" + filenamenod.InnerText + "']");
                                                if (base64Node != null)
                                                {
                                                    var base64String = base64Node.InnerText;
                                                    if (base64String != "")
                                                    {
                                                        File.WriteAllBytes(filepath, Convert.FromBase64String(base64String));
                                                    }
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                // ignore
                                            }



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
                    if (nbi.GUIDKey != "") // exists but not imported if guid empty.
                    {
                        var link2 = objCtrl.GetList(objModInfo.PortalID, moduleId, "NBrightModDATALANG", " and NB1.parentitemid = " + nbi.GUIDKey);
                        foreach (var nbi2 in link2)
                        {
                            nbi2.ParentItemId = nbi.ItemID;
                            objCtrl.Update(nbi2);
                        }
                        nbi.GUIDKey = "";
                        objCtrl.Update(nbi);
                    }
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


                    // check if we have imported into same portal, if so reset the moduleref so it's still unique.
                    var oldmodref = settings.GUIDKey;
                    var checklistcount = objCtrl.GetListCount(objModInfo.PortalID, -1, "SETTINGS", " and NB1.GUIDKey = '" + settings.GUIDKey + "' ");
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

                    var theme = settings.GetXmlProperty("genxml/dropdownlist/themefolder");
                    if (theme != "")
                    {
                        LocalUtils.ImportTheme(objModInfo.PortalID, theme, oldmodref, settings.GUIDKey);
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

            if (modInfo.IsDeleted) return searchDocuments; 

            if (!lastindexflag)
            {

                // Get all the non-langauge data records.
                var objCtrl = new NBrightDataController();

                var culturecodeList = DnnUtils.GetCultureCodeList(modInfo.PortalID);
                foreach (var lang in culturecodeList)
                {
                    var lstData = new List<NBrightInfo>();
                    var lstData1 = objCtrl.GetList(modInfo.PortalID, modInfo.ModuleID, "NBrightModDATA","", " order by NB1.XMLData.value('(genxml/hidden/sortrecordorder)[1]','int'), NB1.ModifiedDate"); // reset search list objects to non-langauge ones.
                    string strContent = "";
                    var modifiedDate = DateTime.Now;
                    var searchTitle = "";

                    foreach (var obj1 in lstData1)
                    {
                        lstData.Add(obj1);
                        // add lanaguge records to list to be indexed
                        var lstDataLang = objCtrl.GetList(modInfo.PortalID, modInfo.ModuleID, "NBrightModDATALANG", " and NB1.ParentItemId = '" + obj1.ItemID + "' and NB1.Lang = '" + lang + "' ", "");
                        foreach (var obj in lstDataLang)
                        {
                            lstData.Add(obj);
                        }
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
                                    //if (xmlNod.Attributes != null && xmlNod.Attributes["datatype"] != null && xmlNod.Attributes["datatype"].InnerText == "html")
                                        strContent += Regex.Replace(HttpUtility.HtmlDecode(xmlNod.InnerText), @"<[^>]+>|&nbsp;", "").Trim() + " ";
                                    //else
                                    //    strContent += xmlNod.InnerText + " ";
                                }
                            }

                            if (searchTitle == "")
                            {
                                var xNod = objContent.XMLDoc.SelectSingleNode("genxml/textbox/searchtitle");
                                if (xNod == null || xNod.InnerText.Trim() == "")
                                {
                                    xNod = objContent.XMLDoc.SelectSingleNode("genxml/textbox/title");
                                }
                                if (xNod != null && xNod.InnerText.Trim() != "") searchTitle = xNod.InnerText;
                            }

                            if (objContent.ModifiedDate < modifiedDate) modifiedDate = objContent.ModifiedDate;
                        }
                    }

                    if (searchTitle == "") searchTitle = modInfo.ParentTab.TabName;

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

        #region IUpgradeable Members
        public string UpgradeModule(string Version)
        {
            return string.Empty;
        }
        #endregion

        #endregion

    }

}
