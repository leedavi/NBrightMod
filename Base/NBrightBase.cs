using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightDNN;
using NBrightMod.common;

namespace Nevoweb.DNN.NBrightMod.Base
{
    public class NBrightModBase : DotNetNuke.Entities.Modules.PortalModuleBase
    {
        public bool DebugMode = false;
        public string ModuleKey = "";
        public string UploadFolder = "";
        public string SelUserId = "";
        public string ThemeFolder = "";


        public DotNetNuke.Framework.CDefault BasePage
        {
            get { return (DotNetNuke.Framework.CDefault)this.Page; }
        }


    }
}
