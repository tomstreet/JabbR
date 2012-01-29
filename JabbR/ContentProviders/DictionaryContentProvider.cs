﻿using System;
using System.Net;
using HtmlAgilityPack;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class DictionaryContentProvider : CollapsibleContentProvider
    {
        private const string _domain = "http://dictionary.reference.com";
        private static readonly string ContentFormat = "<div class='dictionary_wrapper'>" +
                                                       "    <div class=\"dictionary_header\">" +
                                                       "        <img src=\"/Content/images/contentproviders/dictionary_logo.png\" alt=\"\" width=\"64\" height=\"64\">" +
                                                       "        <h2>{0}</h2>" +
                                                       "    </div>" +
                                                       "    <div>{1}</div>" +
                                                       "</div>";

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            var pageInfo = ExtractFromResponse(response);
            return new ContentProviderResultModel()
            {
                Content = String.Format(ContentFormat, pageInfo.Title, pageInfo.WordDefinition),
                Title = pageInfo.Title
            };
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.AbsoluteUri.StartsWith("http://dictionary.reference.com", StringComparison.OrdinalIgnoreCase) ||
            response.ResponseUri.AbsoluteUri.StartsWith("http://dictionary.com", StringComparison.OrdinalIgnoreCase); ;
        }

        private PageInfo ExtractFromResponse(HttpWebResponse response)
        {
            var pageInfo = new PageInfo();
            using (var responseStream = response.GetResponseStream())
            {
                var htmlDocument = new HtmlDocument();
                htmlDocument.Load(responseStream);

                var title = htmlDocument.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
                pageInfo.Title = title != null ? title.Attributes["content"].Value : string.Empty;
                pageInfo.WordDefinition = GetWordDefinition(htmlDocument);
            }

            return pageInfo;
        }

        private string GetWordDefinition(HtmlDocument htmlDocument)
        {
            var wordDefinition = htmlDocument.DocumentNode.SelectSingleNode("//div[@class=\"body\"]");
            if (wordDefinition == null)
                return string.Empty;

            //remove stylesheet links
            var stylesheets = wordDefinition.SelectNodes("//link");
            foreach (var stylesheet in stylesheets)
            {
                stylesheet.Remove();
            }

            // fix relative url
            var links = wordDefinition.SelectNodes("//a");
            try
            {
                foreach (var link in links)
                {
                    var href = link.Attributes["href"];
                    if (href != null && href.Value.StartsWith("/"))
                    {
                        href.Value = string.Format("{0}{1}", _domain, href.Value);

                        if (link.Attributes["style"] != null)
                            link.Attributes["style"].Value = string.Empty;

                        link.SetAttributeValue("target", "_blank");
                    }
                    else
                    {
                        link.Remove();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


            return wordDefinition.InnerHtml;
        }

        private class PageInfo
        {
            public string Title { get; set; }
            public string WordDefinition { get; set; }
        }
    }
}