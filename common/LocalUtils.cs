


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Xml;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
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
            var xmlData = GenXmlFunctions.GetGenXmlByAjax(strIn, "");
            var objInfo = new NBrightInfo();

            objInfo.ItemID = -1;
            objInfo.TypeCode = "AJAXDATA";
            objInfo.XMLData = xmlData;
            return objInfo;
        }

        public static List<NBrightInfo> GetGenXmlListByAjax(string xmlAjaxData, string originalXml, string lang = "en-US", string xmlRootName = "genxml")
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
                var dataRecord = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), "SETTINGS", "NBrightMod");
                if (dataRecord == null) dataRecord = new NBrightInfo(true);
                return dataRecord;
            }
            return new NBrightInfo(true);
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
                    if (!objList.Any()) objList.Add(new NBrightInfo(true));
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
                    var razorTemplateKey = "NBrightModKey" + moduleid + razorTemplName + PortalSettings.Current.PortalId.ToString();

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



        public static void IncludePageHeaders(String moduleid, Page page, String moduleName)
        {
            if (!page.Items.Contains("nbrightinject")) page.Items.Add("nbrightinject", "");
            if (!page.Items["nbrightinject"].ToString().Contains(moduleName + ","))
            {
                var nbi = new NBrightInfo();
                nbi.Lang = Utils.GetCurrentCulture();
                var razorTempl = RazorTemplRender("pageheader.cshtml", moduleid, Utils.GetCurrentCulture(), nbi, Utils.GetCurrentCulture());
                if (razorTempl != "")
                {
                    PageIncludes.IncludeTextInHeader(page, razorTempl);
                    page.Items["nbrightinject"] = page.Items["nbrightinject"] + moduleName + ",";
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
