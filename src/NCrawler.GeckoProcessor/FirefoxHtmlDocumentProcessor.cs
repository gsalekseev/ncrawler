﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using NCrawler.Extensions;
using NCrawler.HtmlProcessor;
using NCrawler.Interfaces;
using NCrawler.Utils;

namespace NCrawler.GeckoProcessor
{
	public class FirefoxHtmlDocumentProcessor : HtmlDocumentProcessor, IPipelineStepWithTimeout
	{
		#region Constructors

		public FirefoxHtmlDocumentProcessor(Dictionary<string, string> filterTextRules,
			Dictionary<string, string> filterLinksRules) : base(filterTextRules, filterLinksRules)
		{
			CheckXulRunnerPath();
		}

		public FirefoxHtmlDocumentProcessor()
		{
			CheckXulRunnerPath();
		}

        #endregion

        #region IPipelineStepWithTimeout Members

        public override async Task ProcessAsync(ICrawler crawler, PropertyBag propertyBag)
        {
            AspectF.Define.
                NotNull(crawler, "crawler").
                NotNull(propertyBag, "propertyBag");

            if (propertyBag.StatusCode != HttpStatusCode.OK)
            {
                return;
            }

            if (!IsHtmlContent(propertyBag.ContentType))
            {
                return;
            }

            using (var geckoBrowserForm = new GeckoBrowserForm(XulRunnerPath, propertyBag.ResponseUri.ToString()))
            {
                geckoBrowserForm.Show();
                while (!geckoBrowserForm.Done)
                {
                    Application.DoEvents();
                }

                propertyBag.GetResponse = () => new MemoryStream(Encoding.UTF8.GetBytes(geckoBrowserForm.DocumentDomHtml));
                await base.ProcessAsync(crawler, propertyBag).ConfigureAwait(false);
            }
        }

        public TimeSpan ProcessorTimeout
		{
			get { return TimeSpan.FromSeconds(30); }
		}

		#endregion

		#region Class Properties

		public static string XulRunnerPath { get; set; }

		#endregion

		#region Class Methods

		private static void CheckXulRunnerPath()
		{
			if (!Directory.Exists(XulRunnerPath))
			{
				throw new DirectoryNotFoundException("XulRunner path '{0}' does not exist".FormatWith(XulRunnerPath));
			}
		}

		private static bool IsHtmlContent(string contentType)
		{
			return contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
		}

		#endregion
	}
}