namespace openaidemo_webapp.Shared
{
    public class ExtractionResult
    {
        public string FileName { get; set; }
        public List<ExtractedParagraph> ExtractedParagraphs { get; set; }
    }

    public class ExtractedParagraph
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
}