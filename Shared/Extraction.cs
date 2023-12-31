﻿namespace openaidemo_webapp.Shared
{
    public class ExtractionResult
    {
        public string FileName { get; set; }
        public List<ExtractedParagraph> ExtractedParagraphs { get; set; }
        public string Company { get; set; }
        public string Year { get; set; }

    }

    public class ExtractedParagraph
    {
        public string Id { get; set; }
        public string Location { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public float[] ContentVector { get; set; }

    }
}