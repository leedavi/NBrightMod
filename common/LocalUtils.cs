


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Xml;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Exceptions;
using NBrightCore.common;
using NBrightCore.render;
using NBrightCore.TemplateEngine;
using NBrightDNN;
using NBrightDNN.render;
using NBrightMod.render;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace NBrightMod.common
{

    public static class LocalUtils
    {


        #region "functions"

        public static String GetTemplateData(String templatename, String lang, Dictionary<String, String> settings = null)
        {
            var themeFolder = "config";
            if (settings != null && settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
            var parseTemplName = templatename.Split('.');
            if (parseTemplName.Count() == 3)
            {
                themeFolder = parseTemplName[0];
                templatename = parseTemplName[1] + "." + parseTemplName[2];
            }

            var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod");
            var templCtrl = new NBrightCore.TemplateEngine.TemplateGetter(PortalSettings.Current.HomeDirectoryMapPath + "\\NBrightMod", controlMapPath, "Themes\\" + themeFolder, "");
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

        public static NBrightInfo GetAjaxFields(HttpContext context)
        {
            var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
            return GetAjaxFields(strIn);
        }

        public static NBrightInfo GetAjaxFields(String ajaxData,String mergeWithXml = "")
        {
            var xmlData = GenXmlFunctions.GetGenXmlByAjax(ajaxData, mergeWithXml);
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

        public static List<NBrightInfo> GetGenXmlListByAjax(HttpContext context)
        {
            var xmlAjaxData = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
            return GetGenXmlListByAjax(xmlAjaxData);
        }

        public static List<NBrightInfo> GetGenXmlListByAjax(String xmlAjaxData)
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
                        var xmlData = GenXmlFunctions.GetGenXmlByAjax(nod.OuterXml, "");
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
            if (UserController.GetCurrentUserInfo().IsInRole("Manager") || UserController.GetCurrentUserInfo().IsInRole("Editor") || UserController.GetCurrentUserInfo().IsInRole("Administrators"))
            {
                return true;
            }
            return false;
        }

        public static NBrightInfo GetSettings(String moduleid)
        {
            // get template
            if (Utils.IsNumeric(moduleid))
            {
                var objCtrl = new NBrightDataController();
                var dataRecord = objCtrl.GetByType(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), "SETTINGS");
                if (dataRecord == null) dataRecord = new NBrightInfo(true);
                return dataRecord;
            }
            return new NBrightInfo(true);
        }

        public static List<NBrightInfo> GetNBrightModList()
        {
            var rtnList = new List<NBrightInfo>();
            // get template
            var objCtrl = new NBrightDataController();
            var dataList = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "SETTINGS", " and NB1.TextData = 'NBrightMod'");
            foreach (var nbi in dataList)
            {
                rtnList.Add(nbi);
            }
            return rtnList;
        }

        public static void RazorClearCache(String moduleid)
        {
            // do razor template
            var modCacheList = (List<String>)Utils.GetCache("nbrightmodcache*" + moduleid);
            if (modCacheList != null)
            {
                foreach (var cachekey in modCacheList)
                {
                    Utils.RemoveCache(cachekey);
                }
            }
        }


        public static String RazorTemplRenderList(String razorTemplName, String moduleid, String cacheKey, List<NBrightInfo> objList, String lang)
        {
            // do razor template
            var cachekey = "NBrightModKey" + razorTemplName + "*" + moduleid + "*" + cacheKey + PortalSettings.Current.PortalId.ToString() + "*" + lang;
            var razorTempl = (String)Utils.GetCache(cachekey);
            if (razorTempl == null)
            {
                var settignInfo = GetSettings(moduleid);
                razorTempl = LocalUtils.GetTemplateData(razorTemplName, lang, settignInfo.ToDictionary());
                if (razorTempl != "")
                {
                    if (!objList.Any())
                    {
                        var obj = new NBrightInfo(true);
                        obj.ModuleId = Convert.ToInt32(moduleid);
                        obj.Lang = Utils.GetCurrentCulture();
                        objList.Add(obj);
                    }
                    var razorTemplateKey = "NBrightModKey" + moduleid + razorTemplName + PortalSettings.Current.PortalId.ToString();

                    var modRazor = new NBrightRazor(objList.Cast<object>().ToList(), settignInfo.ToDictionary(), HttpContext.Current.Request.QueryString);
                    razorTempl = RazorRender(modRazor, razorTempl, razorTemplateKey, settignInfo.GetXmlPropertyBool("genxml/checkbox/debugmode"));

                    Utils.SetCache(cachekey, razorTempl);
                    var modCacheList = (List<String>)Utils.GetCache("nbrightmodcache*" + moduleid);
                    if (modCacheList == null) modCacheList = new List<String>();
                    if (!modCacheList.Contains(cachekey)) modCacheList.Add(cachekey);
                    Utils.SetCache("nbrightmodcache*" + moduleid, modCacheList);
                }
            }
            return razorTempl;
        }

        public static String RazorTemplRender(String razorTemplName, String moduleid, String cacheKey, NBrightInfo obj, String lang)
        {
            // do razor template
            var cachekey = "NBrightModKey" + razorTemplName + "*" + moduleid + "*" + cacheKey + PortalSettings.Current.PortalId.ToString();
            var razorTempl = (String)Utils.GetCache(cachekey);
            if (razorTempl == null)
            {
                var settignInfo = GetSettings(moduleid);
                razorTempl = LocalUtils.GetTemplateData(razorTemplName, lang, settignInfo.ToDictionary());
                if (razorTempl != "")
                {
                    if (obj == null) obj = new NBrightInfo(true);
                    var razorTemplateKey = "NBrightModKey" + moduleid + settignInfo.GetXmlProperty("genxml/dropdownlist/themefolder") + razorTemplName + PortalSettings.Current.PortalId.ToString();

                    var l = new List<object>();
                    l.Add(obj);
                    var modRazor = new NBrightRazor(l, settignInfo.ToDictionary(), HttpContext.Current.Request.QueryString);
                    razorTempl = RazorRender(modRazor, razorTempl, razorTemplateKey, settignInfo.GetXmlPropertyBool("genxml/checkbox/debugmode"));

                    Utils.SetCache(cachekey, razorTempl);
                    var modCacheList = (List<String>)Utils.GetCache("nbrightmodcache*" + moduleid);
                    if (modCacheList == null) modCacheList = new List<String>();
                    if (!modCacheList.Contains(cachekey)) modCacheList.Add(cachekey);
                    Utils.SetCache("nbrightmodcache*" + moduleid, modCacheList);
                }
            }
            return razorTempl;
        }

        /// <summary>
        /// This method preprocesses the razor template, to add meta data required for selecting data into cache.
        /// </summary>
        /// <param name="fullTemplName">template name (Theme prefix is added from settings, if no theme prefix exists)</param>
        /// <param name="moduleid"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static Dictionary<String, String> RazorPreProcessTempl(String fullTemplName, String moduleid, String lang)
        {
            // see if we need to add theme to template name.
            var settignInfo = GetSettings(moduleid);
            var theme = settignInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
            if (fullTemplName.Split('.').Length == 2) fullTemplName = theme + "." + fullTemplName;

            // get cached data if there
            var cachedlist = (Dictionary<String, String>) Utils.GetCache("preprocessmetadata" + fullTemplName + moduleid);
            if (cachedlist != null) return cachedlist;

            // build cache data from template.
            cachedlist = new Dictionary<String, String>();
            var razorTempl = LocalUtils.GetTemplateData(fullTemplName, lang, settignInfo.ToDictionary());
            if (razorTempl != "")
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
                    razorTempl = RazorRender(modRazor, razorTempl, "preprocessmetadata" + fullTemplName + moduleid, settignInfo.GetXmlPropertyBool("genxml/checkbox/debugmode"));
                }
                catch (Exception ex)
                {
                    // Only log exception, could be a error because of missing data.  Thge preprocessing doesn't care.
                    Exceptions.LogException(ex);
                }
                cachedlist = (Dictionary<String, String>)Utils.GetCache("preprocessmetadata" + fullTemplName + moduleid);
            }
            return cachedlist;
        }



        public static void IncludePageHeaders(String moduleid, Page page, String moduleName,String templateprefix = "")
        {
            if (!page.Items.Contains("nbrightinject")) page.Items.Add("nbrightinject", "");
            var settignInfo = GetSettings(moduleid);
            var theme = settignInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
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

        public static String RazorRender(Object info, String razorTempl, String templateKey, Boolean debugMode = false)
        {
            // do razor test
            var config = new TemplateServiceConfiguration();
            config.Debug = debugMode;
            config.BaseTemplateType = typeof(NBrightModRazorTokens<>);
            var service = RazorEngineService.Create(config);
            Engine.Razor = service;

            var result = Engine.Razor.RunCompile(razorTempl, templateKey, null, info);
            return result;
        }


        #endregion

    }

}
