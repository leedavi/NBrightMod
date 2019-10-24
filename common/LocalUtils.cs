


using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.Caching;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
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
using System.Web.UI.WebControls;
using System.Windows.Forms;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Security.Roles;
using System.Net;
using System.Text.RegularExpressions;

namespace NBrightMod.common
{
    public enum AuditCode { Reset, Request, Delete, Validate, Decline, ClearAudit};

    public static class LocalUtils
    {
        #region "Version Control"

        public static bool HasVersion(int ModuleId)
        {
            var objCtrl = new NBrightDataController();
            var c = objCtrl.GetListCount(PortalSettings.Current.PortalId, ModuleId, "vNBrightModDATA");
            c += objCtrl.GetListCount(PortalSettings.Current.PortalId, ModuleId, "aNBrightModDATA");
            c += objCtrl.GetListCount(PortalSettings.Current.PortalId, ModuleId, "vNBrightModHEADER");
            c += objCtrl.GetListCount(PortalSettings.Current.PortalId, ModuleId, "aNBrightModHEADER");

            if (c > 0) return true;
            return false;
        }

        public static bool VersionUserCanValidate(int moduleid)
        {
            if (UserController.Instance.GetCurrentUserInfo().IsInRole("Manager") || UserController.Instance.GetCurrentUserInfo().IsInRole("Administrators"))
            {
                // manager and admin can always update without creating a verison.
                return true;
            }

            var moduleInfo = DnnUtils.GetModuleinfo(moduleid);
            if (moduleInfo != null)
            {
                var validateRoles = new List<string>();
                var permissionsList2 = moduleInfo.ModulePermissions.ToList();
                foreach (var p in permissionsList2)
                {
                    if (p.RoleName.StartsWith("Validate"))
                    {
                        validateRoles.Add(p.RoleName);
                    }
                }
                if (validateRoles.Count >= 1)
                {
                    foreach (var rolename in validateRoles)
                    {
                        if (UserController.Instance.GetCurrentUserInfo().IsInRole(rolename)) return true;
                    }
                }

            }

            return false;
        }

        public static bool VersionUserMustCreateVersion(int moduleid)
        {
            if (VersionUserCanValidate(moduleid))
            {
                // user can validate, so we don;t need to create a verison.
                return false;
            }

            var moduleInfo = DnnUtils.GetModuleinfo(moduleid);
            if (moduleInfo != null)
            {
                var versionRoles = new List<string>();
                var permissionsList2 = moduleInfo.ModulePermissions.ToList();
                foreach (var p in permissionsList2)
                {
                    if (p.RoleName.StartsWith("Version"))
                    {
                        versionRoles.Add(p.RoleName);
                    }
                }
                if (versionRoles.Count >= 1)
                {
                    foreach (var rolename in versionRoles)
                    {
                        if (UserController.Instance.GetCurrentUserInfo().IsInRole(rolename)) return true;
                    }
                }
            }

            return false; // user does NOT have a "Version*" role for this module, so assume they can validate.
        }


        /// <summary>
        /// Get the data version record
        /// </summary>
        /// <param name="nbrightInfo">original data record</param>
        /// <returns>The version record or the orginal if no version</returns>
        public static NBrightInfo VersionGet(NBrightInfo nbrightInfo, string entityType = "NBrightModDATA")
        {
            if (nbrightInfo != null)
            {
                var baseid = nbrightInfo.XrefItemId;
                if (nbrightInfo.TypeCode.StartsWith("v" + entityType))
                {
                    baseid = nbrightInfo.ItemID;
                }
                var objCtrl = new NBrightDataController();
                var nbi = objCtrl.GetData(baseid);
                if (nbi == null)
                {
                    // if null then there is an issue with the DB xrefitemid
                    nbrightInfo.XrefItemId = 0;
                    objCtrl.Update(nbrightInfo);
                    return nbrightInfo;
                }
                return nbi;
            }
            return nbrightInfo;
        }
        /// <summary>
        /// Create Version of Data record
        /// </summary>
        /// <param name="nbrightInfo">original data record</param>
        /// <returns>The version record</returns>
        public static NBrightInfo VersionUpdate(NBrightInfo nbrightInfo, string entityType = "NBrightModDATA")
        {
            var etypecode = nbrightInfo.TypeCode;
            var pitemId = nbrightInfo.ParentItemId;
            var baseid = nbrightInfo.XrefItemId;
            if (nbrightInfo.TypeCode.StartsWith("v" + entityType) || nbrightInfo.TypeCode.StartsWith("a" + entityType))
            {
                baseid = nbrightInfo.ItemID;
            }

            var objCtrl = new NBrightDataController();
            var insertnewversion = true;
            if (nbrightInfo.XrefItemId > 0 || nbrightInfo.TypeCode.StartsWith("v" + entityType) || nbrightInfo.TypeCode.StartsWith("a" + entityType))
            {
                var nbi = objCtrl.Get(baseid);
                if (nbi != null && nbi.XrefItemId != nbi.ItemID)
                {
                    // update existing verison
                    nbrightInfo.ItemID = nbi.ItemID;
                    nbrightInfo.XrefItemId = nbi.XrefItemId;
                    nbrightInfo.ParentItemId = nbi.ParentItemId;
                    if (nbrightInfo.TypeCode.StartsWith(entityType)) nbrightInfo.TypeCode = "v" + nbrightInfo.TypeCode;
                    objCtrl.Update(nbrightInfo);
                    insertnewversion = false;
                }
                else
                {
                    // invalid itemid, clear 
                    nbrightInfo.XrefItemId = 0;
                    objCtrl.Update(nbrightInfo);
                }
            }

            if (insertnewversion)
            {
                var nbi = objCtrl.Get(nbrightInfo.ItemID);
                if (nbi != null)
                {
                    // create new verison
                    if (nbrightInfo.TypeCode.StartsWith(entityType)) nbrightInfo.TypeCode = "v" + nbrightInfo.TypeCode;
                    nbrightInfo.XrefItemId = nbi.ItemID;
                    nbrightInfo.ItemID = -1;
                    if (nbrightInfo.ParentItemId > 0)
                    {
                        var pnbi = objCtrl.Get(nbrightInfo.ParentItemId);
                        nbrightInfo.ParentItemId = pnbi.XrefItemId;
                    }

                    var verisonId = objCtrl.Update(nbrightInfo);
                    nbrightInfo.ItemID = verisonId;

                    // update original with versionid
                    nbi.XrefItemId = verisonId;
                    objCtrl.Update(nbi);

                    // read all unchanged LANG records and create version record.
                    if (nbrightInfo.Lang != "")
                    {
                        var langlist = objCtrl.GetList(nbrightInfo.PortalId, -1, etypecode, "and NB1.ParentItemId = '" + pitemId + "' and NB1.Lang != '" + nbrightInfo.Lang + "' ");
                        foreach (var langnbi in langlist)
                        {
                            if (langnbi.XrefItemId == 0) // only if not created
                            {
                                var langitemid = langnbi.ItemID;
                                var newlangnbi = langnbi;
                                newlangnbi.TypeCode = "v" + etypecode;
                                newlangnbi.XrefItemId = langnbi.ItemID;
                                newlangnbi.ItemID = -1;
                                if (newlangnbi.ParentItemId > 0)
                                {
                                    var pnbi = objCtrl.Get(newlangnbi.ParentItemId);
                                    newlangnbi.ParentItemId = pnbi.XrefItemId;
                                }
                                var langnbi2 = objCtrl.Get(langitemid);
                                langnbi2.XrefItemId = objCtrl.Update(newlangnbi);
                                objCtrl.Update(langnbi2);
                            }

                            if (langnbi.XrefItemId >= 0) // fix any linked record that should not be.
                            {
                                var dummy = objCtrl.Get(langnbi.XrefItemId);
                                if (dummy == null)
                                {
                                    langnbi.XrefItemId = 0;
                                    objCtrl.Update(langnbi);
                                }

                            }

                        }
                    }
                }
            }
            return nbrightInfo;
        }

