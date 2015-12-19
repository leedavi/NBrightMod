﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Xml;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using NBrightCore;
using NBrightCore.common;
using NBrightCore.images;
using NBrightCore.render;
using NBrightDNN;
using NBrightMod.common;
using DataProvider = DotNetNuke.Data.DataProvider;
using System.Web.Script.Serialization;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.UI.WebControls;

namespace Nevoweb.DNN.NBrightMod
{
    /// <summary>
    /// Summary description for XMLconnector
    /// </summary>
    public class XmlConnector : IHttpHandler
    {
        private readonly JavaScriptSerializer _js = new JavaScriptSerializer();
        private String _lang = "";
        private String _itemid = "";

        public void ProcessRequest(HttpContext context)
        {
            #region "Initialize"

            var strOut = "";

            var moduleid = Utils.RequestQueryStringParam(context, "mid");
            var paramCmd = Utils.RequestQueryStringParam(context, "cmd");
            var lang = Utils.RequestQueryStringParam(context, "lang");
            var language = Utils.RequestQueryStringParam(context, "language");
            _itemid = Utils.RequestQueryStringParam(context, "itemid");

            #region "setup language"

            // because we are using a webservice the system current thread culture might not be set correctly,
            //  so use the lang/lanaguge param to set it.
            if (lang == "") lang = language;
            if (!string.IsNullOrEmpty(lang)) _lang = lang;

            // default to current thread if we have no language.
            if (_lang == "") _lang = System.Threading.Thread.CurrentThread.CurrentCulture.ToString();

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture(_lang);

            #endregion

            #endregion

            #region "Do processing of command"

            strOut = "ERROR!! - No Security rights for current user!";
            switch (paramCmd)
            {
                case "test":
                    strOut = "<root>" + UserController.GetCurrentUserInfo().Username + "</root>";
                    break;
                case "getsettings":
                    strOut = GetSettings(context);
                    break;
                case "gettheme":
                    strOut = GetSettings(context);
                    break;                    
                case "savesettings":
                    if (LocalUtils.CheckRights()) strOut = SaveSettings(context);
                    break;
                case "resetsettings":
                    if (LocalUtils.CheckRights()) strOut = ResetSettings(context);
                    break;
                case "getdetail":
                    strOut = GetData(context);
                    break;
                case "getselectlangdata":
                    strOut = GetData(context);
                    break;
                case "getlist":
                    strOut = GetData(context);
                    break;
                case "getimagelist":
                    strOut = GetData(context);
                    break;
                case "addnew":
                    if (LocalUtils.CheckRights()) strOut = GetData(context, true);
                    break;
                case "deleterecord":
                    if (LocalUtils.CheckRights()) strOut = DeleteData(context);
                    break;
                case "savedata":
                    if (LocalUtils.CheckRights())
                    {
                        SaveImages(context);
                        SaveDocs(context);
                        strOut = SaveData(context);
                    }
                    break;
                case "savelistdata":
                    if (LocalUtils.CheckRights())
                    {
                        strOut = SaveListData(context);
                    }
                    break;
                case "selectlang":
                    if (LocalUtils.CheckRights())
                    {
                        SaveImages(context);
                        SaveDocs(context);
                        strOut = SaveData(context);
                    }
                    break;
                case "fileupload":
                    if (LocalUtils.CheckRights()) FileUpload(context, moduleid);
                    break;
                case "addselectedfiles":
                    if (LocalUtils.CheckRights()) AddSelectedFiles(context);
                    break;
                case "replaceselectedfiles":
                    if (LocalUtils.CheckRights()) ReplaceSelectedFiles(context);
                    break;
                case "deleteselectedfiles":
                    if (LocalUtils.CheckRights()) DeleteSelectedFiles(context);
                    break;
                case "getfiles":
                        strOut = GetFiles(context, true);
                        break;
                case "getfolderfiles":
                        strOut = GetFolderFiles(context, true);                    
                    break;
                case "savetheme":
                    if (LocalUtils.CheckRights()) strOut = SaveTheme(context);
                    break;
                case "exporttheme":
                    if (LocalUtils.CheckRights()) strOut = DoThemeExport(context);
                    break;
                case "importtheme":
                    if (LocalUtils.CheckRights())
                    {
                        var fname1 = FileUpload(context, moduleid);
                        strOut = DoThemeImport(fname1);
                        LocalUtils.ClearRazorCache(moduleid);
                    }
                    break;
                case "downloadfile":
                        var fileindex = Utils.RequestQueryStringParam(context, "fileindex");
                    var itemid = Utils.RequestQueryStringParam(context, "itemid");
                    if (Utils.IsNumeric(itemid))
                    {
                        var objCtrl = new NBrightDataController();
                        var nbi = objCtrl.GetData(Convert.ToInt32(itemid));
                        var fpath = nbi.GetXmlProperty("genxml/docs/genxml[" + fileindex + "]/hidden/docpath");
                        var downloadname = Utils.RequestQueryStringParam(context, "downloadname");
                        if (downloadname == "") downloadname = Path.GetFileName(fpath);
                        UpdateDownloadCount(Convert.ToInt32(itemid), fileindex, 1);
                        Utils.ForceDocDownload(fpath, downloadname, context.Response);
                    }
                    else
                    {
                        var filename = Utils.RequestQueryStringParam(context, "filename");
                        if (filename != "")
                        {
                            var fpath = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\" + filename;
                            var downloadname = Utils.RequestQueryStringParam(context, "downloadname");
                            if (downloadname == "") downloadname = Path.GetFileName(fpath);
                            Utils.ForceDocDownload(fpath, downloadname, context.Response);
                        }
                    }
                    strOut = "File Download Error, unable to find download fileindex or filename params";
                    break;
                case "sendemail":
                        strOut = SendEmail(context);
                    break;
            }

            #endregion

            #region "return results"

            //send back xml as plain text
            context.Response.Clear();
            context.Response.ContentType = "text/plain";
            context.Response.Write(strOut);
            context.Response.End();

            #endregion

        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }


        #region "Methods"

        private void SetContextLangauge(NBrightInfo ajaxInfo = null)
        {
            // set langauge if we have it passed.
            if (ajaxInfo == null) ajaxInfo = new NBrightInfo(true);
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
            if (lang != "") _lang = lang;
            // set the context  culturecode, so any DNN functions use the correct culture 
            if (_lang != "" && _lang != System.Threading.Thread.CurrentThread.CurrentCulture.ToString()) System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(_lang);

        }

