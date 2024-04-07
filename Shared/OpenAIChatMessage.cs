namespace openaidemo_webapp.Shared
{
    public class OpenAIChatMessage
    {
        public string ChatBubbleId { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public bool IsTemporaryResponse { get; set; }
        public List<CognitiveSearchResult>? Sources { get; set; }
    }
}
