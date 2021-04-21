
using System.Linq;
using DotNetNuke.Entities.Portals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;


using System.Runtime.Remoting;
using System.Web;
using NBrightDNN;

namespace Nevoweb.DNN.NBrightMod.Components
{


    public abstract class AjaxInterface
	{

        // constructor
        static AjaxInterface()
		{
		}

        public abstract String ProcessCommand(string paramCmd, HttpContext context, string editlang = "");

        public abstract void Validate();

    }

}

