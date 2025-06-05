using MarkdownPreview.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace MarkdownPreview
{
    public static class MediaElementExtensions
    {
        public static bool IsPlaying(this MediaElement mediaElement)
        {
            return mediaElement.IsLoaded && mediaElement.Clock == null && mediaElement.Position > TimeSpan.Zero;
        }
    }
    public class MarkdownParser
    {
        private static readonly Regex _headerRegex = new Regex(@"^(#+)\s+(?<content>.+)$", RegexOptions.Multiline);
        private static readonly Regex _imageRegex = new Regex(@"!\[(?<alt>[^\]]+)\]\((?<url>[^\)]+)\)", RegexOptions.Compiled);
        private static readonly Regex _codeBlockRegex = new Regex(@"```(?:([a-zA-Z0-9+#-]+)\s*\n)?(?<code>[\s\S]*?)```", RegexOptions.Compiled);
        private static readonly Regex _videoRegex = new Regex(@"!\[video\]\((?<url>[^\)]+)\)\s*""(?<alt>[^""]+)""", RegexOptions.Compiled);
        private static readonly Regex _linkRegex = new Regex(@"\[(?<text>[^\]]+)\]\((?<url>[^\)]+)\)", RegexOptions.Compiled);

        private static readonly Regex _boldRegex = new Regex(@"(?<!\\)\*\*(?<text>(?:\\.|.)+?)(?<!\\)\*\*", RegexOptions.Compiled);
        private static readonly Regex _italicRegex = new Regex(@"(?<!\\)\*(?<text>(?:\\.|.)+?)(?<!\\)\*", RegexOptions.Compiled);
        private static readonly Regex _boldAltRegex = new Regex(@"(?<!\\)__(?<text>(?:\\.|.)+?)(?<!\\)__", RegexOptions.Compiled);
        private static readonly Regex _italicAltRegex = new Regex(@"(?<!\\)_(?<text>(?:\\.|.)+?)(?<!\\)_", RegexOptions.Compiled); private static readonly Regex _strikethroughRegex = new Regex(@"~~(?<text>.+?)~~", RegexOptions.Compiled);
        private static readonly Regex _underlineRegex = new Regex(@"&(?<text>.+?)&", RegexOptions.Compiled);
        private static readonly Regex _quoteRegex = new Regex(@"^>\s*(?<text>.+)$", RegexOptions.Multiline);
        private static readonly Regex _listRegex = new Regex(@"^[\*\-\+]\s+(?<text>.+)$", RegexOptions.Multiline);
        private static readonly Regex _orderedListRegex = new Regex(@"^\d+\.\s+(?<text>.+)$", RegexOptions.Multiline);
        private static readonly Regex _horizontalRuleRegex = new Regex(@"^[-*_]{3,}$", RegexOptions.Multiline);
        private static readonly Regex _spoilerRegex = new Regex(@"\|\|(?<text>.+?)\|\|", RegexOptions.Compiled);

        public List<MarkdownElement> Parse(string markdown)
        {
            var elements = new List<MarkdownElement>();
            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool inCodeBlock = false;
            string currentCodeBlockLanguage = null;
            List<string> currentCodeLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        elements.Add(new CodeBlock
                        {
                            Language = currentCodeBlockLanguage ?? "plaintext",
                            Code = string.Join("\n", currentCodeLines)
                        });
                        currentCodeLines.Clear();
                        inCodeBlock = false;
                    }
                    else
                    {
                        inCodeBlock = true;
                        currentCodeBlockLanguage = line.Trim().Length > 3 ?
                            line.Trim().Substring(3).Trim() : null;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    currentCodeLines.Add(line);
                    continue;
                }

                var headerMatch = _headerRegex.Match(line);
                if (headerMatch.Success)
                {
                    elements.Add(new Header
                    {
                        Level = headerMatch.Groups[1].Length,
                        Content = headerMatch.Groups["content"].Value
                    });
                    continue;
                }


                var videoMatch = _videoRegex.Match(line);
                if (videoMatch.Success)
                {
                    elements.Add(new Video
                    {
                        AltText = videoMatch.Groups["alt"].Value,
                        Url = videoMatch.Groups["url"].Value
                    });
                    continue;
                }

                var imageMatch = _imageRegex.Match(line);
                if (imageMatch.Success && imageMatch.Groups["alt"].Value.ToLower() != "video")
                {
                    elements.Add(new Models.Image
                    {
                        AltText = imageMatch.Groups["alt"].Value,
                        Url = imageMatch.Groups["url"].Value
                    });
                    continue;
                }


                var linkMatch = _linkRegex.Match(line);
                if (linkMatch.Success)
                {
                    elements.Add(new Link
                    {
                        Text = linkMatch.Groups["text"].Value,
                        Url = linkMatch.Groups["url"].Value
                    });
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    var paragraph = new Paragraph
                    {
                        RawText = line,
                        InlineElements = ParseInlineElements(line)
                    };
                    elements.Add(paragraph);
                }


            }

            return elements;
        }
        private List<InlineElement> ParseInlineElements(string text)
        {
            var elements = new List<InlineElement>();
            var matches = new List<InlineMatch>();

            // 先匹配粗体（优先处理）
            ProcessMatches(_boldRegex, text, InlineElementType.Bold, matches);
            ProcessMatches(_boldAltRegex, text, InlineElementType.Bold, matches);

            // 然后匹配斜体（排除已在粗体中的部分）
            ProcessMatches(_italicRegex, text, InlineElementType.Italic, matches, excludeRanges: GetExcludeRanges(matches));
            ProcessMatches(_italicAltRegex, text, InlineElementType.Italic, matches, excludeRanges: GetExcludeRanges(matches));

            // 其他内联元素
            ProcessMatches(_strikethroughRegex, text, InlineElementType.Strikethrough, matches, excludeRanges: GetExcludeRanges(matches));
            ProcessMatches(_underlineRegex, text, InlineElementType.Underline, matches, excludeRanges: GetExcludeRanges(matches));
            ProcessMatches(_spoilerRegex, text, InlineElementType.Spoiler, matches, excludeRanges: GetExcludeRanges(matches));

            // 按位置排序匹配项
            matches = matches.OrderBy(m => m.StartIndex).ToList();

            // 构建内联元素列表
            int currentPos = 0;
            foreach (var match in matches)
            {
                // 添加匹配前的普通文本
                if (currentPos < match.StartIndex)
                {
                    elements.Add(new InlineElement
                    {
                        Type = InlineElementType.Normal,
                        Text = text.Substring(currentPos, match.StartIndex - currentPos)
                    });
                }

                // 添加匹配的内联元素
                elements.Add(new InlineElement
                {
                    Type = match.Type,
                    Text = match.Text
                });

                currentPos = match.StartIndex + match.Length;
            }

            // 添加剩余文本
            if (currentPos < text.Length)
            {
                elements.Add(new InlineElement
                {
                    Type = InlineElementType.Normal,
                    Text = text.Substring(currentPos)
                });
            }

            return elements;
        }

        private void ProcessMatches(Regex regex, string text, InlineElementType type,
            List<InlineMatch> matches, List<(int Start, int End)> excludeRanges = null)
        {
            foreach (Match match in regex.Matches(text))
            {
                // 检查是否在排除范围内
                if (excludeRanges != null &&
                    excludeRanges.Any(r => match.Index >= r.Start && match.Index + match.Length <= r.End))
                {
                    continue;
                }

                matches.Add(new InlineMatch
                {
                    Type = type,
                    Text = match.Groups["text"].Value,
                    StartIndex = match.Index,
                    Length = match.Length
                });
            }
        }

        private List<(int Start, int End)> GetExcludeRanges(List<InlineMatch> matches)
        {
            return matches.Select(m => (m.StartIndex, m.StartIndex + m.Length)).ToList();
        }

    }

}
