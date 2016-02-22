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
    public partial class Tools : Base.NBrightModBase
    {

        #region Event Handlers

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            //check if we have a skinsrc, if not add it and reload. NOTE: Where just asking for a infinate loop here, but DNN7.2 doesn't leave much option.
            const string skinSrcAdmin = "?SkinSrc=%2fDesktopModules%2fNBright%2fNBrightData%2fSkins%2fNBrightModAdmin%2fNormal";
            if (Utils.RequestParam(Context, "SkinSrc") == "")
            {
                var itemid = Utils.RequestParam(Context, "itemid");
                if (Utils.IsNumeric(itemid))
                    Response.Redirect(EditUrl("itemid", itemid, Utils.RequestParam(Context, "ctl")) + skinSrcAdmin, false);
                else
                    Response.Redirect(EditUrl(Utils.RequestParam(Context, "ctl")) + skinSrcAdmin, false);

                Context.ApplicationInstance.CompleteRequest(); // do this to stop iis throwing error
            }

            LocalUtils.IncludePageHeaders(base.ModuleId.ToString(""), this.Page, "NBrightMod", "tools", "config");
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                if (Page.IsPostBack == false)
                {
                    var settings = LocalUtils.GetSettings(ModuleId.ToString(""));
                    // check if we have new import
                    if (UserInfo.IsSuperUser && settings.UserId == -1)
                    {
                        LocalUtils.ValidateModuleData();
                    }

                    var strOut = LocalUtils.RazorTemplRender("config.tools.cshtml", ModuleId.ToString(""), "", new NBrightInfo(), Utils.GetCurrentCulture(), settings.GetXmlPropertyBool("genxml/checkbox/debugmode"));
                    var lit = new Literal();
                    lit.Text = strOut;
                    phData.Controls.Add(lit);
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
