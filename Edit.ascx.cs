// --- Copyright (c) notice NevoWeb ---
//  Copyright (c) 2015 SARL Nevoweb.  www.Nevoweb.com. The MIT License (MIT).
// Author: D.C.Lee
// ------------------------------------------------------------------------
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ------------------------------------------------------------------------
// This copyright notice may NOT be removed, obscured or modified without written consent from the author.
// --- End copyright notice --- 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Tabs;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using DotNetNuke.Entities.Portals;
using NBrightMod.common;

namespace Nevoweb.DNN.NBrightMod
{

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The ViewNBrightGen class displays the content
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class Edit : Base.NBrightModBase
    {
        private bool _doSkinRedirect = false;

        #region Event Handlers

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            // refresh view by clearing all cache, and redirtect back to view 
            if ((Utils.RequestParam(Context, "refreshview") == "1" || Utils.RequestParam(Context, "version") != "") && Request.ApplicationPath != null)
            {
                string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/') + "/";
                Utils.RemoveCache("dnnsearchindexflag" + ModuleId);
                LocalUtils.ClearRazorCache(ModuleId.ToString(""));
                LocalUtils.ClearRazorSateliteCache(ModuleId.ToString(""));

                // this should be available from DnnUtils, but use direct to save recompile.
                DataCache.ClearPortalCache(PortalId, true);

                var langparam = "";
                if (DnnUtils.GetCultureCodeList().Count() > 1)
                {
                    langparam = "&language=" + Utils.RequestParam(Context, "language");
                }
                Session["nbrightmodversion"] = Utils.RequestParam(Context, "version");
                if (Utils.RequestParam(Context, "version") == "2")
                {
                    // accept verison changes
                    var objCtrl = new NBrightDataController();
                    var l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "NBrightModDATA");
                    var l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "NBrightModDATALANG");
                    // lang records first, so we don;t auto delete lang on base record delete.
                    foreach (var nbi in l2)
                    {
                        LocalUtils.VersionValidate(nbi);
                    }
                    foreach (var nbi in l)
                    {
                        if (nbi.GetXmlPropertyBool("genxml/versiondelete"))
                        {
                            // remove deleted data record.
                            objCtrl.Delete(nbi.XrefItemId);
                            objCtrl.Delete(nbi.ItemID);
                        }
                        LocalUtils.VersionValidate(nbi);
                    }
                    l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "aNBrightModDATA");
                    l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "aNBrightModDATALANG");
                    // lang records first, so we don;t auto delete lang on base record delete.
                    foreach (var nbi in l2)
                    {
                        LocalUtils.VersionValidate(nbi);
                    }
                    foreach (var nbi in l)
                    {
                        LocalUtils.VersionValidate(nbi);
                    }
                    LocalUtils.VersionSendEmail(ModuleId, "version-email-validate.cshtml");
                    LocalUtils.VersionAuditLog(ModuleId, AuditCode.Validate);
                }
                if (Utils.RequestParam(Context, "version") == "3" || Utils.RequestParam(Context, "version") == "6")
                {
                    // DELETE verison changes
                    var objCtrl = new NBrightDataController();
                    var l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "NBrightModDATA");
                    var l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "NBrightModDATALANG");
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

                    l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "aNBrightModDATA");
                    l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "aNBrightModDATALANG");
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
                    l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "vNBrightModDATA");
                    l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "vNBrightModDATALANG");
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
                    l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "aNBrightModDATA");
                    l2 = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "aNBrightModDATALANG");
                    // lang records first, so we don;t auto delete lang on base record delete.
                    foreach (var nbi in l2)
                    {
                        objCtrl.Delete(nbi.ItemID);
                    }
                    foreach (var nbi in l)
                    {
                        objCtrl.Delete(nbi.ItemID);
                    }
                    if (Utils.RequestParam(Context, "version") == "3")
                    {
                        // don't send email for reset by versioner (version=6)
                        LocalUtils.VersionSendEmail(ModuleId, "version-email-delete.cshtml");
                        LocalUtils.VersionAuditLog(ModuleId, AuditCode.Delete);
                    }
                    if (Utils.RequestParam(Context, "version") == "6")
                    {
                        LocalUtils.VersionAuditLog(ModuleId, AuditCode.Reset);
                    }
                }

                if (Utils.RequestParam(Context, "version") == "4")
                {
                    // DECLINE verison changes and send email.
                    LocalUtils.VersionSendEmail(ModuleId, "version-email-decline.cshtml");
                    LocalUtils.VersionAuditLog(ModuleId, AuditCode.Decline);
                }

                if (Utils.RequestParam(Context, "version") == "5")
                {
                    // Request Validation.
                    LocalUtils.VersionSendEmail(ModuleId, "version-email-new.cshtml");
                    LocalUtils.VersionAuditLog(ModuleId, AuditCode.Request);
                }

                Response.Redirect(baseUrl + "?tabid=" + Utils.RequestParam(Context,"TabId") + langparam, true);
            }


            // clear cache if debug mode
            var settings = LocalUtils.GetSettings(ModuleId.ToString(""));
            var debug = settings.GetXmlPropertyBool("genxml/checkbox/debugmode");
            if (debug)
            {
                LocalUtils.ClearRazorCache(ModuleId.ToString(""));
            }

            //check if we have a skinsrc, if not add it and reload. NOTE: Where just asking for a infinate loop here, but DNN7.2 doesn't leave much option.
            const string skinSrcAdmin = "?SkinSrc=%2fDesktopModules%2fNBright%2fNBrightData%2fSkins%2fNBrightModAdmin%2fNormal";
            if (Utils.RequestParam(Context, "SkinSrc") == "")
            {
                var itemid = Utils.RequestParam(Context, "itemid");
                if (Utils.IsNumeric(itemid))
                    Response.Redirect(EditUrl("itemid", itemid, Utils.RequestParam(Context, "ctl")) + skinSrcAdmin, false);
                else
                {
                    if (Utils.RequestParam(Context, "auditlog") != "")
                    {
                        Response.Redirect(EditUrl("auditlog", "1", Utils.RequestParam(Context, "ctl")) + skinSrcAdmin, false);
                    }
                    else
                    {
                        Response.Redirect(EditUrl(Utils.RequestParam(Context, "ctl")) + skinSrcAdmin, false);
                    }
                }
                _doSkinRedirect = true;
                Context.ApplicationInstance.CompleteRequest(); // do this to stop iis throwing error
            }

            LocalUtils.IncludePageHeaders(base.ModuleId.ToString(""), this.Page, "NBrightMod", "edit");
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                if (Page.IsPostBack == false && _doSkinRedirect == false)
                {
                    if (Utils.RequestParam(Context, "auditlog") != "")
                    {
                        var strOut = LocalUtils.VersionGetAuditLog(base.ModuleId);
                        var lit = new Literal();
                        lit.Text = strOut;
                        phData.Controls.Add(lit);
                    }
                    else
                    {

                        var settings = LocalUtils.GetSettings(ModuleId.ToString(""));
                        var objCtrl = new NBrightDataController();

                        #region "Single Page Edit"

                        // if we don;t have a editlist.cshtml, then it's a single page edit, so set the flag, otherwise cancel flag.
                        var oldsinglepageflag = settings.GetXmlPropertyBool("genxml/hidden/singlepageedit");
                        var listtemplate = LocalUtils.GetTemplateData("editlist.cshtml", Utils.GetCurrentCulture(), settings.ToDictionary());
                        if (listtemplate == "")
                            settings.SetXmlProperty("genxml/hidden/singlepageedit", "true");
                        else
                            settings.SetXmlProperty("genxml/hidden/singlepageedit", "false");

                        var newsinglepageflag = settings.GetXmlPropertyBool("genxml/hidden/singlepageedit");
                        var singlepageitemidtest = settings.GetXmlPropertyInt("genxml/hidden/singlepageitemid");
                        var nbiTest = objCtrl.Get(Convert.ToInt32(singlepageitemidtest));

                        if (newsinglepageflag != oldsinglepageflag || nbiTest == null)
                        {
                            settings.SetXmlProperty("genxml/hidden/singlepageitemid", "");
                            if (newsinglepageflag)
                            {
                                // check to see if we have existing record, if so use the first.
                                var l = objCtrl.GetList(PortalSettings.Current.PortalId, ModuleId, "NBrightModDATA");
                                if (l.Any())
                                {
                                    var firstnbi = l.First();
                                    settings.SetXmlProperty("genxml/hidden/singlepageitemid", firstnbi.ItemID.ToString(""));
                                }
                            }
                            // change in flag so need to save to DB
                            objCtrl.Update(settings);
                        }

                        // if we have a singlepageedit theme then create/set the record itemid
                        if (newsinglepageflag)
                        {
                            var singlepageitemid = settings.GetXmlProperty("genxml/hidden/singlepageitemid");
                            if (!Utils.IsNumeric(singlepageitemid)) singlepageitemid = LocalUtils.AddNew(ModuleId.ToString(""), "NBrightModDATA",ModuleKey);
                            if (Utils.IsNumeric(singlepageitemid))
                            {
                                settings.SetXmlProperty("genxml/hidden/singlepageitemid", singlepageitemid);
                                objCtrl.Update(settings);
                            }
                        }

                        #endregion

                        var strOut = LocalUtils.RazorTemplRender("editbody.cshtml", ModuleId.ToString(""), settings.GetXmlProperty("genxml/dropdownlist/themefolder") + Utils.GetCurrentCulture(), new NBrightInfo(), Utils.GetCurrentCulture(), true); // debug mode, don;t use cache for edit
                        var lit = new Literal();
                        lit.Text = strOut;
                        phData.Controls.Add(lit);
                    }
                }
            }
            catch (Exception exc) //Module failed to load
            {
                //display the error on the template (don;t want to log it here, prefer to deal with errors directly.)
                var l = new Literal();
                l.Text = exc.ToString();
                phData.Controls.Add(l);
            }
        }

        #endregion


    }

}
