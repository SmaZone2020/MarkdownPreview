namespace MarkdownPreview.Models
{
    public class Header : MarkdownElement
    {
        public int Level { get; set; }
        public string Content { get; set; }
    }
}