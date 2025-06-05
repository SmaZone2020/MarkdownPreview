namespace MarkdownPreview.Models
{
    public class CodeBlock : MarkdownElement
    {
        public string Language { get; set; }
        public string Code { get; set; }
    }
}