        public static void DoVersionDelete(int ModuleId, string entityType)
        {
            // DELETE verison changes
            var objCtrl = new NBrightDataController();
            var l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, entityType);
            var l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, entityType + "LANG");
            // lang records first, so we don;t auto delete lang on base record delete.
            foreach (var nbi in l2)
            {
                LocalUtils.VersionDelete(nbi);
            }
            foreach (var nbi in l)
            {
                if (nbi.GetXmlPropertyBool("genxml/versiondelete"))
                {
                    // remove deleted flag from data record.
                    nbi.RemoveXmlNode("genxml/versiondelete");
                    objCtrl.Update(nbi);
                }
                LocalUtils.VersionDelete(nbi);
            }

            l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "a"+ entityType);
            l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "a" + entityType + "LANG");
            // lang records first, so we don;t auto delete lang on base record delete.
            foreach (var nbi in l2)
            {
                LocalUtils.VersionDelete(nbi);
            }
            foreach (var nbi in l)
            {
                LocalUtils.VersionDelete(nbi);
            }

            // DELETE any record that remain (Corrupted records.)
            l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "v" + entityType);
            l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "v" + entityType + "LANG");
            // lang records first, so we don;t auto delete lang on base record delete.
            foreach (var nbi in l2)
            {
                objCtrl.Delete(nbi.ItemID);
            }
            foreach (var nbi in l)
            {
                objCtrl.Delete(nbi.ItemID);
            }
            // DELETE any record that remain (Corrupted records.)
            l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "a" + entityType);
            l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "a" + entityType + "LANG");
            // lang records first, so we don;t auto delete lang on base record delete.
            foreach (var nbi in l2)
            {
                objCtrl.Delete(nbi.ItemID);
            }
            foreach (var nbi in l)
            {
                objCtrl.Delete(nbi.ItemID);
            }

        }

        public static void DoValidate(int ModuleId,string entitytype)
        {
            // accept verison changes
            var objCtrl = new NBrightDataController();
            var l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, entitytype);
            var l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, entitytype + "LANG");
            // lang records first, so we don;t auto delete lang on base record delete.
            foreach (var nbi in l2)
            {
                LocalUtils.VersionValidate(nbi, entitytype);
            }
            foreach (var nbi in l)
            {
                if (nbi.GetXmlPropertyBool("genxml/versiondelete"))
                {
                    // remove deleted data record.
                    objCtrl.Delete(nbi.XrefItemId);
                    objCtrl.Delete(nbi.ItemID);
                }
                LocalUtils.VersionValidate(nbi, entitytype);
            }
            l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "a" + entitytype);
            l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "a" + entitytype + "LANG");
            // lang records first, so we don;t auto delete lang on base record delete.
            foreach (var nbi in l2)
            {
                LocalUtils.VersionValidate(nbi, entitytype);
            }
            foreach (var nbi in l)
            {
                LocalUtils.VersionValidate(nbi, entitytype);
            }
            LocalUtils.VersionSendEmail(ModuleId, "version-email-validate.cshtml");
            LocalUtils.VersionAuditLog(ModuleId, AuditCode.Validate);
        }

        /// <summary>
        /// Delete version record
        /// </summary>
        /// <param name="nbrightInfo">original data record</param>
        public static void VersionDelete(NBrightInfo nbrightInfo, string entityType = "NBrightModDATA")
        {
            var objCtrl = new NBrightDataController();
            if (nbrightInfo.TypeCode.StartsWith("a" + entityType))
            {
                objCtrl.Delete(nbrightInfo.ItemID);
            }
            else
            {
                var nbi = objCtrl.GetData(nbrightInfo.XrefItemId);
                if (nbi != null && (nbi.TypeCode.StartsWith("v" + entityType))) // only delete version records. 
                {
                    objCtrl.Delete(nbi.ItemID);
                    nbrightInfo.XrefItemId = 0;
                    objCtrl.Update(nbrightInfo);
                }
            }
        }

        /// <summary>
        /// Validate the version record, delete orignial data, activates version data.
        /// </summary>
        /// <param name="nbrightInfo">original data record</param>
        public static void VersionValidate(NBrightInfo nbrightInfo, string entityType = "NBrightModDATA")
        {
            var objCtrl = new NBrightDataController();
            if (nbrightInfo.TypeCode.StartsWith("a" + entityType))
            {
                nbrightInfo.TypeCode = nbrightInfo.TypeCode.Replace("a" + entityType, entityType);
                objCtrl.Update(nbrightInfo);
            }
            else
            {
                var nbi = objCtrl.GetData(nbrightInfo.XrefItemId);
                if (nbi != null && (nbi.TypeCode.StartsWith("v" + entityType)))
                {
                    nbi.XrefItemId = 0;
                    nbi.TypeCode = nbi.TypeCode.Replace("v" + entityType, entityType);
                    objCtrl.Update(nbi);
                    objCtrl.Delete(nbrightInfo.ItemID);
                }
            }
        }

        public static void VersionMarkDeleted(NBrightInfo nbrightInfo, string entityType = "NBrightModDATA")
        {
            var objCtrl = new NBrightDataController();
            if (nbrightInfo.TypeCode.StartsWith(entityType))
            {
                nbrightInfo.SetXmlProperty("genxml/versiondelete","True");
                objCtrl.Update(nbrightInfo);
                // create a version, so we know we have version change.
                VersionUpdate(nbrightInfo);
            }
            else
            {
                // base record
                var nbi = objCtrl.Get(nbrightInfo.XrefItemId);
                if (nbi != null)
                {
                    // lang record
                    var l = objCtrl.GetList(nbi.PortalId, nbi.ModuleId, entityType + "LANG", " and NB1.ParentItemId = " + nbi.ItemID + " ");
                    if (l.Any())
                    {
                        foreach (var nbiCleanLang in l)
                        {
                            if (nbiCleanLang.ParentItemId == nbi.ItemID)
                            {
                                nbiCleanLang.XrefItemId = 0;
                                objCtrl.Update(nbiCleanLang);
                            }
                        }
                    }
                    // mark base record as deleted
                    nbi.XrefItemId = 0; // clear removed versionid
                    nbi.SetXmlProperty("genxml/versiondelete", "True");
                    objCtrl.Update(nbi);
                    objCtrl.Delete(nbrightInfo.ItemID);
                    // create a version, so we know we have version change.
                    VersionUpdate(nbi);
                }
                else
                {
                    // record must have been added as version, just remove it.
                    objCtrl.Delete(nbrightInfo.ItemID);
                }
            }
        }

        public static void VersionSendEmail(int moduleid, string emailtemplatename)
        {
            var nbiconfig = LocalUtils.GetConfig(false);
            if (nbiconfig.GetXmlPropertyBool("genxml/checkbox/versionemails"))
            {

                var nbi = LocalUtils.GetSettings(moduleid.ToString());
                var objCtrl = new NBrightDataController();
                if (emailtemplatename.ToLower() == "version-email-delete.cshtml" || emailtemplatename.ToLower() == "version-email-validate.cshtml" || emailtemplatename.ToLower() == "version-email-decline.cshtml")
                {
                    nbi.RemoveXmlNode("genxml/versionemailsent");
                    nbi.RemoveXmlNode("genxml/versiondisplayname");
                    nbi.RemoveXmlNode("genxml/versionusername");
                    objCtrl.Update(nbi);
                }

                nbi.ModuleId = moduleid;

                var moduleInfo = DnnUtils.GetModuleinfo(moduleid);
                if (moduleInfo != null)
                {
                    // set flag for email sent
                    if (emailtemplatename.ToLower() == "version-email-new.cshtml")
                    {
                        nbi.SetXmlProperty("genxml/versionemailsent", "True");
                        nbi.SetXmlProperty("genxml/versionurl", DotNetNuke.Common.Globals.NavigateURL(moduleInfo.TabID));
                        nbi.SetXmlProperty("genxml/versiondisplayname", UserController.Instance.GetCurrentUserInfo().DisplayName);
                        nbi.SetXmlProperty("genxml/versionusername", UserController.Instance.GetCurrentUserInfo().Username);
                        objCtrl.Update(nbi);
                    }

                    // get roles for module
                    var versionRoles = new List<string>();
                    var permissionsList2 = moduleInfo.ModulePermissions.ToList();
                    foreach (var p in permissionsList2)
                    {
                        versionRoles.Add(p.RoleName);
                    }

                    // send email for only Manager and Validate roles.
                    var emailsentlist = new List<string>();
                    var roles = RoleController.Instance.GetRoles(nbi.PortalId);
                    foreach (var role in roles)
                    {
                        if ((role.RoleName == "Manager" || role.RoleName.StartsWith("Validate")) && versionRoles.Contains(role.RoleName))
                        {
                            var usersInRole = RoleController.Instance.GetUsersByRole(nbi.PortalId, role.RoleName);
                            foreach (var u in usersInRole)
                            {
                                var useremail = u.Email;
                                if (!emailsentlist.Contains(useremail))
                                {
                                    var userlang = u.Profile.PreferredLocale;
                                    if (userlang == "") userlang = PortalSettings.Current.DefaultLanguage;
                                    if (userlang == "") userlang = Utils.GetCurrentCulture();
                                    var emailsubject = PortalSettings.Current.PortalName + ": " + DnnUtils.GetResourceString("/DesktopModules/NBright/NBrightMod/App_LocalResources/", "Settings." + emailtemplatename.ToLower().Replace(".cshtml", "") + "-subject", "Text", userlang);
                                    var emailbody = LocalUtils.RazorTemplRender("config." + emailtemplatename, moduleid.ToString(), "", nbi, userlang);
                                    DotNetNuke.Services.Mail.Mail.SendMail(PortalSettings.Current.Email.Trim(), useremail.Trim(), "", emailsubject, emailbody, "", "HTML", "", "", "", "");
                                    emailsentlist.Add(useremail);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void VersionAuditLog(int moduleid, AuditCode action)
        {
            var auditFolder = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\AuditData";
            Utils.CreateFolder(auditFolder); // creates is not there
            using (StreamWriter w = File.AppendText(auditFolder + "\\" + moduleid + ".txt"))
            {
                int i = (int)action;
                var msg = i + "," + UserController.Instance.GetCurrentUserInfo().Username + "," ;
                msg += UserController.Instance.GetCurrentUserInfo().UserID + ",";
                w.WriteLine(DateTime.Now.ToString("s") + "," + msg);
            }
        }

        public static string VersionGetAuditLog(int moduleid)
        {
            var l = GetAuditLog(moduleid);
            if (l.Any())
            {
                return LocalUtils.RazorTemplRenderList("config.auditreport.cshtml", moduleid.ToString(""), "", l, Utils.GetCurrentCulture(), true);
            }
            else
            {
                return "";
            }
        }

        public static List<NBrightInfo> GetAuditLog(int moduleid)
        {
            var rtnList = new List<NBrightInfo>();
            var rtnList2 = new List<NBrightInfo>();
            string line;
            if (File.Exists(PortalSettings.Current.HomeDirectoryMapPath + "\\NBrightMod\\AuditData\\" + moduleid + ".txt"))
            {
                using (StreamReader r = File.OpenText(PortalSettings.Current.HomeDirectoryMapPath + "\\NBrightMod\\AuditData\\" + moduleid + ".txt"))
                {
                    var objUserCtrl = new UserController();
                    while ((line = r.ReadLine()) != null)
                    {
                        var nbi = new NBrightInfo(true);

                        var sline = line.Split(',');
                        if (sline.Count() == 5)
                        {
                            try
                            {
                                var strdate = sline[0];
                                if (Utils.IsDate(strdate))
                                {
                                    var auditdate = Convert.ToDateTime(strdate);
                                    var action = sline[1];
                                    var username = sline[2];
                                    var userid = sline[3];
                                    if (Utils.IsNumeric(userid) && Utils.IsNumeric(action))
                                    {
                                        var uInfo = UserController.Instance.GetUser(PortalSettings.Current.PortalId, Convert.ToInt32(userid));
                                        if (uInfo != null)
                                        {
                                            nbi.SetXmlProperty("genxml/auditdate",strdate);
                                            nbi.SetXmlProperty("genxml/userid", userid);
                                            nbi.SetXmlProperty("genxml/displayname", uInfo.DisplayName );
                                            nbi.SetXmlProperty("genxml/username", uInfo.Username);
                                            nbi.SetXmlProperty("genxml/auditaction", DnnUtils.GetResourceString("/DesktopModules/NBright/NBrightMod/App_LocalResources/", "Settings.auditcode" + action,"Text",Utils.GetCurrentCulture()));
                                            nbi.SetXmlProperty("genxml/auditicon", DnnUtils.GetResourceString("/DesktopModules/NBright/NBrightMod/App_LocalResources/", "Settings.auditicon" + action, "Text", Utils.GetCurrentCulture()));
                                            nbi.SetXmlProperty("genxml/auditline", line);
                                            rtnList2.Add(nbi);
                                        }
                                    }

                                }
                            }
                            catch (Exception e)
                            {
                                // don't report errors
                            }
                        }
                    }
                }

                // purge log file by delete and recreate.
                var auditFolder = PortalSettings.Current.HomeDirectoryMapPath.Trim('\\') + "\\NBrightMod\\AuditData";
                var auditFile = auditFolder + "\\" + moduleid + ".txt";
                if (File.Exists(auditFile))
                {
                        File.WriteAllText(auditFile, String.Empty);
                }

                // set into reverse order
                var lp = 0;
                for (int i = rtnList2.Count - 1; i >= 0; i--)
                {
                    if (lp < 200)
                    {
                        rtnList.Add(rtnList2[i]);
                        lp += 1;
                    }
                }

                //purge audit (200)
                if (rtnList2.Any())
                {
                    using (StreamWriter w = File.AppendText(auditFile))
                    {
                        var lp2 = 0;
                        var start = rtnList2.Count - 200;
                        if (start < 0) start = 0;
                        foreach (var nbi in rtnList2)
                        {
                            if (lp2 >= start)
                            {
                                w.WriteLine(nbi.GetXmlProperty("genxml/auditline"));
                            }
                            lp2 += 1;
                        }
                    }
                }


        }
            return rtnList;
        }



        #endregion 

        #region "functions"

        public static String AddNew(String moduleid, string entitytype, string moduleref)
        {
            if (!Utils.IsNumeric(moduleid)) moduleid = "-1";

            var prefix = "";
            if (VersionUserMustCreateVersion(Convert.ToInt32(moduleid)))
            {
                prefix = "a";
            }


            var objCtrl = new NBrightDataController();
            var nbi = new NBrightInfo(true);
            nbi.PortalId = PortalSettings.Current.PortalId;
            nbi.TypeCode = prefix + entitytype;
            nbi.ModuleId = Convert.ToInt32(moduleid);
            nbi.ItemID = -1;
            nbi.GUIDKey = moduleref;
            nbi.SetXmlProperty("genxml/hidden", "");
            nbi.SetXmlProperty("genxml/hidden/sortrecordorder", "9999");            
            var itemId = objCtrl.Update(nbi);
            nbi.ItemID = itemId;

            foreach (var lang in DnnUtils.GetCultureCodeList(PortalSettings.Current.PortalId))
            {
                var nbi2 = CreateLangaugeDataRecord(itemId, Convert.ToInt32(moduleid), lang, prefix, entitytype, moduleref);
            }

            LocalUtils.ClearRazorCache(nbi.ModuleId.ToString(""));

            return nbi.ItemID.ToString("");
        }

        public static NBrightInfo CreateLangaugeDataRecord(int parentItemId, int moduleid,String lang,string prefix = "",string entityType = "NBrightModDATA",string moduleref = "")
        {
            var objCtrl = new NBrightDataController();
            var nbi2 = new NBrightInfo(true);
            nbi2.PortalId = PortalSettings.Current.PortalId;
            nbi2.TypeCode = prefix + entityType + "LANG";
            nbi2.ModuleId = moduleid;
            nbi2.ItemID = -1;
            nbi2.Lang = lang;
            nbi2.ParentItemId = parentItemId;
            nbi2.GUIDKey = moduleref;
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
            string xmlData = GenXmlFunctions.GetGenXmlByAjax(ajaxData, mergeWithXml,"genxml", ignoresecurityfilter, filterlinks);
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

        public static Boolean CheckRights(int moduleid = 0)
        {
            if (moduleid == 0)
            {
                if (UserController.Instance.GetCurrentUserInfo().IsInRole("Manager") || UserController.Instance.GetCurrentUserInfo().IsInRole("Editor") || UserController.Instance.GetCurrentUserInfo().IsInRole("Administrators"))
                {
                    return true;
                }
            }
            else
            {
                var moduleInfo = DnnUtils.GetModuleinfo(Convert.ToInt32(moduleid));
                if (moduleInfo != null)
                {
                    return ModulePermissionController.HasModuleAccess(SecurityAccessLevel.Edit, "CONTENT", moduleInfo);
                }
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
                var dataRecord = objCtrl.GetByType(-1, Convert.ToInt32(moduleid), "SETTINGS");
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

        public static NBrightInfo GetConfig(Boolean useCache = true)
        {
            var rtnCache = Utils.GetCache("nbrightmodconfig*" + PortalSettings.Current.PortalId);
            if (rtnCache != null && useCache) return (NBrightInfo)rtnCache;
            // get template
                var objCtrl = new NBrightDataController();
            var nbiconfig = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "CONFIG", "NBrightModConfig");
            if (nbiconfig == null)
            {
                nbiconfig = new NBrightInfo(true);
                nbiconfig.ItemID = -1;
                nbiconfig.GUIDKey = "NBrightModConfig";
                nbiconfig.TypeCode = "CONFIG";
                nbiconfig.ModuleId = -1;
                nbiconfig.PortalId = PortalSettings.Current.PortalId;
                objCtrl.Update(nbiconfig);
            }
            Utils.SetCache("nbrightmodconfig*" + PortalSettings.Current.PortalId, nbiconfig);

            return nbiconfig;
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

                    var modref = settingInfo.GetXmlProperty("genxml/hidden/modref");
                    var objCtrl = new NBrightDataController();
                    var headerdataitem = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "NBrightModHEADER", modref);
                    if (headerdataitem == null)
                    {
                        // try loading new record created by versioner
                        headerdataitem = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "aNBrightModHEADER", modref);
                    }
                    else
                    {
                        headerdataitem = LocalUtils.VersionGet(headerdataitem, "NBrightModHEADER");
                    }
                    var headerdata = new NBrightInfo();
                    if (headerdataitem == null)
                    {
                        headerdata.GUIDKey = modref;
                        headerdata.TypeCode = "NBrightModHEADER";
                        headerdata.Lang = lang;
                        if (Utils.IsNumeric(moduleid))
                        {
                            headerdata.ModuleId = Convert.ToInt32(moduleid);
                        }
                    }
                    else
                    {
                        headerdata = objCtrl.Get(headerdataitem.ItemID, lang);
                    }
                    modRazor.HeaderData = headerdata;
                    var razorTemplOut = RazorRender(modRazor, razorTempl2, razorTemplateKey, debug);

                    if (cacheKey != "" && !debug) // only cache if we have a key.
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
                    if (obj.TypeCode == "NBrightModHEADER")
                    {
                        if (Utils.IsNumeric(moduleid))
                        {
                            obj.ModuleId = Convert.ToInt32(moduleid);
                        }
                        modRazor.HeaderData = obj;
                    }
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

            var cachemetadatakey = "preprocessmetadata*" + templName + "*" + moduleid + "*" + PortalSettings.Current.PortalId;

            // get cached data if there
            var cachedlist = (Dictionary<String, String>) Utils.GetCache(cachemetadatakey);  
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
                    razorTempl = RazorRender(modRazor, razorTempl, "render" + cachemetadatakey, settignInfo.GetXmlPropertyBool("genxml/checkbox/debugmode"));

                    // IMPORTANT: The AddPreProcessMetaData token will add any meta data to the cache list, we must get that list back into the cachedlist var.
                    cachedlist = (Dictionary<String, String>)Utils.GetCache(cachemetadatakey);

                    // if we have no preprocess items, we don;t want to run this again, so put the empty dic into cache.
                    if (cachedlist != null && cachedlist.Count == 0) Utils.SetCache(cachemetadatakey, cachedlist);
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
                var debug = settignInfo.GetXmlPropertyBool("genxml/checkbox/debugmode");
                var nbi = new NBrightInfo();
                nbi.Lang = Utils.GetCurrentCulture();
                var razorTempl = RazorTemplRender(fullTemplName, moduleid, Utils.GetCurrentCulture(), nbi, Utils.GetCurrentCulture(), debug);
                if (razorTempl != "")
                {
                    PageIncludes.IncludeTextInHeader(page, razorTempl);
                    page.Items["nbrightinject"] = page.Items["nbrightinject"] + fullTemplName + "." + moduleName + ",";
                }
            }
        }


        public static string RazorRender(Object info, string razorTempl, string templateKey, Boolean debugMode = false)
        {
            var result = "";
            try
            {
                var service = (IRazorEngineService)HttpContext.Current.Application.Get("NBrightModIRazorEngineService");
                if (service == null)
                {
                    // do razor test
                    var config = new TemplateServiceConfiguration();
                    config.Debug = debugMode;
                    config.BaseTemplateType = typeof(NBrightModRazorTokens<>);
                    service = RazorEngineService.Create(config);
                    HttpContext.Current.Application.Set("NBrightModIRazorEngineService", service);
                }
                Engine.Razor = service;
                var israzorCached = Utils.GetCache("rzcache_" + templateKey); // get a cache flag for razor compile.
                if (israzorCached == null || (string)israzorCached != razorTempl)
                {
                    result = Engine.Razor.RunCompile(razorTempl, GetMd5Hash(razorTempl), null, info);
                    Utils.SetCache("rzcache_" + templateKey, razorTempl);
                }
                else
                {
                    result = Engine.Razor.Run(GetMd5Hash(razorTempl), null, info);
                }

            }
            catch (Exception ex)
            {
                result = "<div>" + ex.Message + " templateKey='" + templateKey + "'</div>";
            }

            return result;
        }

        /// <summary>
        /// work arounf MD5 has for razorengine caching.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string GetMd5Hash(string input)
        {
            var md5 = MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hash = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (byte t in hash)
            {
                sb.Append(t.ToString("X2"));
            }
            return sb.ToString();
        }

        public static NBrightInfo CreateRequiredUploadFolders(NBrightInfo settings)
        {
            //var objPortal = PortalController.Instance.GetPortal(settings.PortalId);

            var tempFolder = GetHomePortalRel(settings.PortalId) + "/NBrightTemp";
            var tempFolderMapPath = GetHomePortalMapPath(settings.PortalId) + "\\NBrightTemp";
            Utils.CreateFolder(tempFolderMapPath);
            
            var settingUploadFolder = settings.GetXmlProperty("genxml/textbox/settinguploadfolder");
            if (settingUploadFolder == "")
            {
                settingUploadFolder = "images";
            }

            var uploadFolder = GetHomePortalRel(settings.PortalId) + "/NBrightUpload/" + settingUploadFolder;
            var uploadFolderMapPath = GetHomePortalMapPath(settings.PortalId) + "\\NBrightUpload\\" + settingUploadFolder;
            Utils.CreateFolder(uploadFolderMapPath);

            var uploadDocFolder = GetHomePortalRel(settings.PortalId) + "/NBrightUpload/documents";
            var uploadDocFolderMapPath = GetHomePortalMapPath(settings.PortalId) + "\\NBrightUpload\\documents";
            Utils.CreateFolder(uploadDocFolderMapPath);

            var uploadSecureDocFolder = GetHomePortalRel(settings.PortalId) + "/NBrightUpload/securedocs";
            var uploadSecureDocFolderMapPath = GetHomePortalMapPath(settings.PortalId) + "\\NBrightUpload\\securedocs";
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

                                    if (!relPath.StartsWith(nbi.GetXmlProperty("genxml/uploadfolder")))
                                    {
                                        relPath = "/" + nbi.GetXmlProperty("genxml/uploadfolder").Trim('/') + "/" + datasource.GetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/filename");
                                        datasource.SetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imageurl", relPath);
                                    }

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
                                    if (!relPath.StartsWith(nbi.GetXmlProperty("genxml/uploaddocfolder")))
                                    {
                                        relPath = "/" + nbi.GetXmlProperty("genxml/uploaddocfolder").Trim('/') + "/" + datasource.GetXmlProperty("genxml/docs/genxml[" + lp + "]/hidden/filename");
                                        datasource.SetXmlProperty("genxml/docs/genxml[" + lp + "]/hidden/imageurl", relPath);
                                    }

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

        /// <summary>
        /// Get Export XML data for theme
        /// </summary>
        /// <param name="themefolder">Theme folder</param>
        /// <param name="modref">Module ref is this is for a content export, so we only take hte required module level template.</param>
        /// <returns></returns>
        public static string ExportTheme(int portalId, string themefolder,string modref = "")
        {
            var xmlOut = "";
            if (themefolder != "")
            {
                var objPortal = PortalController.Instance.GetPortal(portalId);

                //APPTHEME
                // get portal level AppTheme templates
                var portalthemeFolderName = objPortal.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + themefolder;
                if (Directory.Exists(portalthemeFolderName))
                {
                    var flist = Directory.GetFiles(portalthemeFolderName, "*.*", SearchOption.AllDirectories);
                    foreach (var f in flist)
                    {
                        var doexport = true;
                        var fname = Path.GetFileName(f);
                        if (modref != "" && fname.StartsWith("_")) // prefix of "_" indicates module level template
                        {
                            if (!fname.Contains(modref))
                            {
                                // we only want to export this modules templates.
                                doexport = false;
                            }
                        }
                        if (doexport)
                        {
                            var nbi2 = new NBrightInfo(true);
                            nbi2.TypeCode = "EXPORTPORTALFILE";
                            nbi2.ModuleId = -1;
                            nbi2.TextData = Utils.ReadFile(f);
                            nbi2.SetXmlProperty("genxml/mappath", f);
                            nbi2.SetXmlProperty("genxml/name", fname);
                            System.Uri uri1 = new Uri(f);
                            System.Uri uri2 = new Uri(objPortal.HomeDirectoryMapPath);
                            Uri relativeUri = uri2.MakeRelativeUri(uri1);
                            nbi2.SetXmlProperty("genxml/relpath", "/" + relativeUri.ToString());
                            nbi2.SetXmlProperty("genxml/themefolder", themefolder);
                            xmlOut += nbi2.ToXmlItem();
                        }
                    }

                }

                var controlMapPath = GetRootWebsiteMapPath(portalId) + "\\DesktopModules\\NBright\\NBrightMod";
                var systhemeFolderName = controlMapPath + "\\Themes\\" + themefolder;
                if (Directory.Exists(systhemeFolderName))
                {
                    var flist = Directory.GetFiles(systhemeFolderName, "*.*", SearchOption.AllDirectories);
                    foreach (var f in flist)
                    {
                        var nbi2 = new NBrightInfo(true);
                        nbi2.TypeCode = "EXPORTSYSFILE";
                        nbi2.ModuleId = -1;
                        nbi2.TextData = Utils.ReadFile(f);
                        nbi2.SetXmlProperty("genxml/mappath", f);
                        nbi2.SetXmlProperty("genxml/name", Path.GetFileName(f));
                        System.Uri uri1 = new Uri(f);
                        System.Uri uri2 = new Uri(GetRootWebsiteMapPath(portalId));
                        Uri relativeUri = uri2.MakeRelativeUri(uri1);
                        nbi2.SetXmlProperty("genxml/relpath", "/" + relativeUri.ToString());
                        nbi2.SetXmlProperty("genxml/themefolder", themefolder);
                        xmlOut += nbi2.ToXmlItem();
                    }

                }
            }
            return xmlOut;
        }

        public static string GetRootWebsiteMapPath(int portalId)
        {
            var objPortal = PortalController.Instance.GetPortal(portalId);
            var s = objPortal.HomeDirectoryMapPath.Split('\\');
            var rtnPath = "";
            for (int i = 0; i < (s.Length - 2); i++)
            {
                rtnPath += s[i] + "\\";
            }
            return rtnPath.TrimEnd('\\');
        }

        public static string GetHomePortalMapPath(int portalId)
        {
            var objPortal = PortalController.Instance.GetPortal(portalId);
            return objPortal.HomeDirectoryMapPath;
        }
        public static string GetHomePortalRel(int portalId)
        {
            var objPortal = PortalController.Instance.GetPortal(portalId);
            return objPortal.HomeDirectory;
        }

        /// <summary>
        /// Import zip file into portal level template area
        /// </summary>
        /// <param name="theme">Name of theme to import</param>
        /// <param name="oldmodref">Old moduleref, if we are importing a module level and the ref has changed.</param>
        /// <param name="newmodref">New moduleref, if we are importing a module level and the ref has changed.</param>
        /// <returns></returns>
        public static string ImportTheme(int portalId, string theme, string oldmodref = "", string newmodref = "")
        {
            var objCtrl = new NBrightDataController();
            // load portal theme files and process
            var themportalfiles = objCtrl.GetList(portalId, -1, "EXPORTPORTALFILE");
            foreach (var nbi in themportalfiles)
            {
                ImportFileToPortalLevel(portalId, nbi, theme,oldmodref,newmodref);
                objCtrl.Delete(nbi.ItemID); // remove temp import record.
            }

            // load system theme files and process
            var themsysfiles = objCtrl.GetList(portalId, -1, "EXPORTSYSFILE");
            foreach (var nbi in themsysfiles)
            {

                // At the moment we don;t import the system level template files. 
                // Only the resx files, because these are merged.
                // Need to think about some version control on the system theme!!
                var fname = nbi.GetXmlProperty("genxml/name");

                if (fname.ToLower().EndsWith(".resx"))
                {
                    LocalUtils.ImportResxXml(nbi, theme);
                }
                else
                {
                    //ImportFileToPortalLevel(nbi, theme, oldmodref, newmodref);
                }

                objCtrl.Delete(nbi.ItemID); // remove temp import record.
            }
            return "";
        }

        private static void ImportFileToPortalLevel(int portalId, NBrightInfo nbi, string theme, string oldmodref = "", string newmodref = "")
        {
            var themefolder = nbi.GetXmlProperty("genxml/themefolder");
            var objPortal = PortalController.Instance.GetPortal(portalId);
            // create directory for theme files 
            var themeFolderName = objPortal.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\" + theme;
            if (!Directory.Exists(objPortal.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod"))
            {
                Directory.CreateDirectory(objPortal.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod");
                Directory.CreateDirectory(objPortal.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightMod\\Themes\\");
            }
            if (!Directory.Exists(themeFolderName))
            {
                Directory.CreateDirectory(themeFolderName);
            }

            // save files
            var relpath = "/" + objPortal.HomeDirectory.Trim('/') + nbi.GetXmlProperty("genxml/relpath");
            var fname = nbi.GetXmlProperty("genxml/name");
            if (oldmodref != "") fname = fname.Replace(oldmodref, newmodref);
            var filemappath = HttpContext.Current.Server.MapPath(relpath);
            if (oldmodref != "") filemappath = filemappath.Replace(oldmodref, newmodref);
            if (theme != themefolder)
            {
                filemappath = filemappath.Replace("\\NBrightMod\\Themes\\" + themefolder, "\\NBrightMod\\Themes\\" + theme);
            }
            // remove any system level path that may exist.
            filemappath = filemappath.Replace("\\DesktopModules\\NBright", "");

            var filefolder = filemappath.Replace("\\" + fname, "");
            if (!Directory.Exists(filefolder))
            {
                Directory.CreateDirectory(filefolder);
            }
            Utils.SaveFile(filemappath, nbi.TextData);

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
            var l4 = objCtrl.GetList(PortalSettings.Current.PortalId, moduleid, "NBrightModHEADER");
            foreach (var i in l4)
            {
                objCtrl.Delete(i.ItemID);
            }

        }

        public static void ImportResxXml(NBrightInfo nbi,string themefolder)
        {
            var resxXML = nbi.TextData;
            var fname = nbi.GetXmlProperty("genxml/name");

            var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod");
            if (!Directory.Exists(controlMapPath + "\\Themes\\" + themefolder))
            {
                Directory.CreateDirectory(controlMapPath + "\\Themes\\" + themefolder);
            }
            if (!Directory.Exists(controlMapPath + "\\Themes\\" + themefolder + "\\resx"))
            {
                Directory.CreateDirectory(controlMapPath + "\\Themes\\" + themefolder + "\\resx");
            }
            var systhemeFileName = controlMapPath + "\\Themes\\" + themefolder + "\\resx\\" + fname;
            if (File.Exists(systhemeFileName))
            {
                // save resx temp file
                Utils.SaveFile(systhemeFileName + ".tmp", resxXML);

                var resxlist = new List<DictionaryEntry>();
                var resxKeys = new List<string>();
                // read temp resx file
                if (File.Exists(systhemeFileName + ".tmp"))
                {
                    ResXResourceReader rsxr = new ResXResourceReader(systhemeFileName + ".tmp");
                    foreach (DictionaryEntry d in rsxr)
                    {
                        resxKeys.Add(d.Key.ToString());
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
                        if (!resxKeys.Contains(d.Key.ToString()))
                        {
                            resxKeys.Add(d.Key.ToString());
                            resxlist.Add(d);
                        }
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
                Utils.SaveFile(systhemeFileName, resxXML);
            }
        }

        public static string DeleteModuleTemplate(int moduleid, string themefolder, string templfilename, string lang = "*")
        {
            var langlist = new List<string>();
            if (lang == "*")
            {
                var langs = DnnUtils.GetCultureCodeList(PortalSettings.Current.PortalId);
                foreach (var l in langs)
                {
                    langlist.Add(l);
                }
                langlist.Add("");
            }
            else
            {
                langlist.Add(lang);
            }

            foreach (var actionlang in langlist)
            {
                var fldrlang = actionlang;
                if (fldrlang == "") fldrlang = "default";

                // for module level template we need to add the modref to the start of the template
                if (Utils.IsNumeric(moduleid))
                {
                    var objCtrl = new NBrightDataController();

                    // assign module themefolder.
                    var modsettings = objCtrl.GetByType(PortalSettings.Current.PortalId, moduleid, "SETTINGS");
                    if (modsettings != null)
                    {
                        themefolder = modsettings.GetXmlProperty("genxml/dropdownlist/themefolder");
                        var moduleref = modsettings.GetXmlProperty("genxml/hidden/modref");
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
            }

            return "OK";
        }

        #endregion



        #region "theme downloads"

        public static string DownloadAllThemes(string url)
        {
            var strOut = "";
            var list = GetWebsiteZipListing(url);
            foreach (var zipname in list)
            {
                DownloadFileAndUnZip(zipname.GetXmlProperty("genxml/hidden/filename"));
                strOut += "<h4>" + zipname + "</h4>";
            }

            return strOut;
        }

        public static string DownloadSingleThemes(string url,string downloadzip)
        {
            var strOut = "";
            var list = GetWebsiteZipListing(url);
            foreach (var zipname in list)
            {
                if (zipname.GetXmlProperty("genxml/hidden/filename") == downloadzip)
                {
                    DownloadFileAndUnZip(zipname.GetXmlProperty("genxml/hidden/filename"));
                    strOut = "<h4>" + zipname + "</h4>";
                }
            }

            return strOut;
        }

        public static List<NBrightInfo> GetWebsiteZipListing(string url)
        {
            var list = new List<NBrightInfo>();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string html = reader.ReadToEnd();

                    var dirnames = html.Replace("|","!").Replace("<br>", "|").Split('|');
                    foreach (var dirname in dirnames)
                    {
                        if (dirname.Contains(".zip"))
                        {
                            Regex regex = new Regex("<A HREF=\".*\">(?<name>.*)</A>");
                            MatchCollection matches = regex.Matches(dirname);
                            if (matches.Count > 0)
                            {
                                foreach (Match match in matches)
                                {
                                    if (match.Success)
                                    {
                                        var nbi = new NBrightInfo(true);
                                        nbi.SetXmlProperty("genxml/hidden/filename", match.Groups["name"].ToString());

                                        var downloadMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes/" + match.Groups["name"].ToString());
                                        if (Directory.Exists(downloadMapPath.ToLower().Replace(".zip", "")))
                                        {
                                            nbi.SetXmlProperty("genxml/hidden/installed", "True");
                                        }
                                        else
                                        {
                                            nbi.SetXmlProperty("genxml/hidden/installed", "False");
                                        }
                                        list.Add(nbi);
                                    }
                                }
                            }
                        }
                    }
                }

            }
            return list;
        }

        public static void DownloadFileAndUnZip(string filename)
        {
            var downloadMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightMod/Themes");
            var filepath = downloadMapPath + "\\" + filename ;
            using (var client = new WebClient())
            {
                client.DownloadFile("http://themes.nbrightproject.org/" + filename, filepath);
            }
            DnnUtils.UnZip(filepath, downloadMapPath);
            File.Delete(filepath);
        }


        #endregion

    }

}