        private String GetSettings(HttpContext context, bool clearCache = false)
        {
            try
            {
                var strOut = "";
                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var razortemplate = ajaxInfo.GetXmlProperty("genxml/hidden/razortemplate");
                if (razortemplate == "") razortemplate = "settings.cshtml";
                if (moduleid == "") moduleid = "-1";

                if (clearCache) LocalUtils.ClearRazorCache(moduleid);


                // do edit field data if a itemid has been selected
                var obj = LocalUtils.GetSettings(moduleid);
                obj.ModuleId = Convert.ToInt32(moduleid); // assign for new records
                strOut = LocalUtils.RazorTemplRender(razortemplate, moduleid, "settings", obj, _lang);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String SaveSettings(HttpContext context)
        {
            try
            {

                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                if (Utils.IsNumeric(moduleid))
                {
                    // get DB record
                    var nbi = LocalUtils.GetSettings(moduleid);
                    if (nbi.ModuleId == 0) // new setting record
                    {
                        nbi = CreateSettingsInfo(moduleid,nbi);
                    }
                    // get data passed back by ajax
                    var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
                    // update record with ajax data
                    nbi.UpdateAjax(strIn);

                    // look for datasource moduleid and use xrefitemid to persist it, this is so we can clear cache of satelite modules.
                    var datasourceref = nbi.GetXmlProperty("genxml/dropdownlist/datasourceref");
                    if (datasourceref != nbi.GUIDKey && datasourceref != "")
                    {
                        var objCtrl = new NBrightDataController();
                        var satnbi = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "SETTINGS", datasourceref);
                        if (satnbi != null)
                        {
                            nbi.XrefItemId = satnbi.ItemID;
                        }
                    }


                    // check for special processing on guidkeys (unique key persists on export/import)
                    if (nbi.GetXmlProperty("genxml/dropdownlist/targetpage") != "")
                    {
                        var guidkey = nbi.GetXmlProperty("genxml/dropdownlist/targetpage");
                        var t = (from kvp in TabController.GetTabsBySortOrder(PortalSettings.Current.PortalId) where kvp.UniqueId.ToString() == guidkey select kvp.TabID);
                        if (t.Any())
                        {
                            nbi.SetXmlProperty("genxml/dropdownlist/targetpagetabid", t.First().ToString());
                        }
                    }
                    else
                    {
                        nbi.RemoveXmlNode("genxml/dropdownlist/targetpagetabid");
                    }
                    if (nbi.GetXmlProperty("genxml/hidden/modref") == "")
                    {
                        if (nbi.GUIDKey != "")
                        {
                            nbi.SetXmlProperty("genxml/hidden/modref", nbi.GUIDKey);
                        }
                        else
                        {
                            var gid = Utils.GetUniqueKey(10);
                            nbi.SetXmlProperty("genxml/hidden/modref", gid);
                            nbi.GUIDKey = gid;
                        }
                    }
                    if (nbi.TextData == "") nbi.TextData = "NBrightMod";

                    var settingUploadFolder = nbi.GetXmlProperty("genxml/textbox/settinguploadfolder");
                    if (settingUploadFolder == "")
                    {
                        settingUploadFolder = "images";
                        nbi.SetXmlProperty("genxml/textbox/settinguploadfolder", settingUploadFolder);
                    }
                    var uploadFolder = PortalSettings.Current.HomeDirectory.TrimEnd('/') + "/NBrightUpload/" + settingUploadFolder;
                    var uploadFolderMapPath = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightUpload\\" + settingUploadFolder;
                    Utils.CreateFolder(uploadFolderMapPath);
                    nbi.SetXmlProperty("genxml/uploadfolder", uploadFolder);
                    nbi.SetXmlProperty("genxml/uploadfoldermappath", uploadFolderMapPath);

                    LocalUtils.UpdateSettings(nbi);

                    LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));

                    LocalUtils.ClearRazorSateliteCache(nbi.ModuleId.ToString(""));

                    LocalUtils.ValidateModuleData();

                }
                return "";

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String ResetSettings(HttpContext context)
        {
            try
            {
                var objCtrl = new NBrightDataController();

                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                if (Utils.IsNumeric(moduleid) && Convert.ToInt32(moduleid) > 0)
                {
                    // get DB record
                    var nbi = LocalUtils.GetSettings(moduleid,false);
                    if (nbi.ItemID > 0) objCtrl.Delete(nbi.ItemID);
                    LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String SaveTheme(HttpContext context)
        {
            try
            {
                var objCtrl = new NBrightDataController();

                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                if (Utils.IsNumeric(moduleid))
                {
                    // get DB record
                    var nbi = LocalUtils.GetSettings(moduleid);

                    if (nbi.ModuleId <= 0) // new setting record
                    {
                        nbi = CreateSettingsInfo(moduleid, nbi);
                    }
                    if (nbi.ModuleId > 0)
                    {
                        nbi.UpdateAjax(LocalUtils.GetAjaxData(context));
                        objCtrl.Update(nbi);
                        LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }
        
        private NBrightInfo CreateSettingsInfo(String moduleid, NBrightInfo settings)
        {
            var modref = Utils.GetUniqueKey(10);
            //rebuild xml
            settings = LocalUtils.CreateRequiredUploadFolders(settings);
            settings.PortalId = PortalSettings.Current.PortalId;
            settings.ModuleId = Convert.ToInt32(moduleid);
            settings.TypeCode = "SETTINGS";
            settings.Lang = "";
            settings.GUIDKey = modref;
            return settings;
        }

        private String GetData(HttpContext context, bool clearCache = false)
        {
            try
            {
                var objCtrl = new NBrightDataController();
                var strOut = "";
                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
                var newitem = ajaxInfo.GetXmlProperty("genxml/hidden/newitem");
                var selecteditemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
                var displayreturn = ajaxInfo.GetXmlProperty("genxml/hidden/displayreturn");
                var uploadtype = ajaxInfo.GetXmlProperty("genxml/hidden/uploadtype");

                if (editlang == "") editlang = _lang;

                if (moduleid == "") moduleid = "-1";

                if (clearCache) LocalUtils.ClearRazorCache(moduleid);


                if (newitem == "new")
                {
                    selecteditemid = "new"; // return list on new record
                    AddNew(moduleid);
                }

                var strTemplate = "editlist.cshtml";
                if (Utils.IsNumeric(selecteditemid)) strTemplate = "editfields.cshtml";


                switch (displayreturn.ToLower())
                {
                    case "list":
                        // removed selected itemid if we want to return to the list.
                        strTemplate = "editlist.cshtml";
                        selecteditemid = "";
                        break;
                }

                if (Utils.IsNumeric(selecteditemid))
                {
                    // do edit field data if a itemid has been selected
                    var obj = objCtrl.Get(Convert.ToInt32(selecteditemid), editlang);
                    if (obj != null)
                    {
                        // check we have a base data record, if so create langauge record.
                        var lnode = obj.XMLDoc.SelectSingleNode("genxml/lang");
                        if (lnode == null)
                        {
                            LocalUtils.CreateLangaugeDataRecord(obj.ItemID, Convert.ToInt32(moduleid), editlang);
                            obj = objCtrl.Get(Convert.ToInt32(selecteditemid), editlang);
                        }
                    }

                    strOut = LocalUtils.RazorTemplRender(strTemplate, moduleid, _lang + itemid + editlang + selecteditemid, obj, _lang);
                }
                else
                {
                    // preprocess razor template to get meta data for data select into cache.
                    var cachedlist = LocalUtils.RazorPreProcessTempl(strTemplate, moduleid, Utils.GetCurrentCulture());
                    var orderby = "";
                    if (cachedlist != null && cachedlist.ContainsKey("orderby")) orderby = cachedlist["orderby"];

                    var settings = LocalUtils.GetSettings(moduleid);

                    // Return list of items
                    var returnlimit = settings.GetXmlPropertyInt("genxml/textbox/returnlimit");
                    var l = objCtrl.GetList(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), "NBrightModDATA", "", orderby, returnlimit, 0, 0, 0, editlang);
                    if (l.Any())
                    {
                        // check we have a base data recxord, if so create langauge record.
                        foreach (var nbi in l)
                        {
                            var lnode = nbi.XMLDoc.SelectSingleNode("genxml/lang");
                            if (lnode == null)
                            {
                                LocalUtils.CreateLangaugeDataRecord(nbi.ItemID, Convert.ToInt32(moduleid), editlang);
                            }
                        }
                        l = objCtrl.GetList(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), "NBrightModDATA", "", orderby, returnlimit, 0, 0, 0, editlang);

                    }

                    strOut = LocalUtils.RazorTemplRenderList(strTemplate, moduleid, _lang + editlang, l, _lang);
                }

                // debug data out by writing out to file (REMOVE FOR PROUCTION)
                //Utils.SaveFile(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_NBrightMod_getData.txt", strOut);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String AddNew(String moduleid)
        {
            return LocalUtils.AddNew(moduleid);
        }

        private String SaveData(HttpContext context)
        {
            try
            {
                var objCtrl = new NBrightDataController();

                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
                var selecteditemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
                if (lang == "") lang = _lang;

                if (Utils.IsNumeric(itemid))
                {
                    // get DB record
                    var nbi = objCtrl.Get(Convert.ToInt32(itemid));
                    if (nbi != null)
                    {
                        // get data passed back by ajax
                        var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
                        // update record with ajax data
                        nbi.UpdateAjax(strIn);
                        nbi.TextData = ""; // clear any output DB caching
                        objCtrl.Update(nbi);

                        // do langauge record
                        nbi = objCtrl.GetDataLang(Convert.ToInt32(itemid), lang);
                        nbi.UpdateAjax(strIn);
                        nbi.TextData = ""; // clear any output DB caching
                        objCtrl.Update(nbi);

                        objCtrl.FillEmptyLanguageFields(nbi.ParentItemId, nbi.Lang);

                        Utils.RemoveCache("dnnsearchindexflag" + moduleid);

                        LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));

                        LocalUtils.ClearRazorSateliteCache(nbi.ModuleId.ToString(""));

                    }
                }
                return "";

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String SaveListData(HttpContext context)
        {
            try
            {
                var objCtrl = new NBrightDataController();
                var moduleid = "";
                // get data passed back by ajax
                var ajaxList = LocalUtils.GetAjaxDataList(context);
                var lp = 1;
                foreach (var ajaxData in ajaxList)
                {
                    var ajaxInfo = LocalUtils.GetAjaxFields(ajaxData);

                    SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.
                    var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
                    moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                    var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
                    if (lang == "") lang = _lang;

                    if (Utils.IsNumeric(itemid))
                    {
                        // get DB record
                        var nbi = objCtrl.Get(Convert.ToInt32(itemid));
                        if (nbi != null)
                        {
                            // update record with ajax data
                            nbi.UpdateAjax(ajaxData);
                            nbi.SetXmlProperty("genxml/hidden/sortrecordorder",lp.ToString("0000")); // always recalc custom sort field
                            objCtrl.Update(nbi);

                            // do langauge record
                            nbi = objCtrl.GetDataLang(Convert.ToInt32(itemid), lang);
                            nbi.UpdateAjax(ajaxData);
                            objCtrl.Update(nbi);

                            objCtrl.FillEmptyLanguageFields(nbi.ParentItemId, nbi.Lang);
                        }
                    }
                    lp += 1;
                }
                if (moduleid != "")
                {
                    LocalUtils.ClearRazorCache(moduleid);
                    LocalUtils.ClearRazorSateliteCache(moduleid);
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String DeleteData(HttpContext context)
        {
            try
            {
                var objCtrl = new NBrightDataController();

                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);

                var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
                var selecteddeleteid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteddeleteid");
                if (Utils.IsNumeric(selecteddeleteid)) itemid = selecteddeleteid;
                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
                if (editlang == "") editlang = _lang;

                if (Utils.IsNumeric(itemid))
                {
                    // delete DB record
                    objCtrl.Delete(Convert.ToInt32(itemid));

                    LocalUtils.ClearRazorCache(moduleid);

                }
                return "";

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String ExtractFileData(String xmlData, String updateType)
        {
            var objInfo = new NBrightInfo(true);
            var ajaxInfo = new NBrightInfo();
            ajaxInfo.XMLData = xmlData;
            var nodList2 = ajaxInfo.XMLDoc.SelectNodes("genxml/*");
            if (nodList2 != null)
            {
                foreach (XmlNode nod1 in nodList2)
                {
                    var nodList = ajaxInfo.XMLDoc.SelectNodes("genxml/" + nod1.Name.ToLower() + "/*");
                    if (nodList != null)
                    {
                        foreach (XmlNode nod in nodList)
                        {
                            if (nod.Attributes != null && nod.Attributes["update"] != null)
                            {
                                if (nod1.Name.ToLower() == "checkboxlist")
                                {
                                    if (nod.Attributes["update"].InnerText.ToLower() == updateType)
                                    {
                                        objInfo.RemoveXmlNode("genxml/checkboxlist/" + nod.Name.ToLower());
                                        objInfo.AddXmlNode(nod.OuterXml, nod.Name.ToLower(), "genxml/checkboxlist");
                                    }
                                }
                                else
                                {
                                    if (nod.Attributes["update"].InnerText.ToLower() == updateType)
                                    {
                                        objInfo.SetXmlProperty("genxml/" + nod1.Name.ToLower() + "/" + nod.Name.ToLower(), nod.InnerText);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return objInfo.XMLData;
        }

        private String DoThemeExport(HttpContext context)
        {
            try
            {
                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                var theme = ajaxInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
                if (theme != "")
                {
                    var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod");
                    var themelevel = ajaxInfo.GetXmlProperty("genxml/radiobuttonlist/exportthemelevel");
                    var level = "system";
                    var themeFolderName = controlMapPath + "\\Themes\\" + theme;
                    if (themelevel == "1")
                    {
                        level = "portal";
                        themeFolderName = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + theme;
                    }
                    var zipFile = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp\\NBrightMod_Theme_" +  theme + "_" + level + ".zip";

                    DnnUtils.ZipFolder(themeFolderName, zipFile);
                    
                    return "NBrightTemp\\" + Path.GetFileName(zipFile);
                }
                return "";
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.ToString();
            }

        }

        private String DoThemeImport(String zipFileMapPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(zipFileMapPath))
                {
                    var s = Path.GetFileNameWithoutExtension(zipFileMapPath).Split('_');
                    if (s.Count() == 4)
                    {
                        
                        var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod");
                        var level = s[3].ToLower();
                        var theme = s[2];
                        if (level == "module" || level == "portal")
                        {
                            var themeFolderName = controlMapPath + "\\Themes\\" + theme;
                            if (level == "portal")
                            {
                                themeFolderName = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + theme;
                                if (!Directory.Exists(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod"))
                                {
                                    Directory.CreateDirectory(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod");
                                    Directory.CreateDirectory(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\");
                                }
                            }

                            DnnUtils.UnZip(zipFileMapPath, themeFolderName);
                            return "";
                        }
                    }
                    return "ERROR: Invalid Theme File Name";
                }
                return "ERROR: Upload Failed";
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.ToString();
            }
        }

        private String SendEmail(HttpContext context)
        {
            try
            {
                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
                if (lang == "") lang = _lang;
                var clienttemplate = ajaxInfo.GetXmlProperty("genxml/hidden/clienttemplate");
                if (clienttemplate == "") clienttemplate = "clientemail.cshtml";
                var managertemplate = ajaxInfo.GetXmlProperty("genxml/hidden/managertemplate");
                if (managertemplate == "") managertemplate = "manageremail.cshtml";
                var emailreturnmsg = ajaxInfo.GetXmlProperty("genxml/hidden/emailreturnmsg");
                var clientemail = ajaxInfo.GetXmlProperty("genxml/textbox/clientemail");

                var strOut = "ERROR - Email Unable to be sent";

                if (!string.IsNullOrEmpty(clientemail.Trim()) && Utils.IsEmail(clientemail.Trim()))
                {
                    if (Utils.IsNumeric(moduleid))
                    {
                        var objCtrl = new NBrightDataController();

                        var settings = LocalUtils.GetSettings(moduleid);
                        var emailstosave = settings.GetXmlProperty("genxml/textbox/emailstosave");

                        // get DB record
                        var nbi = new NBrightInfo(true);
                        // get data passed back by ajax
                        var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
                        // update record with ajax data
                        nbi.UpdateAjax(strIn);
                        nbi.ModuleId = Convert.ToInt32(moduleid);
                        nbi.TypeCode = "NBrightModDATA";
                        if (Utils.IsNumeric(emailstosave) && Convert.ToInt32(emailstosave) > 0) objCtrl.Update(nbi);

                        // do edit field data if a itemid has been selected
                        var emailbody = LocalUtils.RazorTemplRender(managertemplate, moduleid, "", nbi, _lang);

                        var emailarray = settings.GetXmlProperty("genxml/textbox/emailto").Split(',');
                        var emailsubject = settings.GetXmlProperty("genxml/textbox/emailsubject");
                        var emailfrom = settings.GetXmlProperty("genxml/textbox/emailfrom");
                        var copytoclient = settings.GetXmlPropertyBool("genxml/checkbox/copytoclient");

                        foreach (var email in emailarray)
                        {
                            if (!string.IsNullOrEmpty(email.Trim()) && Utils.IsEmail(emailfrom.Trim()) && Utils.IsEmail(email.Trim()))
                            {
                                // multiple attachments as csv with "|" seperator
                                DotNetNuke.Services.Mail.Mail.SendMail(emailfrom.Trim(), email.Trim(), "", emailsubject, emailbody, "", "HTML", "", "", "", "");
                                strOut = emailreturnmsg;
                            }
                        }

                        // Delete Extra unrequired email data
                        if (Utils.IsEmail(emailfrom.Trim()))
                        {
                            if (Utils.IsNumeric(emailstosave) && Convert.ToInt32(emailstosave) > 0)
                            {
                                var l = objCtrl.GetList(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), "NBrightModDATA", "", " order by NB1.ModifiedDate");
                                if (l.Count > Convert.ToInt32(emailstosave))
                                {
                                    var c = 1;
                                    foreach (var i in l)
                                    {
                                        if (c > Convert.ToInt32(emailstosave))
                                        {
                                            objCtrl.Delete(i.ItemID);
                                        }
                                        c += 1;
                                    }
                                }
                            }
                        }

                        if (copytoclient)
                        {
                            if (!string.IsNullOrEmpty(clientemail.Trim()) && Utils.IsEmail(emailfrom.Trim()) && Utils.IsEmail(clientemail.Trim()))
                            {
                                var clientemailbody = LocalUtils.RazorTemplRender(clienttemplate, moduleid, "", nbi, _lang);
                                DotNetNuke.Services.Mail.Mail.SendMail(emailfrom.Trim(), clientemail.Trim(), "", emailsubject, clientemailbody, "", "HTML", "", "", "", "");
                            }
                        }
                    }
                }
                else
                {
                    var invalidemailmsg = ajaxInfo.GetXmlProperty("genxml/hidden/invalidemailmsg");
                    strOut = invalidemailmsg;
                }
                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }


        #endregion

        #region "Images"

        private String SaveImages(HttpContext context)
        {
            var objCtrl = new NBrightDataController();

            var ajaxInfo = LocalUtils.GetAjaxFields(context);
            SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
            if (Utils.IsNumeric(itemid))
            {
                var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
                if (lang == "") lang = _lang;

                var strAjaxXml = ajaxInfo.GetXmlProperty("genxml/hidden/xmlupdateimages");
                strAjaxXml = GenXmlFunctions.DecodeCDataTag(strAjaxXml);
                var imgList = LocalUtils.GetGenXmlListByAjax(strAjaxXml);

                // get DB record
                var nbi = objCtrl.Get(Convert.ToInt32(itemid));
                if (nbi != null)
                {
                    var nbilang = objCtrl.GetDataLang(Convert.ToInt32(itemid), lang);
                    // build xml for data records
                    var strXml = "<genxml><imgs>";
                    var strXmlLang = "<genxml><imgs>";
                    foreach (var imgInfo in imgList)
                    {
                        strXml += ExtractFileData(imgInfo.XMLData, "imgsave");
                        strXmlLang += ExtractFileData(imgInfo.XMLData, "imglang");
                    }
                    strXml += "</imgs></genxml>";
                    strXmlLang += "</imgs></genxml>";

                    // replace image xml 
                    nbi.ReplaceXmlNode(strXml, "genxml/imgs", "genxml");
                    objCtrl.Update(nbi);
                    if (nbilang != null)
                    {
                        nbilang.ReplaceXmlNode(strXmlLang, "genxml/imgs", "genxml");
                        objCtrl.Update(nbilang);
                    }
                    LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));
                }
            }
            return "";
        }

        private String GetImages(NBrightInfo ajaxInfo, bool clearCache = false)
        {
            try
            {
                var objCtrl = new NBrightDataController();
                var strOut = "";
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
                if (editlang == "") editlang = _lang;

                if (moduleid == "") moduleid = "-1";

                if (clearCache) LocalUtils.ClearRazorCache(moduleid);

                if (Utils.IsNumeric(itemid))
                {
                    // do edit field data if a itemid has been selected
                    var obj = objCtrl.GetData(Convert.ToInt32(itemid), editlang, editlang, clearCache);
                    var imgList = obj.XMLDoc.SelectNodes("genxml/imgs/genxml");
                    if (imgList != null)
                    {
                        var imgListLang = obj.XMLDoc.SelectNodes("genxml/lang/genxml/imgs/genxml");
                        var l = new List<NBrightInfo>();
                        var c = 1;
                        foreach (XmlNode i in imgList)
                        {
                            var nbi = new NBrightInfo();
                            nbi.XMLData = i.OuterXml;
                            nbi.ItemID = c;
                            nbi.Lang = obj.Lang;

                            if (imgListLang != null && imgListLang.Count >= c)
                            {
                                var langXml = imgListLang[c - 1].OuterXml;
                                nbi.AddSingleNode("lang", langXml, "genxml");
                            }

                            l.Add(nbi);
                            c += 1;
                        }
                        if (l.Count > 0) strOut = LocalUtils.RazorTemplRenderList("imglist.cshtml", moduleid, _lang + editlang, l, _lang);
                    }
                }

                // debug data out by writing out to file (REMOVE FOR PROUCTION)
                //Utils.SaveFile(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_NBrightContent_getimages.txt", strOut);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetFolderImages(NBrightInfo ajaxInfo, bool clearCache = false)
        {
            try
            {
                var strOut = "";
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
                if (editlang == "") editlang = _lang;
                if (moduleid == "") moduleid = "-1";
                if (clearCache) LocalUtils.ClearRazorCache(moduleid);

                var modSettings = LocalUtils.GetSettings(moduleid);
                var uploadfolder = modSettings.GetXmlProperty("genxml/uploadfoldermappath");

                DirectoryInfo dirInfo = new DirectoryInfo(uploadfolder);
                var extensionArray = new HashSet<string>();
                extensionArray.Add(".jpg");
                extensionArray.Add(".png");
                HashSet<string> allowedExtensions = new HashSet<string>(extensionArray, StringComparer.OrdinalIgnoreCase);
                FileInfo[] files = Array.FindAll(dirInfo.GetFiles(), f => allowedExtensions.Contains(f.Extension));

                var imgl = new List<NBrightInfo>();

                foreach (var f in files)
                {
                    var imageurl = modSettings.GetXmlProperty("genxml/uploadfolder").TrimEnd('/') + "/" + Path.GetFileName(f.FullName);
                    var nbi = new NBrightInfo(true);
                    nbi.SetXmlProperty("genxml/hidden/filename", f.Name);
                    nbi.SetXmlProperty("genxml/hidden/name", f.Name.Replace(f.Extension, ""));
                    nbi.SetXmlProperty("genxml/hidden/imageurl", imageurl);
                    imgl.Add(nbi);
                }


                if (imgl.Count > 0) strOut = LocalUtils.RazorTemplRenderList("imgselectlist.cshtml", moduleid, _lang, imgl, _lang);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String ReplaceSelectedImages(NBrightInfo ajaxInfo)
        {
            //get uploaded params
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");

            if (Utils.IsNumeric(itemid))
            {
                var objCtrl = new NBrightDataController();
                var dataRecord = objCtrl.Get(Convert.ToInt32(itemid));
                if (dataRecord != null)
                {
                    dataRecord.RemoveXmlNode("genxml/imgs");
                    objCtrl.Update(dataRecord);
                    AddSelectedImages(ajaxInfo);
                }
            }
            return "";
        }

        private String AddSelectedImages(NBrightInfo ajaxInfo)
        {
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
            var selectedimages = ajaxInfo.GetXmlProperty("genxml/hidden/selectedfiles");
            var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
            var modSettings = LocalUtils.GetSettings(moduleid);
            var singlefile = ajaxInfo.GetXmlPropertyBool("genxml/hidden/singlefile");

            if (Utils.IsNumeric(itemid))
            {
                var flist = selectedimages.Split(',');
                var alreadyaddedlist = new List<String>();
                foreach (var f in flist)
                {
                    if (ImgUtils.IsImageFile(Path.GetExtension(f)))
                    {
                        var imagepath = modSettings.GetXmlProperty("genxml/uploadfoldermappath").TrimEnd('\\') + "\\" + f;
                        var imageurl = modSettings.GetXmlProperty("genxml/uploadfolder").TrimEnd('/') + "/" + Path.GetFileName(imagepath);
                        if (!alreadyaddedlist.Contains(imagepath))
                        {
                            AddNewImage(Convert.ToInt32(itemid), imageurl, imagepath, singlefile);
                            alreadyaddedlist.Add(imageurl);
                        }
                    }
                }
                LocalUtils.ClearRazorCache(modSettings.ModuleId.ToString(""));
            }
            return "";
        }

        private String DeleteSelectedImages(NBrightInfo ajaxInfo)
        {
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
            var selectedimages = ajaxInfo.GetXmlProperty("genxml/hidden/selectedfiles");
            var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
            var modSettings = LocalUtils.GetSettings(moduleid);

            if (Utils.IsNumeric(itemid))
            {
                var flist = selectedimages.Split(',');
                foreach (var f in flist)
                {
                    if (f != "")
                    {
                        var imagepath = modSettings.GetXmlProperty("genxml/uploadfoldermappath").TrimEnd('\\') + "\\" + f;
                        if (File.Exists(imagepath)) File.Delete(imagepath);
                    }
                }
            }
            return "";
        }

        private void UpdateImage(String imgmappath, String itemid, NBrightInfo modSettings)
        {
            if (Utils.IsNumeric(itemid))
            {
                //get uploaded params
                if (ImgUtils.IsImageFile(Path.GetExtension(imgmappath)) && imgmappath != "")
                {
                    if (File.Exists(imgmappath))
                    {

                        var imgResize = modSettings.GetXmlPropertyInt("genxml/textbox/imgresize");
                        if (imgResize == 0) imgResize = 800;
                        var imagepath = ResizeImage(imgmappath, modSettings, imgResize);

                        // don;t update record with uploaded images
                        //var imageurl = modSettings.GetXmlProperty("genxml/uploadfolder").TrimEnd('/') + "/" + Path.GetFileName(imagepath);
                        //var replaceimages = (modSettings.GetXmlPropertyBool("genxml/checkbox/replacefiles") || modSettings.GetXmlPropertyBool("genxml/hidden/replacefiles"));
                        //if (replaceimages)
                        //{
                        //    var objCtrl = new NBrightDataController();
                        //    var dataRecord = objCtrl.Get(Convert.ToInt32(itemid));
                        //    if (dataRecord != null)
                        //    {
                        //        dataRecord.RemoveXmlNode("genxml/imgs");
                        //        objCtrl.Update(dataRecord);
                        //    }
                        //}
                        //AddNewImage(Convert.ToInt32(itemid), imageurl, imagepath);
                    }
                }
                LocalUtils.ClearRazorCache(modSettings.ModuleId.ToString(""));
            }
        }

        private String ResizeImage(String fullName, NBrightInfo modSettings, int imgSize = 640)
        {
            if (ImgUtils.IsImageFile(Path.GetExtension(fullName)))
            {
                var extension = Path.GetExtension(fullName);
                var newImageFileName = modSettings.GetXmlProperty("genxml/uploadfoldermappath").TrimEnd('\\') + "\\" + Utils.GetUniqueKey() + extension;
                if (extension != null && extension.ToLower() == ".png")
                {
                    newImageFileName = ImgUtils.ResizeImageToPng(fullName, newImageFileName, imgSize);
                }
                else
                {
                    newImageFileName = ImgUtils.ResizeImageToJpg(fullName, newImageFileName, imgSize);
                }
                Utils.DeleteSysFile(fullName);

                return newImageFileName;

            }
            return "";
        }

        private void AddNewImage(int itemId, String imageurl, String imagepath, Boolean singleimage = false)
        {
            var objCtrl = new NBrightDataController();
            var dataRecord = objCtrl.Get(itemId);
            if (dataRecord != null)
            {
                var f = Path.GetFileName(imagepath);
                var r = Path.GetFileNameWithoutExtension(imagepath);
                var strXml = "<genxml><imgs><genxml><hidden><ref>" + r + "</ref><filename>" + f + "</filename><imagepath>" + imagepath + "</imagepath><imageurl>" + imageurl + "</imageurl></hidden></genxml></imgs></genxml>";

                if (singleimage) dataRecord.RemoveXmlNode("genxml/imgs");

                if (dataRecord.XMLDoc.SelectSingleNode("genxml/imgs") == null)
                {
                    dataRecord.AddXmlNode(strXml, "genxml/imgs", "genxml");
                }
                else
                {
                    dataRecord.AddXmlNode(strXml, "genxml/imgs/genxml", "genxml/imgs");
                }
                objCtrl.Update(dataRecord);
            }
        }

        #endregion

        #region "Docs"

        private String SaveDocs(HttpContext context)
        {
            var objCtrl = new NBrightDataController();

            var ajaxInfo = LocalUtils.GetAjaxFields(context);
            SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
            if (Utils.IsNumeric(itemid))
            {
                var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
                if (lang == "") lang = _lang;

                var strAjaxXml = ajaxInfo.GetXmlProperty("genxml/hidden/xmlupdatedocs");
                strAjaxXml = GenXmlFunctions.DecodeCDataTag(strAjaxXml);
                var docList = LocalUtils.GetGenXmlListByAjax(strAjaxXml);

                // get DB record
                var nbi = objCtrl.Get(Convert.ToInt32(itemid));
                if (nbi != null)
                {
                    var nbilang = objCtrl.GetDataLang(Convert.ToInt32(itemid), lang);
                    // build xml for data records
                    var strXml = "<genxml><docs>";
                    var strXmlLang = "<genxml><docs>";
                    foreach (var docInfo in docList)
                    {
                        strXml += ExtractFileData(docInfo.XMLData, "docsave");
                        strXmlLang += ExtractFileData(docInfo.XMLData, "doclang");
                    }
                    strXml += "</docs></genxml>";
                    strXmlLang += "</docs></genxml>";

                    // replace image xml 
                    nbi.ReplaceXmlNode(strXml, "genxml/docs", "genxml");
                    objCtrl.Update(nbi);
                    if (nbilang != null)
                    {
                        nbilang.ReplaceXmlNode(strXmlLang, "genxml/docs", "genxml");
                        objCtrl.Update(nbilang);
                    }
                    LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));
                }
            }
            return "";
        }

        private String GetDocs(NBrightInfo ajaxInfo, bool clearCache = false)
        {
            try
            {
                var objCtrl = new NBrightDataController();
                var strOut = "";
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
                if (editlang == "") editlang = _lang;

                if (moduleid == "") moduleid = "-1";

                if (clearCache) LocalUtils.ClearRazorCache(moduleid);

                if (Utils.IsNumeric(itemid))
                {
                    // do edit field data if a itemid has been selected
                    var obj = objCtrl.GetData(Convert.ToInt32(itemid), editlang, editlang, clearCache);
                    var docList = obj.XMLDoc.SelectNodes("genxml/docs/genxml");
                    if (docList != null)
                    {
                        var docListLang = obj.XMLDoc.SelectNodes("genxml/lang/genxml/docs/genxml");
                        var l = new List<NBrightInfo>();
                        var c = 1;
                        foreach (XmlNode i in docList)
                        {
                            var nbi = new NBrightInfo();
                            nbi.XMLData = i.OuterXml;
                            nbi.ItemID = c;
                            nbi.Lang = obj.Lang;

                            if (docListLang != null && docListLang.Count >= c)
                            {
                                var langXml = docListLang[c - 1].OuterXml;
                                nbi.AddSingleNode("lang", langXml, "genxml");
                            }

                            var docpath = nbi.GetXmlProperty("genxml/hidden/docpath");
                            if (File.Exists(docpath)) l.Add(nbi);
                            c += 1;
                        }
                        if (l.Count > 0) strOut = LocalUtils.RazorTemplRenderList("doclist.cshtml", moduleid, _lang + editlang, l, _lang);
                    }
                }

                // debug data out by writing out to file (REMOVE FOR PROUCTION)
                //Utils.SaveFile(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_NBrightContent_getimages.txt", strOut);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetFolderDocs(NBrightInfo ajaxInfo, bool clearCache = false)
        {
            try
            {
                var strOut = "";
                SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.

                var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
                if (editlang == "") editlang = _lang;
                if (moduleid == "") moduleid = "-1";
                if (clearCache) LocalUtils.ClearRazorCache(moduleid);

                var modSettings = LocalUtils.GetSettings(moduleid);
                var uploadfolder = modSettings.GetXmlProperty("genxml/uploaddocfoldermappath");
                var allowedfiletypes = modSettings.GetXmlProperty("genxml/textbox/allowedfiletypes");
                if (allowedfiletypes == "") allowedfiletypes = "*";
                var allowedfiletypeslist = allowedfiletypes.ToLower().Split(',');

                FileInfo[] files;
                DirectoryInfo dirInfo = new DirectoryInfo(uploadfolder);
                if (allowedfiletypes == "*")
                {
                    var extensionArray = new HashSet<string>();
                    extensionArray.Add(".jpg");
                    extensionArray.Add(".png");
                    HashSet<string> allowedExtensions = new HashSet<string>(extensionArray, StringComparer.OrdinalIgnoreCase);
                    files = Array.FindAll(dirInfo.GetFiles(), f => !allowedExtensions.Contains(f.Extension));
                }
                else
                {
                    var extensionArray = new HashSet<string>();
                    foreach (var e in allowedfiletypeslist)
                    {
                        extensionArray.Add("." + e);
                    }

                    HashSet<string> allowedExtensions = new HashSet<string>(extensionArray, StringComparer.OrdinalIgnoreCase);
                    files = Array.FindAll(dirInfo.GetFiles(), f => allowedExtensions.Contains(f.Extension));
                }

                var imgl = new List<NBrightInfo>();

                foreach (var f in files)
                {
                    var docurl = modSettings.GetXmlProperty("genxml/uploaddocfolder").TrimEnd('/') + "/" + Path.GetFileName(f.FullName);
                    var docref = Path.GetFileNameWithoutExtension(f.Name).Replace(" ", "-");
                    var nbi = new NBrightInfo(true);
                    nbi.SetXmlProperty("genxml/hidden/filename", f.Name);
                    nbi.SetXmlProperty("genxml/hidden/name", f.Name.Replace(f.Extension, ""));
                    nbi.SetXmlProperty("genxml/hidden/docurl", docurl);
                    nbi.SetXmlProperty("genxml/hidden/ref", docref);
                    imgl.Add(nbi);
                }


                if (imgl.Count > 0) strOut = LocalUtils.RazorTemplRenderList("docselectlist.cshtml", moduleid, _lang, imgl, _lang);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String ReplaceSelectedDocs(NBrightInfo ajaxInfo)
        {
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");

            if (Utils.IsNumeric(itemid))
            {
                var objCtrl = new NBrightDataController();
                var dataRecord = objCtrl.Get(Convert.ToInt32(itemid));
                if (dataRecord != null)
                {
                    dataRecord.RemoveXmlNode("genxml/docs");
                    objCtrl.Update(dataRecord);
                    AddSelectedDocs(ajaxInfo);
                }
            }
            return "";
        }

        private String AddSelectedDocs(NBrightInfo ajaxInfo)
        {
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
            var selecteddocs = ajaxInfo.GetXmlProperty("genxml/hidden/selectedfiles");
            var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
            var modSettings = LocalUtils.GetSettings(moduleid);

            if (Utils.IsNumeric(itemid))
            {
                var allowedfiletypes = modSettings.GetXmlProperty("genxml/textbox/allowedfiletypes");
                if (allowedfiletypes == "") allowedfiletypes = "*";
                var allowedfiletypeslist = allowedfiletypes.ToLower().Split(',');
                var flist = selecteddocs.Split(',');
                var alreadyaddedlist = new List<String>();
                foreach (var f in flist)
                {
                    if ((allowedfiletypes == "*" || allowedfiletypeslist.Contains(Path.GetExtension(f).Replace(".", "").ToLower())) && f.Trim() != "")
                    {
                        var docpath = modSettings.GetXmlProperty("genxml/uploaddocfoldermappath").TrimEnd('\\') + "\\" + f;
                        var docurl = modSettings.GetXmlProperty("genxml/uploaddocfolder").TrimEnd('/') + "/" + Path.GetFileName(docpath);
                        if (!alreadyaddedlist.Contains(docpath))
                        {
                            AddNewDoc(Convert.ToInt32(itemid), docurl, docpath);
                            alreadyaddedlist.Add(docurl);
                        }
                    }
                }
                LocalUtils.ClearRazorCache(modSettings.ModuleId.ToString(""));
            }
            return "";
        }

        private String DeleteSelectedDocs(NBrightInfo ajaxInfo)
        {
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
            var selecteddocs = ajaxInfo.GetXmlProperty("genxml/hidden/selectedfiles");
            var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
            var modSettings = LocalUtils.GetSettings(moduleid);

            if (Utils.IsNumeric(itemid))
            {
                var flist = selecteddocs.Split(',');
                foreach (var f in flist)
                {
                    if (f != "")
                    {
                        var docpath = modSettings.GetXmlProperty("genxml/uploaddocfoldermappath").TrimEnd('\\') + "\\" + f;
                        if (File.Exists(docpath)) File.Delete(docpath);
                    }
                }
            }
            LocalUtils.ClearRazorCache(modSettings.ModuleId.ToString(""));
            return "";
        }

        private void UpdateDoc(String docmappath, String itemid, NBrightInfo modSettings)
        {
            if (Utils.IsNumeric(itemid))
            {
                if (File.Exists(docmappath))
                {
                    // don;t update record with uploaded docs
                    //var docurl = modSettings.GetXmlProperty("genxml/uploaddocfolder").TrimEnd('/') + "/" + Path.GetFileName(docmappath);
                    //var replacedocs = (modSettings.GetXmlPropertyBool("genxml/checkbox/replacefiles") || modSettings.GetXmlPropertyBool("genxml/hidden/replacefiles"));
                    //if (replacedocs)
                    //{
                    //    var objCtrl = new NBrightDataController();
                    //    var dataRecord = objCtrl.Get(Convert.ToInt32(itemid));
                    //    if (dataRecord != null)
                    //    {
                    //        dataRecord.RemoveXmlNode("genxml/docs");
                    //        objCtrl.Update(dataRecord);
                    //    }
                    //}
                    //AddNewDoc(Convert.ToInt32(itemid), docurl, docmappath);
                }
                LocalUtils.ClearRazorCache(modSettings.ModuleId.ToString(""));
            }
        }

        private void AddNewDoc(int itemId, String docurl, String docpath)
        {
            var objCtrl = new NBrightDataController();
            var dataRecord = objCtrl.Get(itemId);
            if (dataRecord != null)
            {
                var f = Path.GetFileName(docpath);
                var r = Path.GetFileNameWithoutExtension(docpath);
                var strXml = "<genxml><docs><genxml><hidden><ref>" + r.Replace(" ", "-") + "</ref><filename>" + f + "</filename><folderfilename>" + docpath.Replace(PortalSettings.Current.HomeDirectoryMapPath,"") + "</folderfilename><docpath>" + docpath + "</docpath><docurl>" + docurl + "</docurl></hidden></genxml></docs></genxml>";
                if (dataRecord.XMLDoc.SelectSingleNode("genxml/docs") == null)
                {
                    dataRecord.AddXmlNode(strXml, "genxml/docs", "genxml");
                }
                else
                {
                    dataRecord.AddXmlNode(strXml, "genxml/docs/genxml", "genxml/docs");
                }
                objCtrl.Update(dataRecord);
            }
        }

        private void UpdateDownloadCount(int itemid, String fileindex, int amount = 1)
        {
            var objCtrl = new NBrightDataController();
            var dataRecord = objCtrl.Get(itemid);
            if (dataRecord != null)
            {
                var amt = dataRecord.GetXmlPropertyDouble("genxml/docs/genxml[" + fileindex + "]/hidden/downloadcount");
                amt = amt + amount;
                dataRecord.SetXmlProperty("genxml/docs/genxml[" + fileindex + "]/hidden/downloadcount", amt.ToString("######"));
                objCtrl.Update(dataRecord);
            }
        }

        #endregion

        #region "file actions"

        private String AddSelectedFiles(HttpContext context)
        {
            var ajaxInfo = LocalUtils.GetAjaxFields(context);
            var uploadtype = ajaxInfo.GetXmlProperty("genxml/hidden/uploadtype");

            if (uploadtype == "image") return AddSelectedImages(ajaxInfo);
            return AddSelectedDocs(ajaxInfo);
        }

        private String ReplaceSelectedFiles(HttpContext context)
        {
            var ajaxInfo = LocalUtils.GetAjaxFields(context);
            var uploadtype = ajaxInfo.GetXmlProperty("genxml/hidden/uploadtype");

            if (uploadtype == "image") return ReplaceSelectedImages(ajaxInfo);
            return ReplaceSelectedDocs(ajaxInfo);
        }

        private String DeleteSelectedFiles(HttpContext context)
        {
            var ajaxInfo = LocalUtils.GetAjaxFields(context);
            var uploadtype = ajaxInfo.GetXmlProperty("genxml/hidden/uploadtype");

            if (uploadtype == "image") return DeleteSelectedImages(ajaxInfo);
            return DeleteSelectedDocs(ajaxInfo);
        }

        private String GetFiles(HttpContext context, bool clearCache = false)
        {
            var ajaxInfo = LocalUtils.GetAjaxFields(context);
            var uploadtype = ajaxInfo.GetXmlProperty("genxml/hidden/uploadtype");

            if (uploadtype == "image") return GetImages(ajaxInfo, clearCache);
            return GetDocs(ajaxInfo, clearCache);
        }

        private String GetFolderFiles(HttpContext context, bool clearCache = false)
        {
            var ajaxInfo = LocalUtils.GetAjaxFields(context);
            var uploadtype = ajaxInfo.GetXmlProperty("genxml/hidden/uploadtype");

            if (uploadtype == "image") return GetFolderImages(ajaxInfo, clearCache);
            return GetFolderDocs(ajaxInfo, clearCache);
        }

        #endregion

        #region "fileupload"

        private string FileUpload(HttpContext context, String moduleid)
        {
            try
            {

                var strOut = "";
                switch (context.Request.HttpMethod)
                {
                    case "HEAD":
                    case "GET":
                        break;
                    case "POST":
                    case "PUT":
                        strOut = UploadFile(context, moduleid);
                        break;
                    case "DELETE":
                        break;
                    case "OPTIONS":
                        break;

                    default:
                        context.Response.ClearHeaders();
                        context.Response.StatusCode = 405;
                        break;
                }

                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        // Upload file to the server
        private String UploadFile(HttpContext context, String moduleid)
        {
            return UploadWholeFile(context, moduleid);
        }

        // Upload entire file
        private String UploadWholeFile(HttpContext context, String moduleid)
        {
            var modSettings = LocalUtils.GetSettings(moduleid);
            for (int i = 0; i < context.Request.Files.Count; i++)
            {
                var file = context.Request.Files[i];
                var uploadfolder = modSettings.GetXmlProperty("genxml/uploaddocfoldermappath");
                if (ImgUtils.IsImageFile(Path.GetExtension(file.FileName))) uploadfolder = modSettings.GetXmlProperty("genxml/tempfoldermappath");
                var fullfilename = uploadfolder.TrimEnd('\\') + "\\" + file.FileName;
                if (File.Exists(fullfilename)) File.Delete(fullfilename);
                file.SaveAs(fullfilename);

                if (ImgUtils.IsImageFile(Path.GetExtension(fullfilename)))
                    UpdateImage(fullfilename, _itemid, modSettings);
                else
                    UpdateDoc(fullfilename, _itemid, modSettings);
            }
            return "";
        }



        #endregion

        


    }
}