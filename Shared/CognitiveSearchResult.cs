using System.Text.Json.Serialization;

namespace openaidemo_webapp.Shared
{

    public class CognitiveSearchResults
    {
        [JsonPropertyName("CognitiveSearchResultList")]
        public List<CognitiveSearchResult> CognitiveSearchResultList { get; set; }
    }

    public class CognitiveSearchResult
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public string Score { get; set; }
        public string Content { get; set; }
        public string Company { get; set; }

    }
}