using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.ContentManager;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using System;

namespace Sitecore.Support.Shell.Applications.ContentManager
{
    internal class ContentEditorForm : Sitecore.Shell.Applications.ContentManager.ContentEditorForm
    {
        protected void SwitchTo(string target)
        {
            Assert.ArgumentNotNull((object)target, "target");
            if (!SheerResponse.CheckModified())
                return;
            UrlString urlString = new UrlString(target);
            Item folder = this.ContentEditorDataContext.GetFolder();
            if (folder != null)
            {
                urlString["pa" + WebUtil.GetQueryString("pa", "0")] =folder.Uri.ToString();
                urlString["la"] = this.ContentEditorDataContext.Language.Name;
            }
            SheerResponse.SetInnerHtml("scPostActionText", string.Empty);
            SheerResponse.SetLocation(urlString.ToString());
        }
    }
}
