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
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke.Common;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.FileSystem;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Exceptions;
using NBrightMod.common;

namespace Nevoweb.DNN.NBrightMod
{

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The ViewNBrightGen class displays the content
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class Settings : ModuleSettingsBase
    {

        private String _templD = "";

        #region Event Handlers

        override protected void OnInit(EventArgs e)
        {

            base.OnInit(e);

            try
            {

                _templD = "config.settings.html";

                // Get Display Body
                var settings = LocalUtils.GetSettings(ModuleId.ToString(""));
                var rpDataTempl = LocalUtils.GetTemplateData(_templD, Utils.GetCurrentCulture(), settings.ToDictionary());
                if (settings != null) rpDataTempl = Utils.ReplaceSettingTokens(rpDataTempl, settings.ToDictionary());
                rpDataTempl = Utils.ReplaceUrlTokens(rpDataTempl);
                rpData.ItemTemplate = new GenXmlTemplate(rpDataTempl);


            }
            catch (Exception exc)
            {
                var l = new Literal();
                l.Text = exc.Message;
                phData.Controls.Add(l);
                // catch any error and allow processing to continue, output error.
            }

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Page.IsPostBack == false)
            {
                PageLoad();
            }
        }

        private void PageLoad()
        {
            try
            {

                var obj = LocalUtils.GetSettings(base.ModuleId.ToString());
                var l = new List<object> { obj };
                rpData.DataSource = l;
                rpData.DataBind();
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }


        public override void UpdateSettings()
        {
            try
            {
                //SaveSettings();
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        #endregion

    }

}
