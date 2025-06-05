using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownPreview.Models
{
    public class BoldText : MarkdownElement
    {
        public string Text { get; set; }
    }

    public class ItalicText : MarkdownElement
    {
        public string Text { get; set; }
    }

    public class StrikethroughText : MarkdownElement
    {
        public string Text { get; set; }
    }

    public class UnderlineText : MarkdownElement
    {
        public string Text { get; set; }
    }

    public class Quote : MarkdownElement
    {
        public string Text { get; set; }
    }

    public class ListItem : MarkdownElement
    {
        public string Text { get; set; }
        public bool IsOrdered { get; set; }
        public int? Number { get; set; }
    }

    public class HorizontalRule : MarkdownElement { }

    public class SpoilerText : MarkdownElement
    {
        public string Text { get; set; }
    }



    public enum InlineElementType
    {
        Normal,
        Bold,
        Italic,
        Strikethrough,
        Underline,
        Spoiler
    }
    public class InlineMatch
    {
        public InlineElementType Type { get; set; }
        public string Text { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
    }
    public class InlineElement
    {
        public InlineElementType Type { get; set; }
        public string Text { get; set; }
    }
}