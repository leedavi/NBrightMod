


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.Caching;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.UI;
using System.Xml;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.UI.UserControls;
using NBrightCore.common;
using NBrightCore.render;
using NBrightCore.TemplateEngine;
using NBrightDNN;
using NBrightDNN.render;
using NBrightMod.render;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using System.Text;

namespace NBrightMod.common
{

    public static class LocalUtils
    {


        #region "functions"

        public static String AddNew(String moduleid)
        {
            if (!Utils.IsNumeric(moduleid)) moduleid = "-1";

            var objCtrl = new NBrightDataController();
            var nbi = new NBrightInfo(true);
            nbi.PortalId = PortalSettings.Current.PortalId;
            nbi.TypeCode = "NBrightModDATA";
            nbi.ModuleId = Convert.ToInt32(moduleid);
            nbi.ItemID = -1;
            nbi.GUIDKey = "";
            var itemId = objCtrl.Update(nbi);
            nbi.ItemID = itemId;

            foreach (var lang in DnnUtils.GetCultureCodeList(PortalSettings.Current.PortalId))
            {
                var nbi2 = CreateLangaugeDataRecord(itemId, Convert.ToInt32(moduleid), lang);
            }

            LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));

            return nbi.ItemID.ToString("");
        }

        public static NBrightInfo CreateLangaugeDataRecord(int parentItemId, int moduleid,String lang)
        {
            var objCtrl = new NBrightDataController();
            var nbi2 = new NBrightInfo(true);
            nbi2.PortalId = PortalSettings.Current.PortalId;
            nbi2.TypeCode = "NBrightModDATALANG";
            nbi2.ModuleId = moduleid;
            nbi2.ItemID = -1;
            nbi2.Lang = lang;
            nbi2.ParentItemId = parentItemId;
            nbi2.GUIDKey = "";
            nbi2.ItemID = objCtrl.Update(nbi2);
            return nbi2;
        }

        public static String GetTemplateData(String templatename, String lang, Dictionary<String, String> settings = null)
        {
            var themeFolder = "config";
            if (settings != null && settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
            var parseTemplName = templatename.Split('.');
            if (parseTemplName.Count() >= 3)
            {
                themeFolder = parseTemplName[0];
                for (int i = 1; i < parseTemplName.Length; i++)
                {
                    templatename = parseTemplName[1] + "." + parseTemplName[2];
                }
            }

            var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod");
            var templCtrl = new NBrightCore.TemplateEngine.TemplateGetter(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod", controlMapPath, "Themes\\" + themeFolder, "");
            var templ = "";
            // get module specific template
            if (settings != null && settings.ContainsKey("modref")) templ = templCtrl.GetTemplateData(settings["modref"] + templatename, lang);
            if (templ == "")
            {
                // get standard module template
                templ = templCtrl.GetTemplateData(templatename, lang);
            }
            return templ;
        }

        public static NBrightInfo GetAjaxFields(HttpContext context, bool ignoresecurityfilter = false, bool filterlinks = false)
        {
            var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
            return GetAjaxFields(strIn, "", ignoresecurityfilter, filterlinks);
        }

        public static NBrightInfo GetAjaxFields(String ajaxData,String mergeWithXml = "",bool ignoresecurityfilter = false, bool filterlinks = false)
        {
            var xmlData = GenXmlFunctions.GetGenXmlByAjax(ajaxData, mergeWithXml,"genxml", ignoresecurityfilter, filterlinks);
            var objInfo = new NBrightInfo();

            objInfo.ItemID = -1;
            objInfo.TypeCode = "AJAXDATA";
            objInfo.XMLData = xmlData;
            return objInfo;
        }

        /// <summary>
        /// Split ajax list return into List of ajax data strings
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static List<String> GetAjaxDataList(HttpContext context)
        {
            var rtnList = new List<String>();
            var xmlAjaxData = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
            if (!String.IsNullOrEmpty(xmlAjaxData))
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlAjaxData);
                var nodList = xmlDoc.SelectNodes("root/root");
                if (nodList != null)
                    foreach (XmlNode nod in nodList)
                    {
                        rtnList.Add(nod.OuterXml);
                    }
            }
            return rtnList;
        }

        /// <summary>
        /// Get the XML ajax returned data
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static String GetAjaxData(HttpContext context)
        {
            return HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
        }

        public static List<NBrightInfo> GetGenXmlListByAjax(HttpContext context, bool ignoresecurityfilter = false, bool filterlinks = false)
        {
            var xmlAjaxData = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
            return GetGenXmlListByAjax(xmlAjaxData, ignoresecurityfilter ,  filterlinks );
        }

        public static List<NBrightInfo> GetGenXmlListByAjax(String xmlAjaxData, bool ignoresecurityfilter = false, bool filterlinks = false)
        {
            var rtnList = new List<NBrightInfo>();
            if (!String.IsNullOrEmpty(xmlAjaxData))
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlAjaxData);
                var nodList = xmlDoc.SelectNodes("root/root");
                if (nodList != null)
                    foreach (XmlNode nod in nodList)
                    {
                        var xmlData = GenXmlFunctions.GetGenXmlByAjax(nod.OuterXml, "","genxml", ignoresecurityfilter, filterlinks);
                        var objInfo = new NBrightInfo();
                        objInfo.ItemID = -1;
                        objInfo.TypeCode = "AJAXDATA";
                        objInfo.XMLData = xmlData;
                        rtnList.Add(objInfo);
                    }
            }
            return rtnList;
        }

        public static Boolean CheckRights()
        {
            if (UserController.Instance.GetCurrentUserInfo().IsInRole("Manager") || UserController.Instance.GetCurrentUserInfo().IsInRole("Editor") || UserController.Instance.GetCurrentUserInfo().IsInRole("Administrators"))
            {
                return true;
            }
            return false;
        }

        public static void UpdateSettings(NBrightInfo settings)
        {
            // get template
            if (settings.ModuleId > 0)
            {
                var objCtrl = new NBrightDataController();

                if (settings.GetXmlPropertyBool("genxml/checkbox/resetvalidationflag"))
                {
                    LocalUtils.ResetValidationFlag();
                    settings.SetXmlProperty("genxml/checkbox/resetvalidationflag", "False");
                    settings.UserId = -1; // make sure we save the reset flag on this module.
                }

                objCtrl.Update(settings);
                Utils.RemoveCache("nbrightmodsettings*" + settings.ModuleId.ToString(""));
            }
        }

        public static NBrightInfo GetSettings(String moduleid,Boolean useCache = true)
        {
            var rtnCache = Utils.GetCache("nbrightmodsettings*" + moduleid);
            if (rtnCache != null && useCache) return (NBrightInfo)rtnCache;
            // get template
            if (Utils.IsNumeric(moduleid) && Convert.ToInt32(moduleid) > 0)
            {
                var objCtrl = new NBrightDataController();
                var dataRecord = objCtrl.GetByType(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), "SETTINGS");
                if (dataRecord == null) dataRecord = new NBrightInfo(true);
                if (dataRecord.TypeCode == "SETTINGS")
                {
                    // only add to cache if we have a valid settings record, LocalUtils.IncludePageHeaders may be called before the creation of the settings.
                    Utils.SetCache("nbrightmodsettings*" + moduleid, dataRecord);
                }
                return dataRecord;
            }
            return new NBrightInfo(true);
        }

        [Obsolete("Not used anymore", true)]
        public static String GetDatabaseCache(int portalid, int moduleid, String lang, int userid = -1, Boolean useCache = true)
        {
            var rtnCache = Utils.GetCache("nbrightdatabasecache*" + moduleid.ToString("") + lang + userid.ToString(""));
            if (rtnCache != null && useCache) return (String)rtnCache;
            var objCtrl = new NBrightDataController();
            var dataRecord = objCtrl.GetByType(portalid, moduleid, "NBrightModCACHE", userid.ToString(""), lang);
            if (dataRecord != null && dataRecord.Lang == lang) return dataRecord.TextData; 
            return "";
        }

        [Obsolete("Not used anymore", true)]
        public static void SetDatabaseCache(int portalid, int moduleid, String lang, String cachedata, int userid = -1)
        {
            var objCtrl = new NBrightDataController();
            var dataRecord = objCtrl.GetByType(portalid, moduleid, "NBrightModCACHE", userid.ToString(""), lang);
            if (dataRecord == null || dataRecord.Lang != lang) 
            {
                dataRecord = new NBrightInfo(true);
                dataRecord.TypeCode = "NBrightModCACHE";
                dataRecord.UserId = userid;
                dataRecord.Lang = lang;
                dataRecord.ModuleId = moduleid;
                dataRecord.PortalId = portalid;
            }
            dataRecord.TextData = cachedata;
            objCtrl.Update(dataRecord);
            Utils.SetCache("nbrightdatabasecache*" + moduleid.ToString("") + lang + userid.ToString(""), dataRecord.TextData);
        }

        [Obsolete("Not used anymore", true)] // gives performace issues.
        public static void ClearDatabaseCache(int portalid, int moduleid)
        {
            // clear database module cache
            var objCtrl = new NBrightDataController();
            var dataList = objCtrl.GetList(portalid, moduleid, "NBrightModCACHE");
            foreach (var nbi in dataList)
            {
                nbi.TextData = "";
                objCtrl.Update(nbi);
                Utils.RemoveCache("nbrightdatabasecache*" + moduleid.ToString("") + nbi.Lang + nbi.UserId.ToString(""));
            }

        }

        public static void SetFileCache(String cacheKey, String cachedata, string moduleid)
        {
            var cacheFolder = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Cache";
            Utils.CreateFolder(cacheFolder); // creates is not there
            var modFolder = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Cache\\" + moduleid;
            Utils.CreateFolder(modFolder); // creates is not there
            var cacheFileName = modFolder + "\\" + cacheKey + ".txt";
            Utils.SaveFile(cacheFileName,cachedata);
        }

        public static String GetFileCache(String cacheKey, string moduleid)
        {
            var cacheFolder = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Cache";
            Utils.CreateFolder(cacheFolder); // creates is not there
            var modFolder = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Cache\\" + moduleid;
            Utils.CreateFolder(modFolder); // creates is not there
            var cacheFileName = modFolder + "\\" + cacheKey + ".txt";
            return Utils.ReadFile(cacheFileName);
        }

        public static void ClearFileCache(int moduleid)
        {
            var modFolder = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Cache\\" + moduleid;
            Utils.DeleteFolder(modFolder,true);
        }

        public static Object GetRazorCache(String cacheKey, string moduleid)
        {
            var rtnObj = Utils.GetCache(cacheKey);
            if (rtnObj == null)
            {
                rtnObj = GetFileCache(cacheKey, moduleid);
            }
            return rtnObj;
        }

        public static void SetRazorCache(String cacheKey, String cachedata, string moduleid)
        {
            Utils.SetCache(cacheKey, cachedata);
            var modCacheList = (List<String>) Utils.GetCache("nbrightmodcache*" + moduleid);
            if (modCacheList == null) modCacheList = new List<String>();
            if (!modCacheList.Contains(cacheKey)) modCacheList.Add(cacheKey);
            Utils.SetCache("nbrightmodcache*" + moduleid, modCacheList);
            SetFileCache(cacheKey, cachedata, moduleid);
        }

        public static List<NBrightInfo> GetNBrightModList()
        {
            var rtnList = new List<NBrightInfo>();
            // get template
            var objCtrl = new NBrightDataController();
            var dataList = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "SETTINGS", " and NB1.TextData = 'NBrightMod'", " order by [XMLData].value('(/genxml/dropdownlist/themefolder)[1]', 'varchar(max)') ");
            foreach (var nbi in dataList)
            {
                rtnList.Add(nbi);
            }
            return rtnList;
        }


        public static void ClearRazorCache(String moduleid)
        {
                var modCacheList = (List<String>)Utils.GetCache("nbrightmodcache*" + moduleid);
                if (modCacheList != null)
                {
                    foreach (var cachekey in modCacheList)
                    {
                        Utils.RemoveCache(cachekey);
                    }
                }

            Utils.RemoveCache("nbrightmodsettings*" + moduleid);

            if (Utils.IsNumeric(moduleid))
            {
                ClearFileCache(Convert.ToInt32(moduleid));
            }
        }

        public static void ClearRazorSateliteCache(String moduleid)
        {
            var settings = GetSettings(moduleid);

            // clear satelite module cache
            var objCtrl = new NBrightDataController();
            var dataList = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "SETTINGS", " and NB1.XrefItemId = " + settings.ItemID);
            foreach (var nbi in dataList)
            {
                ClearRazorCache(nbi.ModuleId.ToString(""));
            }

        }


        public static String RazorTemplRenderList(String razorTemplName, String moduleid, String cacheKey, List<NBrightInfo> objList, String lang, Boolean debug = false)
        {
            // do razor template
            var cachekey = "NBrightModKey" + razorTemplName + "-" + moduleid + "-" + cacheKey + PortalSettings.Current.PortalId.ToString() + "-" + lang;
            var razorTempl = (String)GetRazorCache(cachekey,moduleid);
            if (debug || String.IsNullOrWhiteSpace(razorTempl))
            {
                var settingInfo = GetSettings(moduleid);
                var razorTempl2 = LocalUtils.GetTemplateData(razorTemplName, lang, settingInfo.ToDictionary());
                if (!String.IsNullOrWhiteSpace(razorTempl2))
                {
                    //BEGIN: INJECT RESX: assume we always want the resx paths adding
                    razorTempl2 = " @AddMetaData(\"resourcepath\",\"/DesktopModules/NBright/NBrightMod/App_LocalResources\") " + razorTempl2;
                    razorTempl2 = " @AddMetaData(\"resourcepath\",\"/DesktopModules/NBright/NBrightMod/Themes/" + settingInfo.GetXmlProperty("genxml/dropdownlist/themefolder") + "/resx\") " + razorTempl2;
                    razorTempl2 = " @AddMetaData(\"resourcepath\",\"/" + PortalSettings.Current.HomeDirectory.Trim('/') + "/NBrightMod/Themes/" + settingInfo.GetXmlProperty("genxml/dropdownlist/themefolder") + "/resx\") " + razorTempl2;
                    //END: INJECT RESX

                    if (!objList.Any())
                    {
                        var obj = new NBrightInfo(true);
                        obj.ModuleId = Convert.ToInt32(moduleid);
                        obj.Lang = Utils.GetCurrentCulture();
                        objList.Add(obj);
                    }
                    var razorTemplateKey = "NBrightModKey" + moduleid + razorTemplName + PortalSettings.Current.PortalId.ToString() + lang;

                    var modRazor = new NBrightRazor(objList.Cast<object>().ToList(), settingInfo.ToDictionary(), HttpContext.Current.Request.QueryString);
                    var razorTemplOut = RazorRender(modRazor, razorTempl2, razorTemplateKey, settingInfo.GetXmlPropertyBool("genxml/checkbox/debugmode"));

                    if (cacheKey != "") // only cache if we have a key.
                    {
                        SetRazorCache(cachekey, razorTemplOut,moduleid);
                    }
                    return razorTemplOut;
                }
            }
            return (String)razorTempl;
        }

        public static String RazorTemplRender(String razorTemplName, String moduleid, String cacheKey, NBrightInfo obj, String lang, Boolean debug = false)
        {
            // do razor template
            var cachekey = "NBrightModKey" + razorTemplName + "-" + moduleid + "-" + cacheKey + PortalSettings.Current.PortalId.ToString();
            var razorTempl = (String)GetRazorCache(cachekey,moduleid);
            if (debug || String.IsNullOrWhiteSpace(razorTempl))
            {
                var settingInfo = GetSettings(moduleid);
                var razorTempl2 = LocalUtils.GetTemplateData(razorTemplName, lang, settingInfo.ToDictionary());
                if (!String.IsNullOrWhiteSpace(razorTempl2))
                {
                    //BEGIN: INJECT RESX: assume we always want the resx paths adding
                    razorTempl2 = " @AddMetaData(\"resourcepath\",\"/DesktopModules/NBright/NBrightMod/App_LocalResources\") " + razorTempl2;
                    razorTempl2 = " @AddMetaData(\"resourcepath\",\"/DesktopModules/NBright/NBrightMod/Themes/" + settingInfo.GetXmlProperty("genxml/dropdownlist/themefolder") + "/resx\") " + razorTempl2;
                    razorTempl2 = " @AddMetaData(\"resourcepath\",\"/" + PortalSettings.Current.HomeDirectory.Trim('/') + "/NBrightMod/Themes/" + settingInfo.GetXmlProperty("genxml/dropdownlist/themefolder") + "/resx\") " + razorTempl2;
                    //END: INJECT RESX

                    if (obj == null || obj.XMLData == null) obj = new NBrightInfo(true);
                    // we MUST set the langauge so rendering sub-templates works in the correct languague
                    if (lang == "")lang = Utils.RequestParam(HttpContext.Current, "language");
                    obj.SetXmlProperty("genxml/hidden/currentlang", lang);
                    
                    var razorTemplateKey = "NBrightModKey" + moduleid + settingInfo.GetXmlProperty("genxml/dropdownlist/themefolder") + razorTemplName + PortalSettings.Current.PortalId.ToString();

                    var l = new List<object>();
                    l.Add(obj);
                    var modRazor = new NBrightRazor(l, settingInfo.ToDictionary(), HttpContext.Current.Request.QueryString);
                    var razorTemplOut = RazorRender(modRazor, razorTempl2, razorTemplateKey, debug);

                    if (cacheKey != "") // only cache if we have a key.
                    {
                        SetRazorCache(cachekey, razorTemplOut, moduleid);
                    }
                    if (String.IsNullOrWhiteSpace(razorTemplOut))
                    {
                        // we have a temnplate that procduces nothing 
                        // this template might not be required, but to not effect processing too much we'll set a dummy cache
                        SetRazorCache(cachekey, "<span>blank</span>", moduleid);
                        return "";
                    }
                    return razorTemplOut;
                }
                else
                {
                    // the template is empty or blank, but we set a dummy cache record becuase we don't want to process again.
                    SetRazorCache(cachekey, "<span>blank</span>", moduleid);
                }
            }
            if (razorTempl == "<span>blank</span>") return ""; // see if we've set a dummy cache to speed processing.
            return (String)razorTempl;
        }

        /// <summary>
        /// This method preprocesses the razor template, to add meta data required for selecting data into cache.
        /// </summary>
        /// <param name="fullTemplName">template name (Theme prefix is added from settings, if no theme prefix exists)</param>
        /// <param name="moduleid"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static Dictionary<String, String> RazorPreProcessTempl(String templName, String moduleid, String lang)
        {
            // see if we need to add theme to template name.
            var settignInfo = GetSettings(moduleid);
            var theme = settignInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
            if (templName.Split('.').Length == 2) templName = theme + "." + templName;

            var fullTemplName = templName + moduleid; // NOTE: preprocess Needs the moduleid so any filter works correct across modules.

            // get cached data if there
            var cachedlist = (Dictionary<String, String>) Utils.GetCache("preprocessmetadata" + fullTemplName);  
            if (cachedlist != null) return cachedlist;

            // build cache data from template.
            cachedlist = new Dictionary<String, String>();
            var razorTempl = LocalUtils.GetTemplateData(templName, lang, settignInfo.ToDictionary());
            if (razorTempl != "" && razorTempl.Contains("AddPreProcessMetaData("))
            {
                var obj = new NBrightInfo(true);
                obj.Lang = lang;
                obj.ModuleId = Convert.ToInt32(moduleid);
                var l = new List<object>();
                l.Add(obj);
                var modRazor = new NBrightRazor(l, settignInfo.ToDictionary(), HttpContext.Current.Request.QueryString);
                try
                {
                    // do razor and cache preprocessmetadata
                    razorTempl = RazorRender(modRazor, razorTempl, "preprocessmetadata" + fullTemplName, settignInfo.GetXmlPropertyBool("genxml/checkbox/debugmode"));

                    // IMPORTANT: The AddPreProcessMetaData token will add any meta data to the cache list, we must get that list back into the cachedlist var.
                    cachedlist = (Dictionary<String, String>)Utils.GetCache("preprocessmetadata" + fullTemplName);

                    // if we have no preprocess items, we don;t want to run this again, so put the empty dic into cache.
                    if (cachedlist != null && cachedlist.Count == 0) Utils.SetCache("preprocessmetadata" + fullTemplName, cachedlist);
                }
                catch (Exception ex)
                {
                    // Only log exception, could be a error because of missing data.  Thge preprocessing doesn't care.
                    Exceptions.LogException(ex);
                }
            }
            return cachedlist;
        }



        public static void IncludePageHeaders(String moduleid, Page page, String moduleName,String templateprefix = "",String theme = "")
        {
            if (!page.Items.Contains("nbrightinject")) page.Items.Add("nbrightinject", "");
            var settignInfo = GetSettings(moduleid);
            if (theme == "") theme = settignInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
            var fullTemplName = theme + "." + templateprefix + "pageheader.cshtml";
            if (!page.Items["nbrightinject"].ToString().Contains(fullTemplName + "." + moduleName + ","))
            {
                var nbi = new NBrightInfo();
                nbi.Lang = Utils.GetCurrentCulture();
                var razorTempl = RazorTemplRender(fullTemplName, moduleid, Utils.GetCurrentCulture(), nbi, Utils.GetCurrentCulture());
                if (razorTempl != "")
                {
                    PageIncludes.IncludeTextInHeader(page, razorTempl);
                    page.Items["nbrightinject"] = page.Items["nbrightinject"] + fullTemplName + "." + moduleName + ",";
                }
            }
        }

        public static void RemoveCachedRazorEngineService()
        {
            HttpContext.Current.Application.Set("NBrightModIRazorEngineService", null);
        }

        public static String RazorRender(Object info, String razorTempl, String templateKey, Boolean debugMode = false)
        {

            var service = (IRazorEngineService)HttpContext.Current.Application.Get("NBrightModIRazorEngineService");
            if (service == null || debugMode)
            {
                // do razor test
                var config = new TemplateServiceConfiguration();
                config.Debug = debugMode;
                config.BaseTemplateType = typeof(NBrightModRazorTokens<>);
                service = RazorEngineService.Create(config);
                Engine.Razor = service;
                HttpContext.Current.Application.Set("NBrightModIRazorEngineService", service);
            }

            var result = "";
            try
            {
                result = Engine.Razor.RunCompile(razorTempl, templateKey, null, info);
            }
            catch (Exception e)
            {
                result = "<div>" + e.Message + " templateKey='" + templateKey + "'</div>";
            }

            return result;
        }

        public static NBrightInfo CreateRequiredUploadFolders(NBrightInfo settings)
        {
            var objPortal = PortalController.Instance.GetPortal(settings.PortalId);

            var tempFolder = objPortal.HomeDirectory.TrimEnd('/') + "/NBrightTemp";
            var tempFolderMapPath = objPortal.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp";
            Utils.CreateFolder(tempFolderMapPath);

            var settingUploadFolder = settings.GetXmlProperty("genxml/textbox/settinguploadfolder");
            if (settingUploadFolder == "")
            {
                settingUploadFolder = "images";
                settings.SetXmlProperty("genxml/textbox/settinguploadfolder", settingUploadFolder);
            }

            var uploadFolder = objPortal.HomeDirectory.TrimEnd('/') + "/NBrightUpload/" + settingUploadFolder;
            var uploadFolderMapPath = objPortal.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightUpload\\" + settingUploadFolder;
            Utils.CreateFolder(uploadFolderMapPath);

            var uploadDocFolder = objPortal.HomeDirectory.TrimEnd('/') + "/NBrightUpload/documents";
            var uploadDocFolderMapPath = objPortal.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightUpload\\documents";
            Utils.CreateFolder(uploadDocFolderMapPath);

            var uploadSecureDocFolder = objPortal.HomeDirectory.TrimEnd('/') + "/NBrightUpload/securedocs";
            var uploadSecureDocFolderMapPath = objPortal.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightUpload\\securedocs";
            Utils.CreateFolder(uploadSecureDocFolderMapPath);

            settings.SetXmlProperty("genxml/tempfolder", "/" + tempFolder.TrimStart('/'));
            settings.SetXmlProperty("genxml/tempfoldermappath", tempFolderMapPath);
            settings.SetXmlProperty("genxml/uploadfolder", "/" + uploadFolder.TrimStart('/'));
            settings.SetXmlProperty("genxml/uploadfoldermappath", uploadFolderMapPath);
            settings.SetXmlProperty("genxml/uploaddocfolder", "/" + uploadDocFolder.TrimStart('/'));
            settings.SetXmlProperty("genxml/uploaddocfoldermappath", uploadDocFolderMapPath);
            settings.SetXmlProperty("genxml/uploadsecuredocfolder", "/" + uploadSecureDocFolder.TrimStart('/'));
            settings.SetXmlProperty("genxml/uploadsecuredocfoldermappath", uploadSecureDocFolderMapPath);

            return settings;
        }


        /// <summary>
        /// do any validation of data required.  
        /// </summary>
        public static void ResetValidationFlag()
        {
            var objCtrl = new NBrightDataController();
            var allSettings = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "SETTINGS");
            foreach (var nbi in allSettings)
            {
                nbi.UserId = -1;
                objCtrl.Update(nbi);
            }
        }

        public static string RemoveWhitespace(this string str)
        {
            StringBuilder sb = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }


        /// <summary>
        /// do any validation of data required.  
        /// </summary>
        public static void ClearModuleCacheByTheme(String themefolder)
        {
            var objCtrl = new NBrightDataController();

            // check for invalid records and remove
            var modList = LocalUtils.GetNBrightModList();
            foreach (var tItem in modList)
            {
                var modInfo = DnnUtils.GetModuleinfo(tItem.ModuleId);
                if (modInfo != null)
                {
                    var modsettings = objCtrl.GetByType(PortalSettings.Current.PortalId, modInfo.ModuleID, "SETTINGS");
                    if (modsettings != null && modsettings.GetXmlProperty("genxml/dropdownlist/themefolder") == themefolder)
                    {
                        LocalUtils.ClearRazorCache(tItem.ModuleId.ToString(""));
                        LocalUtils.ClearRazorSateliteCache(tItem.ModuleId.ToString(""));
                        // clear any setting cache
                        Utils.RemoveCache("nbrightmodsettings*" + tItem.ModuleId.ToString(""));
                    }
                }
            }
        }

        /// <summary>
        /// do any validation of data required.  
        /// </summary>
        public static void ValidateModuleData()
        {
            var objCtrl = new NBrightDataController();

            // check for invalid records and remove
            var modList = LocalUtils.GetNBrightModList();
            foreach (var tItem in modList)
            {
                var modInfo = DnnUtils.GetModuleinfo(tItem.ModuleId);
                if (modInfo == null) // might happen if invalid module data is imported
                {
                    DeleteAllDataRecords(tItem.ModuleId);
                }
                else
                {
                    LocalUtils.ClearRazorCache(tItem.ModuleId.ToString(""));
                    LocalUtils.ClearRazorSateliteCache(tItem.ModuleId.ToString(""));
                }
                // clear any setting cache
                Utils.RemoveCache("nbrightmodsettings*" + tItem.ModuleId.ToString(""));
            }


            // realign module satelite link + plus clear import flag
            var allSettings = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "SETTINGS"," and NB1.userid = -1");
            foreach (var nbi in allSettings)
            {
                if (nbi.UserId == -1) // flag to indicate import of module has been done.
                {
                    nbi.UserId = 0;
                    if (nbi.XrefItemId > 0)
                    {
                        var datasource = objCtrl.GetByGuidKey(nbi.PortalId, -1, "SETTINGS", nbi.GetXmlProperty("genxml/dropdownlist/datasourceref"));
                        if (datasource != null)
                            nbi.XrefItemId = datasource.ItemID;
                        else
                            nbi.XrefItemId = 0;
                    }

                    // realign singlepage itemid
                    if (nbi.GetXmlPropertyBool("genxml/hidden/singlepageedit") && nbi.GetXmlPropertyInt("genxml/hidden/singlepageitemid") > 0)
                    {
                        var datasource = objCtrl.GetByType(nbi.PortalId, nbi.ModuleId, "NBrightModDATA");
                        if (datasource != null)
                        {
                            nbi.SetXmlProperty("genxml/hidden/singlepageitemid", datasource.ItemID.ToString(""));
                        }
                    }

                    // update any image paths
                    var datalist = objCtrl.GetList(nbi.PortalId, nbi.ModuleId, "NBrightModDATA");
                    foreach (var datasource in datalist)
                    {
                        var upd = false;
                        var imgList = datasource.XMLDoc.SelectNodes("genxml/imgs/genxml");
                        if (imgList != null)
                        {
                            var lp = 1;
                            foreach (var xnod in imgList)
                            {
                                var relPath = datasource.GetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imageurl");
                                if (relPath != "")
                                {
                                    var imgMapPath = System.Web.Hosting.HostingEnvironment.MapPath(relPath);
                                    datasource.SetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imagepath", imgMapPath);
                                    upd = true;
                                }
                                lp += 1;
                            }
                        }
                        var docList = datasource.XMLDoc.SelectNodes("genxml/docs/genxml");
                        if (docList != null)
                        {
                            var lp = 1;
                            foreach (var xnod in docList)
                            {
                                var relPath = datasource.GetXmlProperty("genxml/docs/genxml[" + lp + "]/hidden/docurl");
                                if (relPath != "")
                                {
                                    var docMapPath = System.Web.Hosting.HostingEnvironment.MapPath(relPath);
                                    datasource.SetXmlProperty("genxml/docs/genxml[" + lp + "]/hidden/docpath", docMapPath);
                                    upd = true;
                                }
                                lp += 1;
                            }
                        }
                        if (upd) objCtrl.Update(datasource);
                    }

                    //update language data records.
                    var datalist2 = objCtrl.GetList(nbi.PortalId, nbi.ModuleId, "NBrightModDATALANG");
                    foreach (var datasource in datalist2)
                    {
                        var upd = false;

                        // check for invalid empty record.
                        if (datasource.XMLDoc == null)
                        {
                            datasource.ValidateXmlFormat();
                            upd = true;
                        }

                        var imgList = datasource.XMLDoc.SelectNodes("genxml/imgs/genxml");
                        if (imgList != null)
                        {
                            var lp = 1;
                            foreach (var xnod in imgList)
                            {
                                var relPath = datasource.GetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imageurl");
                                if (relPath != "")
                                {
                                    var imgMapPath = System.Web.Hosting.HostingEnvironment.MapPath(relPath);
                                    datasource.SetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imagepath", imgMapPath);
                                    upd = true;
                                }
                                lp += 1;
                            }
                        }
                        var docList = datasource.XMLDoc.SelectNodes("genxml/docs/genxml");
                        if (docList != null)
                        {
                            var lp = 1;
                            foreach (var xnod in docList)
                            {
                                var relPath = datasource.GetXmlProperty("genxml/docs/genxml[" + lp + "]/hidden/docurl");
                                if (relPath != "")
                                {
                                    var docMapPath = System.Web.Hosting.HostingEnvironment.MapPath(relPath);
                                    datasource.SetXmlProperty("genxml/docs/genxml[" + lp + "]/hidden/docpath", docMapPath);
                                    upd = true;
                                }
                                lp += 1;
                            }
                        }
                        if (upd) objCtrl.Update(datasource);
                    }


                    var nbisave = CreateRequiredUploadFolders(nbi);
                    objCtrl.Update(nbisave);


                }
            }



        }


        public static string ExportPortalTheme(string theme,string exportname = "")
        {
            if (theme != "")
            {
                var portalthemeFolderName = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + theme;

                if (!Directory.Exists(portalthemeFolderName)) return "";

                // export from portal level, so save export.config
                var exportconfig = new NBrightInfo(true);
                exportconfig.SetXmlProperty("genxml/portallevel", "true");
                exportconfig.SetXmlProperty("genxml/portalmappath", PortalSettings.Current.HomeDirectoryMapPath);
                exportconfig.SetXmlProperty("genxml/portalpath", PortalSettings.Current.HomeDirectory);
                exportconfig.SetXmlProperty("genxml/systhemefoldername", "");
                exportconfig.SetXmlProperty("genxml/portalthemefoldername", portalthemeFolderName);
                exportconfig.SetXmlProperty("genxml/portalrelfolder", PortalSettings.Current.HomeDirectory + "/NBrightMod/Themes/" + theme);
                exportconfig.SetXmlProperty("genxml/systemrelfolder", "/DesktopModules/NBright/NBrightMod/Themes/" + theme);
                exportconfig.SetXmlProperty("genxml/themename", theme);

                exportconfig.SetXmlProperty("genxml/resxfiles", GenXmlFunctions.EncodeCDataTag(ExportResxXml(theme)));

                Utils.SaveFile(portalthemeFolderName.TrimEnd('\\') + "\\export.config", exportconfig.XMLData);

                Utils.CreateFolder(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp");
                if (exportname == "") exportname = theme;
                var zipFile = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp\\NBrightMod_Theme_" + exportname + ".zip";

                DnnUtils.ZipFolder(portalthemeFolderName, zipFile);

                return zipFile;
            }
            return "";
        }

        public static string ExportSystemTheme(string theme, string exportname = "")
        {
            //get uploaded params
            if (theme != "")
            {
                var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod");
                var systhemeFolderName = controlMapPath + "\\Themes\\" + theme;

                if (!Directory.Exists(systhemeFolderName)) return "";

                // export from portal level, so save export.config
                var exportconfig = new NBrightInfo(true);

                exportconfig.SetXmlProperty("genxml/portallevel", "false");
                exportconfig.SetXmlProperty("genxml/portalmappath", PortalSettings.Current.HomeDirectoryMapPath);
                exportconfig.SetXmlProperty("genxml/portalpath", PortalSettings.Current.HomeDirectory);
                exportconfig.SetXmlProperty("genxml/systhemefoldername", systhemeFolderName);
                exportconfig.SetXmlProperty("genxml/portalthemefoldername", "");
                exportconfig.SetXmlProperty("genxml/portalrelfolder",PortalSettings.Current.HomeDirectory + "/NBrightMod/Themes/" + theme);
                exportconfig.SetXmlProperty("genxml/systemrelfolder","/DesktopModules/NBright/NBrightMod/Themes/" + theme);
                exportconfig.SetXmlProperty("genxml/themename", theme);

                Utils.SaveFile(systhemeFolderName.TrimEnd('\\') + "\\export.config", exportconfig.XMLData);

                Utils.CreateFolder(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp");
                if (exportname == "") exportname = theme;
                var zipFile = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp\\NBrightMod_Theme_" + exportname + ".zip";

                DnnUtils.ZipFolder(systhemeFolderName, zipFile);

                return zipFile;
            }
            return "";

        }

        /// <summary>
        /// Import zip file into portal level template area
        /// </summary>
        /// <param name="zipFileMapPath">Zip File MapPath</param>
        /// <param name="extractFolderMapPath">Mappath to add to extract folder, if empty the default theme fodler under NBrightMod\Themes is used.</param>
        /// <returns>String with status message</returns>
        public static string ImportPortalTheme(String zipFileMapPath,string extractFolderMapPath = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(zipFileMapPath))
                {
                    var themeName = Path.GetFileName(zipFileMapPath).Replace("NBrightMod_Theme_", "").Replace(".zip", "");
                    if (!string.IsNullOrEmpty(themeName))
                    {
                        var themeFolderName = extractFolderMapPath;
                        if (extractFolderMapPath == "")
                        {
                            themeFolderName = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + themeName;
                            if (!Directory.Exists(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod"))
                            {
                                Directory.CreateDirectory(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod");
                                Directory.CreateDirectory(PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\");
                            }
                        }
                        else
                        {
                            if (!Directory.Exists(extractFolderMapPath))
                            {
                                Directory.CreateDirectory(extractFolderMapPath);
                            }
                        }
                        DnnUtils.UnZip(zipFileMapPath, themeFolderName);
                        Utils.DeleteSysFile(zipFileMapPath);

                        //// import/merge resx data for theme.ascx.resx
                        //var nodfl = xmlDoc.SelectNodes("genxml/resxfiles/resxfile");
                        //LocalUtils.ImportResxXml(nodfl, theme);


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

        public static string ImportSystemTheme(String zipFileMapPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(zipFileMapPath))
                {
                    var themeName = Path.GetFileName(zipFileMapPath).Replace("NBrightMod_Theme_", "").Replace(".zip", "");
                    var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod");
                    var systhemeFolderName = controlMapPath + "\\Themes\\" + themeName;

                    if (!string.IsNullOrEmpty(themeName))
                    {
                        DnnUtils.UnZip(zipFileMapPath, systhemeFolderName);
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

        public static void DeleteAllDataRecords(int moduleid)
        {
            var objCtrl = new NBrightDataController();

            var l1 = objCtrl.GetList(PortalSettings.Current.PortalId, moduleid, "NBrightModDATA");
            foreach (var i in l1)
            {
                objCtrl.Delete(i.ItemID);
            }
            var l2 = objCtrl.GetList(PortalSettings.Current.PortalId, moduleid, "NBrightModDATALANG");
            foreach (var i in l2)
            {
                objCtrl.Delete(i.ItemID);
            }
            var l3 = objCtrl.GetList(PortalSettings.Current.PortalId, moduleid, "SETTINGS");
            foreach (var i in l3)
            {
                objCtrl.Delete(i.ItemID);
            }

        }

        public static string ExportResxXml(string themefolder)
        {
            var xmlOut = "<resxfiles>";

            var sourcesystemresx = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + themefolder + "/resx");
            if (Directory.Exists(sourcesystemresx))
            {
                var resxfiles = Directory.GetFiles(sourcesystemresx, "theme.ascx*.resx");
                foreach (var r in resxfiles)
                {
                    xmlOut += "<resxfile name='" + Path.GetFileName(r) + "'><![CDATA[";
                    var rData = Utils.ReadFile(r);
                    xmlOut += GenXmlFunctions.EncodeCDataTag(rData);
                    xmlOut += "]]></resxfile>";
                }
            }
            xmlOut += "</resxfiles>";
            return xmlOut;
        }

        public static void ImportResxXml(XmlNodeList nodList,string themefolder)
        {
            if (nodList != null)
            {
                foreach (XmlNode xNodResx in nodList)
                {
                    var resxXML = xNodResx.InnerXml.Replace("<![CDATA[", "").Replace("]]>", "");
                    resxXML = GenXmlFunctions.DecodeCDataTag(resxXML);
                    var xmlDocresx = new XmlDocument();
                    xmlDocresx.LoadXml(resxXML);

                    var xNod = xmlDocresx.SelectSingleNode("/resxfile");
                    if (xNod?.Attributes?["name"] != null)
                    {
                        var fname = xNod.Attributes["name"].InnerText;
                        var fdata = xNod.InnerText;

                        var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod");
                        var systhemeFileName = controlMapPath + "\\Themes\\" + themefolder + "\\" + fname;
                        if (File.Exists(systhemeFileName))
                        {
                            // save resx temp file

                            Utils.SaveBase64ToFile(systhemeFileName + ".tmp", fdata);

                            var resxlist = new List<DictionaryEntry>();
                            // read temp resx file
                            if (File.Exists(systhemeFileName + ".tmp"))
                            {
                                ResXResourceReader rsxr = new ResXResourceReader(systhemeFileName + ".tmp");
                                foreach (DictionaryEntry d in rsxr)
                                {
                                    resxlist.Add(d);
                                }
                                rsxr.Close();
                                File.Delete(systhemeFileName + ".tmp");
                            }

                            // read resx file
                            if (File.Exists(systhemeFileName))
                            {
                                ResXResourceReader rsxr = new ResXResourceReader(systhemeFileName);
                                foreach (DictionaryEntry d in rsxr)
                                {
                                    resxlist.Add(d);
                                }
                                rsxr.Close();
                                File.Delete(systhemeFileName);
                            }

                            // merge resx file
                            using (ResXResourceWriter resx = new ResXResourceWriter(systhemeFileName))
                            {
                                foreach (var d in resxlist)
                                {
                                    resx.AddResource(d.Key.ToString(), d.Value.ToString());
                                }
                            }

                        }
                        else
                        {
                            // save resx file
                            Utils.SaveBase64ToFile(systhemeFileName, fdata);
                        }
                    }
                }
            }

        }


        #endregion

    }

}
