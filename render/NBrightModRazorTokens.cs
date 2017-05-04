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
using DotNetNuke.Entities.Modules;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Security.Roles;

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
            var s = "";
            if (allowEmpty) strOut += "    <option value=''></option>";
            foreach (var tItem in modList)
            {
                if (selectedmodref == tItem.GetXmlProperty("genxml/hidden/modref"))
                    s = "selected";
                else
                    s = "";

                var ident = tItem.GetXmlProperty("genxml/ident");
                if (ident == "") ident = tItem.GetXmlProperty("genxml/hidden/modref");
                strOut += "    <option value='" + tItem.GetXmlProperty("genxml/hidden/modref") + "' " + s + ">" + ident + "</option>";
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

                //BEGIN: INJECT RESX: assume we always want the resx paths adding
                TemplateData = " @AddMetaData(\"resourcepath\",\"/DesktopModules/NBright/NBrightMod/App_LocalResources\") " + TemplateData;
                TemplateData = " @AddMetaData(\"resourcepath\",\"/DesktopModules/NBright/NBrightMod/Themes/" + model.GetSetting("themefolder") + "/resx\") " + TemplateData;
                TemplateData = " @AddMetaData(\"resourcepath\",\"/" + PortalSettings.Current.HomeDirectory.Trim('/') + "/NBrightMod/Themes/" + model.GetSetting("themefolder") + "/resx\") " + TemplateData;
                //END: INJECT RESX

                if (TemplateData.Contains("AddPreProcessMetaData("))
                {
                    // do razor and cache preprocessmetadata
                    // Use the filename to link the preprocess data in cache, this shoud have been past as the param on the @AddPreProcessMetaData razor token in hte template.
                    var razorTempl = LocalUtils.RazorRender(model, TemplateData, "preprocessmetadata" + Path.GetFileName(templatePath), false);
                }

                strOut = LocalUtils.RazorRender(model, TemplateData, model.GetSetting("themefolder") + "." + Path.GetFileName(templatePath), false);

            }

            return new RawString(strOut);
        }

        public IEncodedString ThemeSelectList(NBrightInfo info, String xpath, String relitiveRootFolder, String attributes = "", Boolean allowEmpty = true)
        {
            if (attributes.StartsWith("ResourceKey:")) attributes = ResourceKey(attributes.Replace("ResourceKey:", "")).ToString();

            var tList = new List<String>();

            var mappathRootFolder = System.Web.Hosting.HostingEnvironment.MapPath(relitiveRootFolder);
            if (mappathRootFolder != null && Directory.Exists(mappathRootFolder))
            {
                var dirlist = System.IO.Directory.GetDirectories(mappathRootFolder);
                foreach (var d in dirlist)
                {
                    var dr = new System.IO.DirectoryInfo(d);
                    tList.Add(dr.Name);
                }
            }

            // add portal themes
            var mappathRootFolder2 = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes";
            if (Directory.Exists(mappathRootFolder2))
            {
                var dirlist2 = System.IO.Directory.GetDirectories(mappathRootFolder2);
                foreach (var d in dirlist2)
                {
                    var dr = new System.IO.DirectoryInfo(d);
                    if (!tList.Contains(dr.Name)) tList.Add(dr.Name);
                }
            }

            var strOut = "";

            var upd = getUpdateAttr(xpath, attributes);
            var id = xpath.Split('/').Last();
            strOut = "<select id='" + id + "' " + upd + " " + attributes + ">";
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

        public IEncodedString ThemePortalSelectList(NBrightInfo info, String xpath, String attributes = "", Boolean allowEmpty = true)
        {
            if (attributes.StartsWith("ResourceKey:")) attributes = ResourceKey(attributes.Replace("ResourceKey:", "")).ToString();

            var tList = new List<String>();
            var mappathRootFolder = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\Themes";
            if (Directory.Exists(mappathRootFolder))
            {
                var dirlist = System.IO.Directory.GetDirectories(mappathRootFolder);
                foreach (var d in dirlist)
                {
                    var dr = new System.IO.DirectoryInfo(d);
                    tList.Add(dr.Name);
                }
            }
            var strOut = "";

            var upd = getUpdateAttr(xpath, attributes);
            var id = xpath.Split('/').Last();
            strOut = "<select id='" + id + "' " + upd + " " + attributes + ">";
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

        public IEncodedString GetSnippets(String cssclass, String cssclassli, String headerli = "", String xpath = "")
        {
            var snippetXml = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/config/default/snippets.xml");
            var XMLDoc = new XmlDocument();
            XMLDoc.Load(snippetXml);
            var nodList = XMLDoc.SelectNodes(xpath);

            var strOut = new StringBuilder("");
            if (nodList != null)
            {
                strOut.Append("<ul class='" + cssclass + "'>");
                strOut.Append(headerli);
                foreach (XmlNode n in nodList)
                {
                    var titledtata = n.Attributes["name"].InnerText;
                    if (n.Attributes["title"] != null && n.Attributes["title"].InnerText != "")
                    {
                        titledtata = n.Attributes["title"].InnerText;
                    }
                    strOut.Append("<li>");
                    strOut.Append("<a href='javascript:void(0)' snipname='snip" + n.Attributes["name"].InnerText + "' class='selectsnippet' " + cssclassli + "' title='" + titledtata + "'>" + n.Attributes["text"].InnerText + "</a>");
                    strOut.Append("<span id='snip" + n.Attributes["name"].InnerText + "' style='display:none;' >" + n.InnerText + "</span>");
                    strOut.Append("</li>");
                }
                strOut.Append("</ul>");
            }
            return new RawString(strOut.ToString());
        }

        public IEncodedString TreeViewTabsFancyTreeClones(String moduleid)
        {
            var strOut = ""; 
            if (Utils.IsNumeric(moduleid))
            {
                var selectedlist = "";
                var objmodules = new ModuleController();
                var tlist = objmodules.GetAllTabsModulesByModuleID(Convert.ToInt32(moduleid));
                foreach (ModuleInfo t in tlist)
                {
                    selectedlist += t.TabID + ",";
                }

                strOut = DnnUtils.GetTreeViewTabJSData(selectedlist);
            }
            return new RawString(strOut);
        }

        public IEncodedString TreeViewTabsFancyTreeRoles(string roleid, int portalID)
        {
            var strOut = "";
            var selectedlist = "";
            var objmodules = new ModuleController();

            var mlist = objmodules.GetAllTabsModules(portalID, false);
            foreach (ModuleInfo t in mlist)
            {
                if (t.ModuleDefinition.DefinitionName == "NBrightMod")
                {
                    var roleexist = false;
                    var permissionsList2 = t.ModulePermissions.ToList();
                    foreach (var p in permissionsList2)
                    {
                        if (Utils.IsNumeric(roleid) && p.RoleID == Convert.ToInt32(roleid)) roleexist = true;
                    }
                    if (roleexist)
                    {
                        selectedlist += t.TabID + ",";
                    }
                }
            }

            strOut = DnnUtils.GetTreeViewTabJSData(selectedlist).Replace("treeData", "roletreeData");
            return new RawString(strOut);
        }

        public IEncodedString RolesDropDownList(NBrightInfo info, String xpath, String attributes = "", String defaultValue = "")
        {
            if (attributes.StartsWith("ResourceKey:")) attributes = ResourceKey(attributes.Replace("ResourceKey:", "")).ToString();
            if (defaultValue.StartsWith("ResourceKey:")) defaultValue = ResourceKey(defaultValue.Replace("ResourceKey:", "")).ToString();

            var strOut = "";

            var roles = RoleController.Instance.GetRoles(PortalSettings.Current.PortalId);

            var datavalue = "";
            var datatext = "";
            foreach (var role in roles)
            {
                datavalue += role.RoleID + ",";
                datatext += role.RoleName + ",";
            }
            datavalue = datavalue.TrimEnd(',');
            datatext = datatext.TrimEnd(',');

            var datav = datavalue.Split(',');
            var datat = datatext.Split(',');
            if (datav.Count() == datat.Count())
            {
                var upd = getUpdateAttr(xpath, attributes);
                var id = getIdFromXpath(xpath);
                strOut = "<select id='" + id + "' " + upd + " " + attributes + ">";
                var c = 0;
                var s = "";
                var value = info.GetXmlProperty(xpath);
                if (value == "") value = defaultValue;
                foreach (var v in datav)
                {
                    if (value == v)
                        s = "selected";
                    else
                        s = "";

                    strOut += "    <option value='" + v + "' " + s + ">" + datat[c] + "</option>";
                    c += 1;
                }
                strOut += "</select>";
            }
            return new RawString(strOut);
        }


        public IEncodedString TemplateFileSelect(NBrightInfo info, String cssclass, String cssclassli, String headerli = "",String filematchcsv = "")
        {
            var nodList = info.XMLDoc.SelectNodes("genxml/files/file");

            var strOut = new StringBuilder("");
            if (nodList != null)
            {
                filematchcsv = filematchcsv.Trim() + ",";
                strOut.Append("<ul class='" + cssclass + "'>");
                strOut.Append(headerli);
                foreach (XmlNode n in nodList)
                {
                    if (filematchcsv == "," || filematchcsv.Contains(n.InnerText + ","))
                    {
                        var cssclassli2 = cssclassli;
                        if (isPortalTemplate(info, n.InnerText)) cssclassli2 += " isportaltemplate";
                        if (isModuleTemplate(info, n.InnerText)) cssclassli2 += " ismoduletemplate";
                        strOut.Append("<li>");
                        strOut.Append("<a href='javascript:void(0)' filename='" + n.InnerText + "' class='selectfiletemplate " + cssclassli2 + "'>" + n.InnerText + "</a>");
                        strOut.Append("</li>");
                    }
                }
                strOut.Append("</ul>");
            }
            return new RawString(strOut.ToString());
        }

        public Boolean IsPortalTemplate(NBrightInfo info, String filename)
        {
            if (isPortalTemplate(info,filename)) return true;
            return false;
        }

        private Boolean isPortalTemplate(NBrightInfo info, String filename)
        {
            var lang = info.GetXmlProperty("genxml/editlang");
            var nod = info.XMLDoc.SelectSingleNode("genxml/portalfiles[file='" + lang + filename + "']");
            if (nod != null) return true;
            if (!info.GetXmlPropertyBool("genxml/systemtheme")) return true;
            return false;
        }

        public Boolean IsModuleTemplate(NBrightInfo info, String filename)
        {
            if (isModuleTemplate(info, filename)) return true;
            return false;
        }

        private Boolean isModuleTemplate(NBrightInfo info, String filename)
        {
            var lang = info.GetXmlProperty("genxml/editlang");
            var nod = info.XMLDoc.SelectSingleNode("genxml/modulefiles[file='" + lang + filename + "']");
            if (nod != null)
            {
                return true;
            }
            return false;
        }

        public Boolean IsModuleDefaultTemplate(NBrightInfo info, String filename)
        {
            if (isModuleDefaultTemplate(info, filename)) return true;
            return false;
        }
        private Boolean isModuleDefaultTemplate(NBrightInfo info, String filename)
        {
            if (info.GetXmlProperty("genxml/editlang") == "" || info.GetXmlProperty("genxml/editlang") == "none") return false;
            var nod = info.XMLDoc.SelectSingleNode("genxml/modulefiles[file='" + filename + "']");
            if (nod != null)
            {
                return true;
            }
            return false;
        }

        public Boolean IsPortalDefaultTemplate(NBrightInfo info, String filename)
        {
            if (isPortalDefaultTemplate(info, filename)) return true;
            return false;
        }

        private Boolean isPortalDefaultTemplate(NBrightInfo info, String filename)
        {
            if (info.GetXmlProperty("genxml/editlang") == "" || info.GetXmlProperty("genxml/editlang") == "none") return false;
            var nod = info.XMLDoc.SelectSingleNode("genxml/portalfiles[file='" + filename + "']");
            if (nod != null) return true;
            return false;
        }

    }


}
