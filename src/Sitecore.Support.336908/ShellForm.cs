using Sitecore.Shell.Applications;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using System.Web;
using System.Web.UI;

namespace Sitecore.Support.Shell.Applications
{
    public class ShellForm : Sitecore.Shell.Applications.ShellForm
    {
        protected new void ShowLinkProperties(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                var subControl = Context.ClientPage.FindSubControl(args.Parameters["source"]);
                if (subControl != null && subControl is ListviewItem)
                {
                    ListviewItem listviewItem = subControl as ListviewItem;
                    string str = StringUtil.GetString(listviewItem.ServerProperties["Link"]);
                    if (!string.IsNullOrEmpty(str))
                        listviewItem.ServerProperties["Link"] = (object)HttpUtility.UrlDecode(str);
                }
            }
            base.ShowLinkProperties(args);
        }
    }
}