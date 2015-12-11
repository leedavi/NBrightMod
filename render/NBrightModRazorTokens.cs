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
using System.IO;

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
                if (modInfo != null) // might happen if invalid module data is imported
                {
                    strOut += "    <option value='" + tItem.GetXmlProperty("genxml/hidden/modref") + "' " + s + ">" + tItem.GetXmlProperty("genxml/hidden/modref") + " : " + modInfo.ModuleTitle + "</option>";
                }
            }
            strOut += "</select>";

            return new RawString(strOut);
        }

        public IEncodedString RenderTemplate(String templateRelPath, NBrightRazor model)
        {
            var TemplateData = "";
            var strOut = "";
            var templatePath = HttpContext.Current.Server.MapPath(templateRelPath);
            if (File.Exists(templatePath))
            {
                string inputLine;
                var inputStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
                var streamReader = new StreamReader(inputStream);

                while ((inputLine = streamReader.ReadLine()) != null)
                {
                    TemplateData += inputLine + Environment.NewLine;
                }
                streamReader.Close();
                inputStream.Close();

                if (TemplateData.Contains("AddPreProcessMetaData("))
                {
                    // do razor and cache preprocessmetadata
                    // Use the filename to link the preprocess data in cache, this shoud have been past as the param on the @AddPreProcessMetaData razor token in hte template.
                    var razorTempl = LocalUtils.RazorRender(model, TemplateData, "preprocessmetadata" + Path.GetFileName(templatePath), false);
                }

                strOut = LocalUtils.RazorRender(model, TemplateData, "", false);

            }

            return new RawString(strOut);
        }

        public IEncodedString ThemeSelectList(NBrightInfo info, String xpath, String relitiveRootFolder, String attributes = "", Boolean allowEmpty = true)
        {
            if (attributes.StartsWith("ResourceKey:")) attributes = ResourceKey(attributes.Replace("ResourceKey:", "")).ToString();

            var mappathRootFolder = System.Web.Hosting.HostingEnvironment.MapPath(relitiveRootFolder);
            var dirlist = System.IO.Directory.GetDirectories(mappathRootFolder);
            var tList = new List<String>();
            foreach (var d in dirlist)
            {
                var dr = new System.IO.DirectoryInfo(d);
                tList.Add(dr.Name);
            }
            var strOut = "";

            var upd = getUpdateAttr(xpath, attributes);
            var id = xpath.Split('/').Last();
            strOut = "<select id='" + id + "' " + upd + " " + attributes + ">";
            var c = 0;
            var s = "";
            if (allowEmpty) strOut += "    <option value=''></option>";
            foreach (var tItem in tList)
            {
                if (tItem.ToLower() != "shared" && tItem.ToLower() != "config")
                {
                    if (info.GetXmlProperty(xpath) == tItem)
                        s = "selected";
                    else
                        s = "";
                    strOut += "    <option value='" + tItem + "' " + s + ">" + tItem + "</option>";
                }
            }
            strOut += "</select>";

            return new RawString(strOut);
        }

        public IEncodedString EditButtons(String displaybuttonlist = "", String extraclass = "")
        {
            var strOut = "<div class='buttons " + extraclass + "' >";
            var buttons = displaybuttonlist.Split(',');
            foreach (var button in buttons)
            {
                if (button.ToLower() == "add" || button.ToLower() == "a")
                {
                    strOut += "<button id='addnew' type='button' class='btn btn-primary'><span class='glyphicon glyphicon-plus'></span> " + ResourceKey("Edit.addnew") + "</button>";
                }
                if (button.ToLower() == "savelist" || button.ToLower() == "sl")
                {
                    strOut += "<button id='savelistdata' type='button' class='btn btn-success'><span class='glyphicon glyphicon-save'></span> " + ResourceKey("Edit.save") + "</button>";
                }
                if (button.ToLower() == "save" || button.ToLower() == "s")
                {
                    strOut += "<button id='savedata' type='button' class='btn btn-success'><span class='glyphicon glyphicon-save'></span> " + ResourceKey("Edit.save") + "</button>";
                }
                if (button.ToLower() == "saveexit" || button.ToLower() == "sx")
                {
                    strOut += "<button id='savedataexit' type='button' class='btn btn-success'><span class='glyphicon glyphicon-save'></span> " + ResourceKey("Edit.saveexit") + "</button>";
                }
                if (button.ToLower() == "savereturn" || button.ToLower() == "sr")
                {
                    strOut += "<button id='savedatareturn' type='button' class='btn btn-success'><span class='glyphicon glyphicon-save'></span> " + ResourceKey("Edit.saveexit") + "</button>";
                }
                if (button.ToLower() == "delete" || button.ToLower() == "d")
                {
                    strOut += "<button id='delete' type='button' class='btn btn-danger'><span class='glyphicon glyphicon-remove'></span> " + ResourceKey("Edit.delete") + "</button>";
                }
                if (button.ToLower() == "return" || button.ToLower() == "r")
                {
                    strOut += "<button id='return' type='button' class='btn btn-default'><span class='glyphicon glyphicon-log-out'></span> " + ResourceKey("Edit.return") + "</button>";
                }
                if (button.ToLower() == "exit" || button.ToLower() == "ex")
                {
                    strOut += "<button id='exitedit' type='button' class='btn btn-default'><span class='glyphicon glyphicon-log-out'></span> " + ResourceKey("Edit.exit") + "</button>";
                }
                if (button.ToLower() == "uploadimage" )
                {
                    strOut += "<button type='button' class='btn btn-sm btn-success imageclick' data-toggle='modal' data-target='#NBrightModModal' ><span class='glyphicon glyphicon-folder-open'></span> " + ResourceKey("Edit.browse") + "</button>";
                }
                if (button.ToLower() == "uploaddoc")
                {
                    strOut += "<button type='button' class='btn btn-sm btn-success fileclick' data-toggle='modal' data-target='#NBrightModModal' ><span class='glyphicon glyphicon-folder-open'></span> " + ResourceKey("Edit.browse") + "</button>";
                }
                if (button.ToLower() == "undodoc")
                {
                    strOut += "<button id='undodoc' type='button' class='btn btn-sm btn-warning' style='display: none;'><span class='glyphicon glyphicon-minus'></span> " + ResourceKey("Edit.undo") + "</button>";
                }
                if (button.ToLower() == "undoimage")
                {
                    strOut += "<button id='undoimage' type='button' class='btn btn-sm btn-warning' style='display: none;'><span class='glyphicon glyphicon-minus'></span> " + ResourceKey("Edit.undo") + "</button>";
                }
            }
            strOut += "</div>";


            return new RawString(strOut);
        }

        public IEncodedString IconButtons(NBrightInfo info, String displaybuttonlist = "", String extraclass = "")
        {
            var strOut = "<div class='actionbuttons " + extraclass + "' >";
            var buttons = displaybuttonlist.Split(',');
            foreach (var button in buttons)
            {
                if (button.ToLower() == "edit" || button.ToLower() == "e")
                {
                    strOut += "<button type='button' class='btn btn-sm btn-primary edititem' itemid='" + info.ItemID + "' data-tooltip='tooltip' data-placement='top' data-original-title='" + ResourceKey("Edit.edit") + "'><span class='glyphicon glyphicon-pencil' aria-hidden='true'></span></button>";
                }
                if (button.ToLower() == "uploadimage" || button.ToLower() == "ui")
                {
                    strOut += "<button type='button' class='btn btn-sm btn-success imagelistclick'  data-tooltip='tooltip' itemid='" + info.ItemID + "' data-toggle='modal' data-target='#NBrightModModal' data-placement='top' data-original-title='" + ResourceKey("Edit.browse") + "'><span class='glyphicon glyphicon-folder-open' aria-hidden='true'></span></button>";
                }
                if (button.ToLower() == "sortup" || button.ToLower() == "su")
                {
                    strOut += "<button type='button' class='btn btn-sm sortelementUp' data-tooltip='tooltip' data-placement='top' data-original-title='" + ResourceKey("Edit.moveup") + "'><span class='glyphicon glyphicon-arrow-up' aria-hidden='true'></span></button>";
                }
                if (button.ToLower() == "sortdown" || button.ToLower() == "sd")
                {
                    strOut += "<button type='button' class='btn btn-sm sortelementDown' data-tooltip='tooltip' data-placement='top' data-original-title='" + ResourceKey("Edit.movedown") + "'><span class='glyphicon glyphicon-arrow-down' aria-hidden='true'></span></button>";
                }
                if (button.ToLower() == "deleteelement" || button.ToLower() == "de")
                {
                    strOut += "<button type='button' class='btn btn-sm btn-danger removeelement' itemid='" + info.ItemID + "' data-tooltip='tooltip' data-placement='top' data-original-title='" + ResourceKey("Edit.delete") + "'><span class='glyphicon glyphicon-remove' aria-hidden='true'></span></button>";
                }
                if (button.ToLower() == "uploaddoc" || button.ToLower() == "ud")
                {
                    strOut += "<button type='button' class='btn btn-sm btn-success filelistclick'  data-tooltip='tooltip' itemid='" + info.ItemID + "' data-toggle='modal' data-target='#NBrightModModal' data-placement='top' data-original-title='" + ResourceKey("Edit.browse") + "'><span class='glyphicon glyphicon-folder-open'></span></button>";
                }
                if (button.ToLower() == "deleteitem" || button.ToLower() == "di")
                {
                    strOut += "<button type='button' class='btn btn-sm btn-danger deleteitemclick' itemid='" + info.ItemID + "' data-tooltip='tooltip' data-placement='top' data-original-title='" + ResourceKey("Edit.delete") + "'><span class='glyphicon glyphicon-remove'></span></button>";
                }
            }
            strOut += "</div>";


            return new RawString(strOut);
        }


    }


}
