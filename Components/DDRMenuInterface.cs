using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DotNetNuke.Entities.Portals;
using DotNetNuke.UI;
using DotNetNuke.Web.DDRMenu;
using DotNetNuke.Web.DDRMenu.Localisation;
using NBrightCore.common;
using NBrightDNN;
using NBrightMod.common;

namespace Nevoweb.DNN.NBrightMod.Components
{
    public class DdrMenuInterface : INodeManipulator
    {
        public List<MenuNode> ManipulateNodes(List<MenuNode> nodes, DotNetNuke.Entities.Portals.PortalSettings portalSettings)
        {
            var parentcatref = "";
 
            var tabid = PortalSettings.Current.ActiveTab.TabID;

            var rtnNode = new List<MenuNode>();

            var activateModList = new List<NBrightInfo>();
            var l = LocalUtils.GetNBrightModList();
            foreach (var m in l)
            {
                if (m.GetXmlPropertyBool("genxml/checkbox/categorymode"))
                {
                    activateModList.Add(m);
                }
            }

            if (activateModList.Count() == 0) return nodes;

            foreach (var nod in nodes)
            {
                foreach (var modSettings in activateModList)
                {
                    if (nod.TabId == modSettings.GetXmlPropertyInt("genxml/hidden/tabid"))
                    {
                        var objCtrl = new NBrightDataController();
                        var datalist = objCtrl.GetList(PortalSettings.Current.PortalId, modSettings.ModuleId, "NBrightModDATA", "", " ORDER BY NB1.XMLData.value('(genxml/hidden/sortrecordorder)[1]','int') ", 100, 0, 0, 0, Utils.GetCurrentCulture());

                        foreach (var dataInfo in datalist)
                        {
                            var pagename = dataInfo.GetXmlProperty("genxml/lang/genxml/textbox/pagename");
                            if (pagename == "") pagename = dataInfo.GetXmlProperty("genxml/lang/genxml/textbox/title");
                            var pagetitle = dataInfo.GetXmlProperty("genxml/lang/genxml/textbox/pagetitle");
                            if (pagetitle == "") pagetitle = pagename;


                            var n = new MenuNode();
                            n.Parent = nod;
                            n.TabId = nod.TabId;
                            n.Text = pagename;
                            n.Title = pagetitle;
                            n.Url =  nod.Url + "/eid/" + dataInfo.ItemID + "/" + Utils.UrlFriendly(pagename);
                            n.Enabled = true;
                            n.Selected = false;
                            n.Breadcrumb = false;
                            n.Separator = false;
                            n.LargeImage = "";
                            n.Icon = "";
                            n.Keywords = dataInfo.GetXmlProperty("genxml/lang/genxml/textbox/tagwords");
                            n.Description = dataInfo.GetXmlProperty("genxml/lang/genxml/textbox/pagedescription");
                            n.CommandName = "";
                            n.CommandArgument = ""; // not used
                            n.Children = new List<MenuNode>();
                            nod.Children.Add(n);

                            if (nod.Depth > 0)
                            {
                                rtnNode.Add(n);
                            }
                        }
                    }
                }
                rtnNode.Add(nod);
            }

            return rtnNode;
        }


        private List<MenuNode> GetNodeXml(string currentTabId, int parentItemId = 0, bool recursive = true, int depth = 0, MenuNode pnode = null, string defaultListPage = "")
        {

            var nodes = new List<MenuNode>();
            var l = new List<object>();

            foreach (var obj in l)
            {
                //if (!obj.ishidden)
                //{

                //    var n = new MenuNode();

                //    n.Parent = pnode;

                //    n.TabId = obj.categoryid;
                //    n.Text = obj.categoryname;
                //    n.Title = obj.categorydesc;
                //    n.Url = "";
                //    n.Enabled = true;
                //    if (obj.disabled) n.Enabled = false;
                //    n.Selected = false;
                //    // redundant with caching
                //    //if (_catid == obj.categoryid.ToString("")) n.Selected = true;
                //    n.Breadcrumb = false;
                //    //if (_catid == obj.categoryid.ToString("")) n.Breadcrumb = true;
                //    n.Separator = false;
                //    n.LargeImage = "";
                //    n.Icon = "";
                //    var img = obj.imageurl;
                //    if (img != "")
                //    {
                //        n.LargeImage = img;
                //        n.Icon = "";
                //    }
                //    n.Keywords = obj.metakeywords;
                //    n.Description = obj.metadescription;
                //    n.CommandName = "";
                //    //n.CommandArgument = string.Format("entrycount={0}|moduleid={1}", obj.GetXmlProperty("genxml/hidden/entrycount"), obj.ModuleId.ToString(""));
                //    n.CommandArgument = obj.entrycount.ToString(""); // not used, so we use it to store the entry count


                //    nodes.Add(n);
                //}

            }

            return nodes;

        }


    }


}
