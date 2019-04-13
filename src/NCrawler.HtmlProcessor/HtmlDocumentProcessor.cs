﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

using NCrawler.Extensions;
using NCrawler.HtmlProcessor.Extensions;
using NCrawler.HtmlProcessor.Properties;
using NCrawler.Interfaces;
using NCrawler.Utils;

namespace NCrawler.HtmlProcessor
{
	public class HtmlDocumentProcessor : ContentCrawlerRules, IPipelineStep
	{
		#region Constructors

		public HtmlDocumentProcessor()
			: this(null, null)
		{
		}

		public HtmlDocumentProcessor(Dictionary<string, string> filterTextRules,
			Dictionary<string, string> filterLinksRules)
			: base(filterTextRules, filterLinksRules)
		{
		}

		#endregion

		#region Instance Methods

		protected virtual string NormalizeLink(string baseUrl, string link)
		{
			return link.NormalizeUrl(baseUrl);
		}

        #endregion

        #region IPipelineStep Members

        public virtual async Task ProcessAsync(ICrawler crawler, PropertyBag propertyBag)
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


            var htmlDoc = new HtmlDocument
            {
                OptionAddDebuggingAttributes = false,
                OptionAutoCloseOnEnd = true,
                OptionFixNestedTags = true,
                OptionReadEncoding = true
            };
            using (var reader = propertyBag.GetResponse())
            {
                var documentEncoding = htmlDoc.DetectEncoding(reader);
                reader.Seek(0, SeekOrigin.Begin);
                if (!documentEncoding.IsNull())
                {
                    htmlDoc.Load(reader, documentEncoding, true);
                }
                else
                {
                    htmlDoc.Load(reader, true);
                }
            }

            var originalContent = htmlDoc.DocumentNode.OuterHtml;
            if (this.HasTextStripRules || this.HasSubstitutionRules)
            {
                var content = this.StripText(originalContent);
                content = this.Substitute(content, propertyBag.Step);
                using (TextReader tr = new StringReader(content))
                {
                    htmlDoc.Load(tr);
                }
            }

            propertyBag["HtmlDoc"].Value = htmlDoc;

            var nodes = htmlDoc.DocumentNode.SelectNodes("//title");
            // Extract Title
            if (!nodes.IsNull())
            {
                propertyBag.Title = string.Join(";", nodes.
                    Select(n => n.InnerText).
                    ToArray()).Trim();
            }

            // Extract Meta Data
            nodes = htmlDoc.DocumentNode.SelectNodes("//meta[@content and @name]");
            if (!nodes.IsNull())
            {
                propertyBag["Meta"].Value = (
                    from entry in nodes
                    let name = entry.Attributes["name"]
                    let content = entry.Attributes["content"]
                    where !name.IsNull() && !name.Value.IsNullOrEmpty() && !content.IsNull() && !content.Value.IsNullOrEmpty()
                    select name.Value + ": " + content.Value).ToArray();
            }

            // Extract text
            propertyBag.Text = htmlDoc.ExtractText().Trim();
            if (this.HasLinkStripRules || this.HasTextStripRules)
            {
                var content = this.StripLinks(originalContent);
                using (TextReader tr = new StringReader(content))
                {
                    htmlDoc.Load(tr);
                }
            }

            var baseUrl = propertyBag.ResponseUri.GetLeftPath();

            // Extract Head Base
            nodes = htmlDoc.DocumentNode.SelectNodes("//head/base[@href]");
            if (!nodes.IsNull())
            {
                baseUrl =
                    nodes.
                    Select(entry => new { entry, href = entry.Attributes["href"] }).
                        Where(@t => !@t.href.IsNull() && !@t.href.Value.IsNullOrEmpty() &&
                            Uri.IsWellFormedUriString(@t.href.Value, UriKind.RelativeOrAbsolute)).
                    Select(@t => @t.href.Value).
                    AddToEnd(baseUrl).
                    FirstOrDefault();
            }

            // Extract Links
            var links = htmlDoc.GetLinks();
            foreach (var link in links.Links.Union(links.References))
            {
                if (link.IsNullOrEmpty())
                {
                    continue;
                }

                var decodedLink = ExtendedHtmlUtility.HtmlEntityDecode(link);
                var normalizedLink = this.NormalizeLink(baseUrl, decodedLink);
                if (normalizedLink.IsNullOrEmpty())
                {
                    continue;
                }

                await crawler.AddStepAsync(new Uri(normalizedLink), propertyBag.Step.Depth + 1,
                    propertyBag.Step, new Dictionary<string, object>
                        {
                            {Resources.PropertyBagKeyOriginalUrl, link},
                            {Resources.PropertyBagKeyOriginalReferrerUrl, propertyBag.ResponseUri}
                        }).ConfigureAwait(false);
            }
        }

        #endregion

        #region Class Methods

        private static bool IsHtmlContent(string contentType)
		{
			return contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
		}

        public Task Process(Crawler crawler, PropertyBag propertyBag)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}