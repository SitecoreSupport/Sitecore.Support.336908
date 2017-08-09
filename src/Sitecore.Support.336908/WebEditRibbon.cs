using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web.Bundling;
using Sitecore.Web.UI.HtmlControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Xml.XPath;
using Sitecore.Web;

namespace Sitecore.Support.Web.UI.WebControls
{
    public class WebEditRibbon : Sitecore.Web.UI.WebControls.WebEditRibbon
    {
        private Item currentItem;
        private string siteName = string.Empty;
        private bool setModifiedFlag;
        private bool IsUnitTesting
        {
            get
            {
                return MainUtil.GetBool(WebUtil.GetQueryString("sc_unit_test"), false);
            }
        }

        public string SiteName
        {
            get
            {
                return this.siteName;
            }
            set
            {
                Assert.IsNotNullOrEmpty(value, "SiteName");
                this.siteName = value;
            }
        }



        protected override void Render(HtmlTextWriter output, Item item)
        {
            Assert.ArgumentNotNull(output, "output");
            System.Collections.Generic.List<string> list = ScriptResources.GenerateBaseContentEditingScriptsList(this.WebEdit, this.Debug);
            foreach (string current in list)
            {
                output.Write(HtmlUtil.GetClientScriptIncludeHtml(current));
            }
            if (this.Page != null)
            {
                this.Page.Response.AddHeader("X-UA-Compatible", "IE=Edge,chrome=1");
            }
            if (item == null)
            {
                return;
            }
            currentItem = item;
            SiteContext site = Sitecore.Context.Site;
            if (site == null)
            {
                return;
            }
            if (WebUtil.GetQueryString("sc_duration") == "temporary")
            {
                return;
            }
            UrlString urlString = new UrlString("/sitecore/shell/Applications/WebEdit/WebEditRibbon.aspx");
            item.Uri.AddToUrlString(urlString);
            urlString["url"] = HttpUtility.UrlDecode(WebUtil.GetRawUrl());
            if (WebEditUtility.IsDebugActive(site))
            {
                urlString["debug"] = "1";
            }
            if (site.DisplayMode == DisplayMode.Edit)
            {
                urlString["mode"] = "edit";
            }
            else if (site.DisplayMode == DisplayMode.Preview)
            {
                urlString["mode"] = "preview";
            }
            if (site.DisableWebEditEditing)
            {
                urlString["wede"] = "1";
            }
            DiagnosticContext diagnostics = Sitecore.Context.Diagnostics;
            if (diagnostics.Tracing)
            {
                urlString["tr"] = "1";
            }
            if (diagnostics.Profiling)
            {
                urlString["pr"] = "1";
            }
            if (diagnostics.DrawRenderingBorders)
            {
                urlString["rb"] = "1";
            }
            if (diagnostics.ShowRenderingInfo)
            {
                urlString["ri"] = "1";
            }
            if (Sitecore.Context.PageDesigner.IsDesigning)
            {
                urlString["sc_de"] = WebUtil.GetQueryString("sc_de");
            }
            else
            {
                WebUtil.RemoveSessionValue("SC_PD_" + item.ID.ToShortID());
            }
            if (!string.IsNullOrEmpty(this.SiteName))
            {
                urlString["sc_pagesite"] = this.SiteName;
            }
            if (this.setModifiedFlag)
            {
                urlString["sc_smf"] = "1";
            }
            urlString["trf"] = Tracer.GetReportFileName();
            urlString["prf"] = Profiler.GetReportFileName();
            string text = "scFixedRibbon";
            if (this.Parent is Sitecore.Web.UI.WebControls.Placeholder)
            {
                text = string.Empty;
            }
            if (this.WebEdit)
            {
                if (WebUtility.IsSublayoutInsertingMode)
                {
                    return;
                }
                output.Write(ScriptResources.GetClientSettings());
                output.Write(ScriptResources.GetClientText());
                JsBundler jsBundler = new JsBundler(ScriptResources.GenerateContentEditingScriptsList(AnalyticsUtility.IsAnalyticsEnabled(), this.IsUnitTesting), Sitecore.Configuration.Settings.WebEdit.BundledJSFilesPath)
                {
                    Disabled = !Sitecore.Configuration.Settings.WebEdit.EnableJSBundling
                };
                output.Write(jsBundler.Render());
            }
            string device = WebUtility.GetDevice(urlString);
            WebEditUtil.Testing.TestSettings testSettings = null;
            if (this.WebEdit && AnalyticsUtility.IsAnalyticsEnabled())
            {
                testSettings = WebEditUtil.Testing.CurrentSettings;
            }
            if (testSettings != null)
            {
                urlString["sc_tt"] = testSettings.TestType.ToString();
            }
            output.Write("<link href=\"{0}\" rel=\"stylesheet\" />", Sitecore.Configuration.Settings.WebEdit.ContentEditorStylesheet);
            output.Write("<link rel='stylesheet' href='{0}'/>", Sitecore.ExperienceEditor.Settings.WebEdit.JQueryUIStylesheet);
            if (this.IsUnitTesting)
            {
                output.Write("<link href=\"{0}\" rel=\"stylesheet\" />", "/sitecore/shell/Controls/Lib/QUnit/qunit.css");
            }
            urlString[State.Client.UsesBrowserWindowsQueryParameterName] = "1";
            output.Write(string.Concat(new object[]
            {
                "<iframe id=\"scWebEditRibbon\" src=\"",
                urlString,
                "\" class=\"scWebEditRibbon ",
                text,
                "\" frameborder=\"0\" marginwidth=\"0\" marginheight=\"0\"></iframe>"
            }));
            if (this.WebEdit)
            {
                this.RenderLoadingIndicator(output);
            }
            WebUtility.RenderLayout(item, output, this.siteName, device);
            this.WriteLanguageCssClassParameter(output);
            if (site.DisplayMode == DisplayMode.Edit)
            {
                System.Collections.Generic.List<string> capabilities = WebEditRibbon.GetCapabilities();
                if (testSettings != null && testSettings.TestType == WebEditUtil.Testing.TestType.PageLevel)
                {
                    capabilities.Remove("testing");
                }
                output.Write("<input type=\"hidden\" id=\"scCapabilities\" value=\"" + string.Join("|", capabilities.ToArray()) + "\" />");
                if (testSettings != null && testSettings.TestType == WebEditUtil.Testing.TestType.Multivariate)
                {
                    output.Write("<input type=\"hidden\" id=\"scTestRunningFlag\" name=\"scTestRunningFlag\" value='{0}' />", MainUtil.BoolToString(testSettings.IsTestRunning));
                }
            }
            if (this.WebEdit && this.IsUnitTesting)
            {
                WebEditRibbon.GenerateMarkupForUnitTests(output);
            }
        }

