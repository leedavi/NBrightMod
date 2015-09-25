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

                _templD = "settings.html";

                // Get Display Body
                var rpDataTempl = LocalUtils.GetTemplateData(_templD, Utils.GetCurrentCulture());
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

        private void SaveSettings()
        {
            try
            {

                var tempFolder = PortalSettings.Current.HomeDirectory.TrimEnd('/') + "/NBrightTemp";
                var tempFolderMapPath = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightTemp";
                Utils.CreateFolder(tempFolderMapPath);
                var uploadFolder = PortalSettings.Current.HomeDirectory.TrimEnd('/') + "/NBrightUpload";
                var uploadFolderMapPath = PortalSettings.Current.HomeDirectoryMapPath.TrimEnd('\\') + "\\NBrightUpload";
                Utils.CreateFolder(uploadFolderMapPath);

                var objCtrl = new NBrightDataController();
                var dataRecord = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, base.ModuleId, "SETTINGS", "NBrightMod");
                if (dataRecord == null)
                {
                    dataRecord = new NBrightInfo(true); // populate empty XML so we can update nodes.
                    dataRecord.GUIDKey = "NBrightMod";
                    dataRecord.PortalId = PortalSettings.Current.PortalId;
                    dataRecord.ModuleId = base.ModuleId;
                    dataRecord.TypeCode = "SETTINGS";
                    dataRecord.Lang = "";
                }
                //rebuild xml
                dataRecord.ModuleId = base.ModuleId;
                dataRecord.XMLData = GenXmlFunctions.GetGenXml(rpData);
                dataRecord.SetXmlProperty("genxml/tempfolder", tempFolder);
                dataRecord.SetXmlProperty("genxml/uploadfolder", uploadFolder);
                dataRecord.SetXmlProperty("genxml/tempfoldermappath", tempFolderMapPath);
                dataRecord.SetXmlProperty("genxml/uploadfoldermappath", uploadFolderMapPath);
                objCtrl.Update(dataRecord);


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
