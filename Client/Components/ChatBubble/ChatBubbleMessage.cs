namespace openaidemo_webapp.Client.Components.ChatBubble
{
    public class ChatBubbleMessage
    {
        public string ChatBubbleId { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public bool IsTemporaryResponse { get; set; }
    }
}
