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
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Exceptions;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Security;
using DotNetNuke.Services.Localization;
using NBrightMod.common;

namespace Nevoweb.DNN.NBrightMod
{

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The ViewNBrightGen class displays the content
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class View : Base.NBrightModBase, IActionable
    {

        #region Event Handlers


        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            LocalUtils.IncludePageHeaders(base.ModuleId.ToString(""), this.Page, "NBrightMod","view");
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {

                base.OnLoad(e);
                if (Page.IsPostBack == false)
                {
                    // check we have settings
                    var settings = LocalUtils.GetSettings(ModuleId.ToString());
                    if (settings.ModuleId == 0 || settings.GetXmlProperty("genxml/dropdownlist/themefolder") == "")
                    {
                        var lit = new Literal();
                        var rtnValue = Localization.GetString("notheme", "/DesktopModules/NBright/NBrightMod/App_LocalResources/View.ascx.resx", PortalSettings.Current, Utils.GetCurrentCulture(), true);
                        lit.Text = rtnValue;
                        phData.Controls.Add(lit);
                    }
                    else
                    {
                        PageLoad();
                    }
                }
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        private void PageLoad()
        {
            var objCtrl = new NBrightDataController();
            var settings = LocalUtils.GetSettings(ModuleId.ToString());

            // preprocess razor template to get meta data for data select into cache.
            var cachedlist = LocalUtils.RazorPreProcessTempl("view.cshtml", ModuleId.ToString(""), Utils.GetCurrentCulture());
            var orderby = "";
            if (cachedlist != null && cachedlist.ContainsKey("orderby")) orderby = cachedlist["orderby"];

            // get source moduleid
            var sourcemodid = Convert.ToInt32(ModuleId);
            if (settings.GUIDKey != settings.GetXmlProperty("genxml/dropdownlist/datasourceref") && settings.GetXmlProperty("genxml/dropdownlist/datasourceref") != "")
            {
                var sourcesettings = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "SETTINGS", settings.GetXmlProperty("genxml/dropdownlist/datasourceref"));
                sourcemodid = sourcesettings.ModuleId;
            }

            // get data list
            var l = objCtrl.GetList(PortalSettings.Current.PortalId, sourcemodid, "NBrightModDATA", "", orderby, 0, 0, 0, 0, Utils.GetCurrentCulture());

            var strOut = LocalUtils.RazorTemplRenderList("view.cshtml", ModuleId.ToString(""), settings.GetXmlProperty("genxml/dropdownlist/themefolder") + Utils.GetCurrentCulture(), l, Utils.GetCurrentCulture());
            var lit = new Literal();
            lit.Text = strOut;
            phData.Controls.Add(lit);

        }

        #endregion


        #region Optional Interfaces

        public ModuleActionCollection ModuleActions
        {
            get
            {
                var settings = LocalUtils.GetSettings(ModuleId.ToString());
                var actions = new ModuleActionCollection();
                if (settings.GUIDKey == settings.GetXmlProperty("genxml/dropdownlist/datasourceref") || settings.GetXmlProperty("genxml/dropdownlist/datasourceref") == "")
                {
                    actions.Add(GetNextActionID(), Localization.GetString("EditModule", this.LocalResourceFile), "", "", "", EditUrl(), false, SecurityAccessLevel.Edit, true, false);
                }
                actions.Add(GetNextActionID(), Localization.GetString("ModuleTheme", this.LocalResourceFile), "", "", "", EditUrl("themes"), false, SecurityAccessLevel.Admin, true, false);
                return actions;
            }
        }

        #endregion



    }

}
