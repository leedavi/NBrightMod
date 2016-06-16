using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Web.Management;
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
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.WebControls;
using DotNetNuke.Entities.Modules;

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
        private String _resxdatadefault = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<root>\r\n<data name=\"text.Text\" xml:space=\"preserve\"><value>Text</value></data>\r\n</root>";

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

            // Ajax can break context with DNN, so reset the context language to match the client.
            // NOTE: "genxml/hidden/lang" should be set in the template for langauge to work OK.
            SetContextLangauge(context);

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture(_lang);

            #endregion

            #endregion

            #region "Do processing of command"

            strOut = "ERROR!! - No Security rights for current user!";
            switch (paramCmd)
            {
                case "test":
                    strOut = "<root>" + UserController.Instance.GetCurrentUserInfo().Username + "</root>";
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
                    if (LocalUtils.CheckRights())
                    {
                        var zipfile = DoThemeExport(context);
                        strOut = "<a href='/DesktopModules/NBright/NBrightMod/XmlConnector.ashx?cmd=downloadfile&filename=/NBrightTemp/" + Path.GetFileName(zipfile) + "'>Download Theme</a>";
                    }
                    break;
                case "importtheme":
                    if (LocalUtils.CheckRights())
                    {
                        var fname1 = FileUpload(context, moduleid,true);
                        strOut = DoThemeImport(fname1);
                        LocalUtils.ClearRazorCache(moduleid);
                    }
                    break;
                case "downloadfile":
                    var fileindex = Utils.RequestQueryStringParam(context, "fileindex");
                    var itemid = Utils.RequestQueryStringParam(context, "itemid");
                    var filename = Utils.RequestQueryStringParam(context, "filename");
                    if (Utils.IsNumeric(itemid) && Utils.IsNumeric(fileindex))
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
                        if (filename != "")
                        {
                            var fpath = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\" + filename;
                            var downloadname = Utils.RequestQueryStringParam(context, "downloadname");
                            if (downloadname == "") downloadname = Path.GetFileName(fpath);
                            Utils.ForceDocDownload(fpath, downloadname, context.Response);
                        }
                    }
                    strOut = "File Download Error, filename: " + filename + ", itemid: " + itemid + ", fileindex: " + fileindex + " ";
                    break;
                case "sendemail":
                        strOut = SendEmail(context);
                    break;
                case "doportalvalidation":
                    if (LocalUtils.CheckRights())
                    {
                        LocalUtils.ResetValidationFlag();
                        LocalUtils.ValidateModuleData();
                        strOut = "Portal Validation Ativated";
                    }
                    break;
                case "createtemplate":
                    if (LocalUtils.CheckRights())
                    {
                        CreatePortalTemplates(context);
                        strOut = "OK";
                    }
                    break;
                case "makethemesys":
                    if (LocalUtils.CheckRights())
                    {
                        strOut = MoveThemeToSystem(context);
                    }
                    break;                    
                case "gettemplatemenu":
                    if (LocalUtils.CheckRights())
                    {
                        strOut = GetTemplateMenu(context);
                    }
                    break;
                case "savetemplatedata":
                    if (LocalUtils.CheckRights())
                    {
                        strOut = SaveTemplateMenu(context);
                    }
                    break;
                case "deleteportalresx":
                    if (LocalUtils.CheckRights())
                    {
                        strOut = DeletePortalResx(context);
                    }
                    break;
                case "deleteportaltempl":
                    if (LocalUtils.CheckRights())
                    {
                        strOut = DeletePortalTemplate(context);
                    }
                    break;
                case "deletemoduletempl":
                    if (LocalUtils.CheckRights())
                    {
                        strOut = DeleteModuleTemplate(context);
                    }
                    break;
                case "deletetheme":
                    if (LocalUtils.CheckRights())
                    {
                        strOut = DeleteTheme(context);
                    }
                    break;
                case "clonemodule":
                    if (LocalUtils.CheckRights())
                    {
                        strOut = CloneModule(context);
                    }
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

        private void SetContextLangauge(HttpContext context)
        {
            var ajaxInfo = LocalUtils.GetAjaxFields(context);
            SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.
        }

        private void SetContextLangauge(NBrightInfo ajaxInfo = null)
        {
            // NOTE: "genxml/hidden/lang" should be set in the template for langauge to work OK.
            // set langauge if we have it passed.
            if (ajaxInfo == null) ajaxInfo = new NBrightInfo(true);
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/currentlang");
            if (lang == "") lang = Utils.RequestParam(HttpContext.Current,"langauge"); // fallbacl
            if (lang == "") lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang"); // fallbacl
            if (lang == "") lang = Utils.GetCurrentCulture(); // fallback, but very often en-US on ajax call
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
                            nbi.SetXmlProperty("genxml/hidden/moduleiddatasource",satnbi.ModuleId.ToString());
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

                    nbi = LocalUtils.CreateRequiredUploadFolders(nbi);

                    LocalUtils.UpdateSettings(nbi);

                    LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));

                    LocalUtils.ClearRazorSateliteCache(nbi.ModuleId.ToString(""));


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

                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                if (Utils.IsNumeric(moduleid) && Convert.ToInt32(moduleid) > 0)
                {
                    // get DB record
                    var nbi = LocalUtils.GetSettings(moduleid,false);
                    if (nbi.ItemID > 0) objCtrl.Delete(nbi.ItemID);

                    LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));

                    LocalUtils.ClearRazorSateliteCache(nbi.ModuleId.ToString(""));


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
                        var nolang = false;
                        foreach (var nbi in l)
                        {
                            var lnode = nbi.XMLDoc.SelectSingleNode("genxml/lang");
                            if (lnode == null)
                            {
                                LocalUtils.CreateLangaugeDataRecord(nbi.ItemID, Convert.ToInt32(moduleid), editlang);
                                nolang = true;
                            }
                        }
                        if (nolang) // reload if we found invalid data list
                        {
                            l = objCtrl.GetList(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), "NBrightModDATA", "", orderby, returnlimit, 0, 0, 0, editlang);
                        }

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

        private String GetTemplateMenu(HttpContext context)
        {
            #region "init params from ajax"
            var strOut = "";
            //get uploaded params
            var ajaxInfo = LocalUtils.GetAjaxFields(context);

            var themefolder = ajaxInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
            var newname = ajaxInfo.GetXmlProperty("genxml/textbox/newname");
            var updatetype = ajaxInfo.GetXmlProperty("genxml/hidden/updatetype");
            if (updatetype == "new") themefolder = newname; // if we are creating a new theme, use the new name to save.
            var razortemplname = "config.edittheme.cshtml";
            var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
            var templfilename = ajaxInfo.GetXmlProperty("genxml/hidden/templfilename");
            var resxfilename = ajaxInfo.GetXmlProperty("genxml/hidden/resxfilename");
            var currentedittab = ajaxInfo.GetXmlProperty("genxml/hidden/currentedittab");
            var modulelevel = ajaxInfo.GetXmlPropertyBool("genxml/hidden/modulelevel");
            var moduleid = ajaxInfo.GetXmlPropertyInt("genxml/hidden/moduleid");
            var moduleref = "";
            var modInfo = new NBrightInfo();
            var templData = new NBrightInfo(true);

            // for module level template we need to add the modref to the start of the template
            if (Utils.IsNumeric(moduleid))
            {
                var objCtrl = new NBrightDataController();

                // assign module themefolder.
                modInfo = objCtrl.GetByType(PortalSettings.Current.PortalId, moduleid, "SETTINGS");
                if (modInfo != null)
                {
                    themefolder = modInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
                    moduleref = modInfo.GetXmlProperty("genxml/hidden/modref");
                }
                else
                {
                    modInfo = new NBrightInfo();
                }
            }

            if (modulelevel)
            {
                templfilename = moduleref + templfilename; // module level templates prefixed with moduleref
                templData.SetXmlProperty("genxml/modulelevel","True");
            }
            else
            {
                modInfo = new NBrightInfo(); // we're editing portal level, clear module info so we pickup only portal level templates.
                templData.SetXmlProperty("genxml/modulelevel", "False");
            }

            var fulltemplfilename = themefolder + "." + ajaxInfo.GetXmlProperty("genxml/hidden/templfilename");

            #endregion

            var razorTempl2 = "";
            if (templfilename.EndsWith(".cshtml"))
            {
                razorTempl2 = LocalUtils.GetTemplateData(fulltemplfilename, editlang, modInfo.ToDictionary());
            }
            else
            {
                var sourceroot = "";
                var sourceportal = "";
                if (templfilename.EndsWith(".css"))
                {
                    sourceportal = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\css";
                    sourceroot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/css");
                }
                if (templfilename.EndsWith(".js"))
                {
                    sourceportal = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\js";
                    sourceroot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/js");
                }
                if (templfilename.EndsWith(".resx"))
                {
                    sourceportal = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\resx";
                    sourceroot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/resx");
                }
                razorTempl2 = Utils.ReadFile(sourceportal + "\\" + templfilename);
                if (razorTempl2 == "")
                {
                    razorTempl2 = Utils.ReadFile(sourceroot + "\\" + templfilename);
                }
            }

            // get resxdata for theme.ascx.**-**.resx
            var sourceresx = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\resx";

            if (resxfilename == "" || !resxfilename.StartsWith("theme."))
            {
                resxfilename = "theme.ascx." + editlang + ".resx";
                if (editlang == "none" || editlang == "")
                {
                    resxfilename = "theme.ascx.resx";
                }
            }
            var isportalresxlevel = "1";
            var resxdata = Utils.ReadFile(sourceresx + "\\" + resxfilename);
            if (resxdata == "")
            {
                // set simple base if no file exists
                resxdata = _resxdatadefault;
                isportalresxlevel = "0";
            }
            templData.Lang = editlang;
            templData.SetXmlProperty("genxml/templtext", razorTempl2);
            templData.SetXmlProperty("genxml/resxdata", resxdata);
            templData.SetXmlProperty("genxml/templfilename", templfilename);
            var displayname = templfilename;
            if (moduleref != "") displayname = templfilename.Replace(moduleref, "");
            templData.SetXmlProperty("genxml/displayfilename", displayname);
            templData.SetXmlProperty("genxml/resxfilename", resxfilename);
            templData.SetXmlProperty("genxml/resxportal", isportalresxlevel);
            templData.SetXmlProperty("genxml/hidden/currentedittab", currentedittab);

            // get template files
            templData.RemoveXmlNode("genxml/files");
            templData.AddSingleNode("files", "", "genxml");
            templData.RemoveXmlNode("genxml/portalfiles");
            templData.AddSingleNode("portalfiles", "", "genxml");
            templData.RemoveXmlNode("genxml/modulefiles");
            templData.AddSingleNode("modulefiles", "", "genxml");

            templData = GetListOfTemplateFiles(templData, themefolder, "default", moduleref);
            templData = GetListOfTemplateFiles(templData, themefolder, "css", moduleref);
            templData = GetListOfTemplateFiles(templData, themefolder, "js", moduleref);
            templData = GetListOfTemplateFiles(templData, themefolder, "resx", moduleref);

            strOut = LocalUtils.RazorTemplRender(razortemplname, "-1", "", templData, _lang,true);

            return strOut;
        }

        private NBrightInfo GetListOfTemplateFiles(NBrightInfo templData,String themefolder,String themesubfolder,String modref)
        {
            var sourceRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/" + themesubfolder);
            if (!System.IO.Directory.Exists(sourceRoot)) sourceRoot = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\" + themesubfolder;

            if (System.IO.Directory.Exists(sourceRoot))
            {
                string[] files = System.IO.Directory.GetFiles(sourceRoot);
                foreach (string s in files)
                {
                    templData.AddSingleNode("file", System.IO.Path.GetFileName(s), "genxml/files");
                    var portalpath = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\" + themesubfolder + "\\" + Path.GetFileName(s);
                    if (File.Exists(portalpath))
                    {
                        templData.AddSingleNode("file", System.IO.Path.GetFileName(s), "genxml/portalfiles");
                    }
                    var modulepath = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\" + themesubfolder + "\\" + modref + Path.GetFileName(s);
                    if (File.Exists(modulepath))
                    {
                        templData.AddSingleNode("file", modref + System.IO.Path.GetFileName(s), "genxml/modulefiles"); // real file name
                        templData.AddSingleNode("file", System.IO.Path.GetFileName(s), "genxml/modulefiles"); // standard file name, to use for testing if file is module level.
                    }
                }
            }
            return templData;
        }

        private String SaveTemplateMenu(HttpContext context)
        {
            #region "init params from ajax"
            //get uploaded params
            var ajaxInfo = LocalUtils.GetAjaxFields(context);

            var themefolder = ajaxInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
            var newname = ajaxInfo.GetXmlProperty("genxml/textbox/newname");
            var updatetype = ajaxInfo.GetXmlProperty("genxml/hidden/updatetype");
            if (updatetype == "new") themefolder = newname; // if we are creating a new theme, use the new name to save.
            var templfilename = ajaxInfo.GetXmlProperty("genxml/hidden/templfilename");
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
            var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
            var fldrlang = lang;
            if (fldrlang == "") fldrlang = "default";
            var resxfilename = ajaxInfo.GetXmlProperty("genxml/hidden/resxfilename");
            var simpletext = ajaxInfo.GetXmlProperty("genxml/textbox/simpletext");
            var modulelevel = ajaxInfo.GetXmlPropertyBool("genxml/hidden/modulelevel");
            var moduleid = ajaxInfo.GetXmlPropertyInt("genxml/hidden/moduleid");
            var moduleref = "";
            var modInfo = new NBrightInfo();

            // for module level template we need to add the modref to the start of the template
            if (Utils.IsNumeric(moduleid))
            {
                var objCtrl = new NBrightDataController();

                // assign module themefolder.
                modInfo = objCtrl.GetByType(PortalSettings.Current.PortalId, moduleid, "SETTINGS");
                if (modInfo != null)
                {
                    themefolder = modInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
                    moduleref = modInfo.GetXmlProperty("genxml/hidden/modref");
                }
                else
                {
                    modInfo = new NBrightInfo();
                }
            }
            if (modulelevel)
            {
                templfilename = moduleref + templfilename; // module level templates prefixed with moduleref
            }
            else
            {
                modInfo = new NBrightInfo(); // we're editing portal level, clear module info so we pickup only portal level templates.
            }

            var fulltemplfilename = themefolder + "." + ajaxInfo.GetXmlProperty("genxml/hidden/templfilename");

            #endregion

            if (simpletext != "")
            {
                var razorTempl2 = "";
                if (templfilename.EndsWith(".cshtml"))
                {
                    razorTempl2 = LocalUtils.GetTemplateData(fulltemplfilename, lang, modInfo.ToDictionary());
                }
                else
                {
                    var sourceroot = "";
                    if (templfilename.EndsWith(".css"))
                    {
                        sourceroot = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\css";
                    }
                    if (templfilename.EndsWith(".js"))
                    {
                        sourceroot = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\js";
                    }
                    if (templfilename.EndsWith(".resx"))
                    {
                        sourceroot = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\resx";
                    }
                    razorTempl2 = Utils.ReadFile(sourceroot + "\\" + templfilename);
                    if (razorTempl2 == "")
                    {
                        // get orginal
                        if (templfilename.EndsWith(".css"))
                        {
                            sourceroot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/css");
                        }
                        if (templfilename.EndsWith(".js"))
                        {
                            sourceroot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/js");
                        }
                        if (templfilename.EndsWith(".resx"))
                        {
                            sourceroot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/resx");
                        }
                        razorTempl2 = Utils.ReadFile(sourceroot + "\\" + templfilename);
                    }

                }

                if (LocalUtils.RemoveWhitespace(razorTempl2) != LocalUtils.RemoveWhitespace(simpletext)) // only save if it's different. 
                {
                    var fldrDefault = "";
                    if (templfilename.EndsWith(".cshtml"))
                    {
                        fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\" + fldrlang;
                    }
                    else
                    {
                        if (templfilename.EndsWith(".css"))
                        {
                            fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\css";
                        }
                        if (templfilename.EndsWith(".js"))
                        {
                            fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\js";
                        }
                        if (templfilename.EndsWith(".resx"))
                        {
                            fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\resx";
                        }
                    }
                    if (fldrDefault != "")
                    {
                        Utils.CreateFolder(fldrDefault);
                        File.WriteAllText(fldrDefault + "\\" + templfilename, simpletext);
                    }
                }

                var sourceresx = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\resx";
                var existingresxdata = Utils.ReadFile(sourceresx + "\\" + resxfilename);
                var resxdata = ajaxInfo.GetXmlProperty("genxml/textbox/resxdata");
                var resxdatatest = LocalUtils.RemoveWhitespace(resxdata);

                if (resxdatatest != LocalUtils.RemoveWhitespace(_resxdatadefault) && resxdatatest != LocalUtils.RemoveWhitespace(existingresxdata))
                {
                    Utils.CreateFolder(sourceresx);
                    if (lang == "")
                    {
                            File.WriteAllText(sourceresx + "\\" + "\\theme.ascx.resx", resxdata);
                    }
                    else
                    {
                        File.WriteAllText(sourceresx + "\\" + "\\theme.ascx." + lang + ".resx", resxdata);
                    }
                }

                LocalUtils.ClearModuleCacheByTheme(themefolder);

            }
            return "OK";
        }

        private String DeleteModuleTemplate(HttpContext context)
        {
            //get uploaded params
            var ajaxInfo = LocalUtils.GetAjaxFields(context);

            var themefolder = ajaxInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
            var templfilename = ajaxInfo.GetXmlProperty("genxml/hidden/templfilename");
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
            var fldrlang = lang;
            if (fldrlang == "") fldrlang = "default";

            var moduleid = ajaxInfo.GetXmlPropertyInt("genxml/hidden/moduleid");
            var moduleref = "";

            // for module level template we need to add the modref to the start of the template
            if (Utils.IsNumeric(moduleid))
            {
                var objCtrl = new NBrightDataController();

                // assign module themefolder.
                var modsettings = objCtrl.GetByType(PortalSettings.Current.PortalId, moduleid, "SETTINGS");
                if (modsettings != null)
                {
                    themefolder = modsettings.GetXmlProperty("genxml/dropdownlist/themefolder");
                    moduleref = modsettings.GetXmlProperty("genxml/hidden/modref");
                    templfilename = moduleref + templfilename;
                }



                var fldrDefault = "";
                if (templfilename.EndsWith(".cshtml"))
                {
                    fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\" + fldrlang;
                }
                else
                {
                    if (templfilename.EndsWith(".css"))
                    {
                        fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\css";
                    }
                    if (templfilename.EndsWith(".js"))
                    {
                        fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\js";
                    }
                    if (templfilename.EndsWith(".resx"))
                    {
                        fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\resx";
                    }
                }
                if (fldrDefault != "")
                {
                    if (File.Exists(fldrDefault + "\\" + templfilename))
                    {
                        File.Delete(fldrDefault + "\\" + templfilename);
                        LocalUtils.ClearRazorCache(moduleid.ToString(""));
                        LocalUtils.ClearRazorSateliteCache(moduleid.ToString(""));
                    }
                }

            }


            return "OK";
        }

        private String DeletePortalTemplate(HttpContext context)
        {
            //get uploaded params
            var ajaxInfo = LocalUtils.GetAjaxFields(context);

            var themefolder = ajaxInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
            var templfilename = ajaxInfo.GetXmlProperty("genxml/hidden/templfilename");
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
            var fldrlang = lang;
            if (fldrlang == "") fldrlang = "default";

            var fldrDefault = "";
            if (templfilename.EndsWith(".cshtml"))
            {
                fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\" + fldrlang;
            }
            else
            {
                if (templfilename.EndsWith(".css"))
                {
                    fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\css";
                }
                if (templfilename.EndsWith(".js"))
                {
                    fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\js";
                }
                if (templfilename.EndsWith(".resx"))
                {
                    fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\resx";
                }
            }
            if (fldrDefault != "")
            {
                if (File.Exists(fldrDefault + "\\" + templfilename))
                {
                    File.Delete(fldrDefault + "\\" + templfilename);
                    LocalUtils.ClearModuleCacheByTheme(themefolder); 
                }
            }


            return "OK";
        }

        private String DeletePortalResx(HttpContext context)
        {
            //get uploaded params
            var ajaxInfo = LocalUtils.GetAjaxFields(context);

            var themefolder = ajaxInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
            var templfilename = ajaxInfo.GetXmlProperty("genxml/hidden/templfilename");
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
            var fldrlang = lang;
            if (fldrlang == "") fldrlang = "default";
            var resxfilename = ajaxInfo.GetXmlProperty("genxml/hidden/resxfilename");

            var moduleid = ajaxInfo.GetXmlPropertyInt("genxml/hidden/moduleid");

            if (themefolder == "" && Utils.IsNumeric(moduleid)) // themefolder is blank for modulelevel editing
            {
                var objCtrl = new NBrightDataController();
                var modsettings = objCtrl.GetByType(PortalSettings.Current.PortalId, moduleid, "SETTINGS");
                if (modsettings != null)
                {
                    themefolder = modsettings.GetXmlProperty("genxml/dropdownlist/themefolder");
                }
            }


            var sourceresx = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\resx";

            if (lang == "")
            {
                if (File.Exists(sourceresx + "\\" + "\\theme.ascx.resx"))
                {
                    File.Delete(sourceresx + "\\" + "\\theme.ascx.resx");
                    LocalUtils.ClearModuleCacheByTheme(themefolder);
                }
            }
            else
            {
                if (File.Exists(sourceresx + "\\" + "\\theme.ascx." + lang + ".resx"))
                {
                    File.Delete(sourceresx + "\\" + "\\theme.ascx." + lang + ".resx");
                    LocalUtils.ClearModuleCacheByTheme(themefolder);
                }
            }

            return "OK";
        }

        private String DeleteTheme(HttpContext context)
        {
            try
            {
                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                var theme = ajaxInfo.GetXmlProperty("genxml/dropdownlist/portalthemefolder");
                var updatetype = ajaxInfo.GetXmlProperty("genxml/hidden/updatetype");
                var exportname = ajaxInfo.GetXmlProperty("genxml/textbox/newname");
                if (exportname == "") exportname = theme;

                if (updatetype == "del" && theme != "")
                {
                    var portalthemeFolderName = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + theme;

                    if (Directory.Exists(portalthemeFolderName))
                    {
                        DirectoryInfo di = new DirectoryInfo(portalthemeFolderName);

                        foreach (FileInfo file in di.GetFiles())
                        {
                            file.Delete();
                        }
                        foreach (DirectoryInfo dir in di.GetDirectories())
                        {
                            dir.Delete(true);
                        }

                        Utils.DeleteFolder(portalthemeFolderName);
                        LocalUtils.ClearModuleCacheByTheme(theme);
                    }

                    return "Theme Removed";
                }
                return "";
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.ToString();
            }

        }

        private String CloneModule(HttpContext context)
        {
            try
            {
                var objmodules = new ModuleController();

                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                var clonelist = ajaxInfo.GetXmlProperty("genxml/hidden/clonelist");
                var moduleid = ajaxInfo.GetXmlPropertyInt("genxml/hidden/moduleid");
                var currenttabid = ajaxInfo.GetXmlPropertyInt("genxml/hidden/currenttabid");
                var tablist = clonelist.Split(',');

                // delete existing clones not selected.
                var tabList = DotNetNuke.Entities.Tabs.TabController.GetTabsBySortOrder(DotNetNuke.Entities.Portals.PortalSettings.Current.PortalId, Utils.GetCurrentCulture(), true);
                foreach  (var tabinfo in tabList)
                {
                    if (tabinfo.TabID != currenttabid)
                    {
                        ModuleInfo mi = objmodules.GetModule(moduleid, tabinfo.TabID);
                        if (mi != null && !tablist.Contains(tabinfo.TabID.ToString("")))
                        {
                            objmodules.DeleteTabModule(tabinfo.TabID, moduleid, false);
                        }
                    }
                }

                if (moduleid > 0)
                {
                    foreach (var tabId in tablist)
                    {
                        if (Utils.IsNumeric(tabId))
                        {
                            var existingmodule = objmodules.GetModule(moduleid, Convert.ToInt32(tabId));
                            if (existingmodule == null && currenttabid != Convert.ToInt32(tabId))
                            {
                                ModuleInfo fmi = objmodules.GetModule(moduleid);
                                ModuleInfo newModule = fmi.Clone();

                                newModule.UniqueId = Guid.NewGuid(); // Cloned Module requires a different uniqueID 
                                newModule.TabID = Convert.ToInt32(tabId);
                                objmodules.AddModule(newModule);
                            }

                        }
                    }


                    return "OK";
                }
                return "";
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.ToString();
            }

        }


        private String CreatePortalTemplates(HttpContext context)
        {
            try
            {
                var objCtrl = new NBrightDataController();

                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);

                var themefolder = ajaxInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
                var portalthemefolder = ajaxInfo.GetXmlProperty("genxml/dropdownlist/portalthemefolder");
                var newname = ajaxInfo.GetXmlProperty("genxml/textbox/newname");
                var updatetype = ajaxInfo.GetXmlProperty("genxml/hidden/updatetype");
                var modulelevel = ajaxInfo.GetXmlPropertyBool("genxml/hidden/modulelevel");
                var moduleid = ajaxInfo.GetXmlPropertyInt("genxml/hidden/moduleid");

                if (modulelevel && Utils.IsNumeric(moduleid))
                {
                    // assign module themefolder.
                    var modsettings = objCtrl.GetByType(PortalSettings.Current.PortalId, moduleid, "SETTINGS");
                    if (modsettings != null)
                    {
                        themefolder = modsettings.GetXmlProperty("genxml/dropdownlist/themefolder");
                    }
                }


                if (updatetype == "new" && newname != "" && themefolder != "")
                {
                    // create folders (if required)
                    var fldrRoot = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + newname;
                    Utils.CreateFolder(fldrRoot);
                    var fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + newname + "\\default";
                    Utils.CreateFolder(fldrDefault);
                    var fldrResx = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + newname + "\\resx";
                    Utils.CreateFolder(fldrResx);
                    var fldrJs = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + newname + "\\js";
                    Utils.CreateFolder(fldrJs);
                    var fldrCss = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + newname + "\\css";
                    Utils.CreateFolder(fldrCss);

                    var sourceRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/default");
                    CopyFileInFolder(sourceRoot, fldrDefault);
                    sourceRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/resx");
                    CopyFileInFolder(sourceRoot, fldrResx);
                    sourceRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/js");
                    CopyFileInFolder(sourceRoot, fldrJs);
                    sourceRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/css");
                    CopyFileInFolder(sourceRoot, fldrCss);
                }

                if (updatetype == "edit" && themefolder != "")
                {
                    // create folders (if required)
                    var fldrRoot = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder;
                    Utils.CreateFolder(fldrRoot);
                    var fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\default";
                    Utils.CreateFolder(fldrDefault);
                    var fldrResx = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\resx";
                    Utils.CreateFolder(fldrResx);
                    var fldrJs = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\js";
                    Utils.CreateFolder(fldrJs);
                    var fldrCss = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + themefolder + "\\css";
                    Utils.CreateFolder(fldrCss);
                }

                return "OK";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String MoveThemeToSystem(HttpContext context)
        {
            try
            {
                var strOut = "Error unable to Move Theme";

                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);

                var portalthemefolder = ajaxInfo.GetXmlProperty("genxml/dropdownlist/portalthemefolder");
                var updatetype = ajaxInfo.GetXmlProperty("genxml/hidden/updatetype");

                if (updatetype == "sys" && portalthemefolder != "")
                {
                    var targetRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + portalthemefolder);
                    //see if we already have a portal level 
                    if (Directory.Exists(targetRoot))
                    {
                        strOut = "ERROR: Theme already exists at system level, unable to Move Theme";
                    }
                    else
                    {
                        Utils.CreateFolder(targetRoot);
                        
                        var fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + portalthemefolder + "\\default";
                        var fldrResx = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + portalthemefolder + "\\resx";
                        var fldrJs = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + portalthemefolder + "\\js";
                        var fldrCss = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + portalthemefolder + "\\css";

                        targetRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + portalthemefolder + "/default");
                        Utils.CreateFolder(targetRoot);
                        CopyFileInFolder(fldrDefault, targetRoot, true);
                        targetRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + portalthemefolder + "/resx");
                        Utils.CreateFolder(targetRoot);
                        CopyFileInFolder(fldrResx,targetRoot, true);
                        targetRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + portalthemefolder + "/js");
                        Utils.CreateFolder(targetRoot);
                        CopyFileInFolder(fldrJs,targetRoot, true);
                        targetRoot = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + portalthemefolder + "/css");
                        Utils.CreateFolder(targetRoot);
                        CopyFileInFolder(fldrCss,targetRoot,true);

                        // delete theme directory if we have no files
                        var filecount = 0;
                        fldrDefault = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes\\" + portalthemefolder;
                        filecount += Directory.GetFiles(fldrDefault, "*.*", SearchOption.AllDirectories).Length;
                        if (filecount == 0)
                        {
                            Directory.Delete(fldrDefault, true);
                        }

                        strOut = "Theme Moved to System Level and available on ALL portals";
                    }

                }


                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }


        private String DoThemeExport(HttpContext context)
        {
            try
            {
                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);
                var theme = ajaxInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
                var updatetype = ajaxInfo.GetXmlProperty("genxml/hidden/updatetype");
                var exportname = ajaxInfo.GetXmlProperty("genxml/textbox/newname");
                if (exportname == "") exportname = theme;

                if (updatetype == "export" && theme != "")
                {
                    var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod");
                    var systhemeFolderName = controlMapPath + "\\Themes\\" + theme;
                    var portalthemeFolderName = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + theme;

                    // export from portal level, so save export.config
                    var exportconfig = new NBrightInfo(true);

                    var themeFolderName = systhemeFolderName;
                    if (Directory.Exists(portalthemeFolderName))
                    {
                        themeFolderName = portalthemeFolderName;
                        exportconfig.SetXmlProperty("genxml/portallevel", "true");
                    }
                    else
                    {
                        exportconfig.SetXmlProperty("genxml/portallevel", "false");
                    }

                    exportconfig.SetXmlProperty("genxml/portalmappath", PortalSettings.Current.HomeDirectoryMapPath);
                    exportconfig.SetXmlProperty("genxml/portalpath", PortalSettings.Current.HomeDirectory);
                    exportconfig.SetXmlProperty("genxml/systhemefoldername", systhemeFolderName);
                    exportconfig.SetXmlProperty("genxml/portalthemefoldername", portalthemeFolderName);
                    exportconfig.SetXmlProperty("genxml/portalrelfolder", PortalSettings.Current.HomeDirectory + "/NBrightMod/Themes/" +  theme);
                    exportconfig.SetXmlProperty("genxml/systemrelfolder", "/DesktopModules/NBright/NBrightMod/Themes/" + theme);
                    exportconfig.SetXmlProperty("genxml/themename", theme);

                    Utils.SaveFile(themeFolderName.TrimEnd('\\') + "\\export.config", exportconfig.XMLData);

                    Utils.CreateFolder(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp");
                    var zipFile = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp\\NBrightMod_Theme_" + exportname + ".zip";

                    DnnUtils.ZipFolder(themeFolderName, zipFile);

                    return zipFile;
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
                    var themeName = Path.GetFileName(zipFileMapPath).Replace("NBrightMod_Theme_", "").Replace(".zip", "");
                    if (!string.IsNullOrEmpty(themeName))
                    {
                        var themeFolderName = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + themeName;
                        if (!Directory.Exists(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod"))
                        {
                            Directory.CreateDirectory(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod");
                            Directory.CreateDirectory(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\");
                        }
                        DnnUtils.UnZip(zipFileMapPath, themeFolderName);
                        Utils.DeleteSysFile(zipFileMapPath);
                        return "";
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


        private void CopyFileInFolder(String sourcePath, String targetPath,Boolean move = false)
        {
            // To copy a folder's contents to a new location:
            // Create a new target folder, if necessary.
            if (!System.IO.Directory.Exists(targetPath))
            {
                System.IO.Directory.CreateDirectory(targetPath);
            }

            // To copy all the files in one directory to another directory.
            // Get the files in the source folder. (To recursively iterate through
            // all subfolders under the current directory, see
            // "How to: Iterate Through a Directory Tree.")
            // Note: Check for target path was performed previously
            //       in this code example.
            if (System.IO.Directory.Exists(sourcePath))
            {
                string[] files = System.IO.Directory.GetFiles(sourcePath);

                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    var fileName = System.IO.Path.GetFileName(s);
                    var destFile = System.IO.Path.Combine(targetPath, fileName);
                    System.IO.File.Copy(s, destFile, true);
                    if (move)
                    {
                        File.Delete(s);
                    }
                }
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
                    LocalUtils.ClearRazorSateliteCache(moduleid);

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

        private String SendEmail(HttpContext context)
        {
            try
            {
                //get uploaded params
                var ajaxInfo = LocalUtils.GetAjaxFields(context);

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
                        nbi.PortalId = PortalSettings.Current.PortalId;
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
                                DotNetNuke.Services.Mail.Mail.SendMail(clientemail.Trim(), email.Trim(), "", emailsubject, emailbody, "", "HTML", "", "", "", "");
                                strOut = emailreturnmsg;
                            }
                        }

                        // Delete Extra unrequired email data
                        if (Utils.IsEmail(emailfrom.Trim()))
                        {
                            if (Utils.IsNumeric(emailstosave) && Convert.ToInt32(emailstosave) > 0)
                            {
                                var l = objCtrl.GetList(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), "NBrightModDATA", "", " order by NB1.ModifiedDate DESC");
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
                        LocalUtils.ClearRazorCache(moduleid);
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
                var gotfiles = dirInfo.GetFiles();
                FileInfo[] files = Array.FindAll(gotfiles, f => allowedExtensions.Contains(f.Extension));

                var imgl = new List<NBrightInfo>();

                foreach (var f in files)
                {
                    var fullname = f.FullName; // don't use file object directly, it locks the file on servr, but not on dev machine.???? I presume it's something the Path.GetFileName does?? 
                    var name = f.Name;
                    var ext = f.Extension;
                    var imageurl = modSettings.GetXmlProperty("genxml/uploadfolder").TrimEnd('/') + "/" + Path.GetFileName(fullname);
                    var nbi = new NBrightInfo(true);
                    nbi.SetXmlProperty("genxml/hidden/filename", name);
                    nbi.SetXmlProperty("genxml/hidden/name", name.Replace(ext, ""));
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
                    var fullname = f.FullName; // don't use file object directly, it locks the file on servr, but not on dev machine.???? I presume it's something the Path.GetFileName does?? 
                    var name = f.Name;
                    var docurl = modSettings.GetXmlProperty("genxml/uploaddocfolder").TrimEnd('/') + "/" + Path.GetFileName(fullname);
                    var docref = Path.GetFileNameWithoutExtension(name).Replace(" ", "-");
                    var nbi = new NBrightInfo(true);
                    nbi.SetXmlProperty("genxml/hidden/filename", name);
                    nbi.SetXmlProperty("genxml/hidden/name", name.Replace(f.Extension, ""));
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

        private string FileUpload(HttpContext context, String moduleid, Boolean passbackfilename = false)
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
                        strOut = UploadFile(context, moduleid, passbackfilename);
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
        private String UploadFile(HttpContext context, String moduleid, Boolean passbackfilename = false)
        {
            return UploadWholeFile(context, moduleid, passbackfilename);
        }

        // Upload entire file
        private String UploadWholeFile(HttpContext context, String moduleid, Boolean passbackfilename = false)
        {
            var modSettings = LocalUtils.GetSettings(moduleid);
            for (int i = 0; i < context.Request.Files.Count; i++)
            {
                var file = context.Request.Files[i];
                var uploadfolder = modSettings.GetXmlProperty("genxml/uploaddocfoldermappath");
                if (ImgUtils.IsImageFile(Path.GetExtension(file.FileName))) uploadfolder = modSettings.GetXmlProperty("genxml/tempfoldermappath");
                if (uploadfolder == "") uploadfolder = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightTemp";
                Utils.CreateFolder(uploadfolder);
                var fullfilename = uploadfolder.TrimEnd('\\') + "\\" + file.FileName;
                if (File.Exists(fullfilename)) File.Delete(fullfilename);
                file.SaveAs(fullfilename);

                if (ImgUtils.IsImageFile(Path.GetExtension(fullfilename)))
                    UpdateImage(fullfilename, _itemid, modSettings);
                else
                    UpdateDoc(fullfilename, _itemid, modSettings);
                if (passbackfilename) return fullfilename;
            }
            return "";
        }



        #endregion

        


    }
}