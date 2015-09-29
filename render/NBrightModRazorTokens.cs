using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using DotNetNuke.Collections;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Localization;
using NBrightCore.common;
using NBrightCore.providers;
using NBrightCore.render;
using NBrightDNN;
using NBrightDNN.render;
using NBrightMod.common;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace NBrightMod.render
{
    public class NBrightModRazorTokens<T> : RazorEngineTokens<T>
    {

        public IEncodedString NBrightModSelectList(NBrightInfo info, String xpath, String attributes = "", Boolean allowEmpty = true)
        {
            if (attributes.StartsWith("ResourceKey:")) attributes = ResourceKey(attributes.Replace("ResourceKey:", "")).ToString();

            var modList = LocalUtils.GetNBrightModList();
            var strOut = "";
            var selectedmodref = info.GetXmlProperty(xpath);
            if (selectedmodref == "") selectedmodref = info.GetXmlProperty("genxml/hidden/modref");
            var upd = getUpdateAttr(xpath, attributes);
            var id = xpath.Split('/').Last();
            strOut = "<select id='" + id + "' " + upd + " " + attributes + ">";
            var c = 0;
            var s = "";
            if (allowEmpty) strOut += "    <option value=''></option>";
            foreach (var tItem in modList)
            {
                if (selectedmodref == tItem.GetXmlProperty("genxml/hidden/modref"))
                    s = "selected";
                else
                    s = "";
                var modInfo = DnnUtils.GetModuleinfo(tItem.ModuleId);
                strOut += "    <option value='" + tItem.GetXmlProperty("genxml/hidden/modref") + "' " + s + ">" + tItem.GetXmlProperty("genxml/hidden/modref") + " : " + modInfo.ModuleTitle + "</option>";
            }
            strOut += "</select>";

            return new RawString(strOut);
        }


    }


}