        private void WriteLanguageCssClassParameter(System.IO.TextWriter ouputStream)
        {
            Assert.ArgumentNotNull(ouputStream, "ouputStream");
            string defaultValue = Sitecore.Context.Language.ToString();
            if (Sitecore.Context.Site != null)
            {
                defaultValue = Sitecore.Context.Site.Language;
            }
            using (new LanguageSwitcher(WebUtil.GetCookieValue("shell", "lang", defaultValue)))
            {
                ouputStream.Write("<input id=\"scLanguageCssClass\" type=\"hidden\" value=\"" + UIUtil.GetLanguageCssClassString() + "\" />");
            }
        }


        private bool IsLayoutPresetApplied()
        {
            return this.WebEdit && WebUtility.IsLayoutPresetApplied();
        }

        private void RenderLoadingIndicator(HtmlTextWriter output)
        {
            Assert.IsNotNull(output, "output");
            output.Write("<div id='scPeLoadingIndicator' class='scLoadingIndication'>");
            output.Write("<div class='scLoadingIndicatorInner'>");
            output.Write("<img src='{0}' />", "/sitecore/shell/Themes/Standard/Images/PageEditor/loading-indicator.gif");
            output.Write("<span>");
            using (new LanguageSwitcher(WebUtil.GetCookieValue("shell", "lang", Sitecore.Context.Language.Name)))
            {
                output.Write(Translate.Text("Loading..."));
            }
            output.Write("</span>");
            output.Write("</div>");
            output.Write("</div>");
        }

        private static void GenerateMarkupForUnitTests(HtmlTextWriter output)
        {
            output.Write("<div id='unit-tests'>");
            output.Write("<h1 id='qunit-header'>Unit tests result</h1>");
            output.Write("<h2 id='qunit-banner'></h2>");
            output.Write("<div id='qunit-testrunner-toolbar'></div>");
            output.Write("<h2 id='qunit-userAgent'></h2>");
            output.Write("<ol id='qunit-tests'></ol>");
            output.Write("<div id='qunit-fixture'></div>");
            output.Write("</div>");
        }

        private static System.Collections.Generic.List<string> GetCapabilities()
        {
            System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
            if (Policy.IsAllowed("Page Editor/Can Design") && Registry.GetString("/Current_User/Page Editor/Capability/design") != Sitecore.ExperienceEditor.Constants.Registry.CheckboxUnTickedRegistryValue)
            {
                list.Add("design");
            }
            if (Policy.IsAllowed("Page Editor/Can Edit") && Registry.GetString("/Current_User/Page Editor/Capability/edit") != Sitecore.ExperienceEditor.Constants.Registry.CheckboxUnTickedRegistryValue)
            {
                list.Add("edit");
            }
            if (Policy.IsAllowed("Page Editor/Extended features/Personalization"))
            {
                list.Add("personalization");
            }
            if (Policy.IsAllowed("Page Editor/Extended features/Testing"))
            {
                list.Add("testing");
            }
            return list;
        }
    }
}