namespace MarkdownPreview.Models
{
    public class Paragraph : MarkdownElement
    {
        public string RawText { get; set; }
        public List<InlineElement> InlineElements { get; set; } = new List<InlineElement>();

    }
}