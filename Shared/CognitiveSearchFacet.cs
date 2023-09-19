using System.Text.Json.Serialization;

namespace openaidemo_webapp.Shared
{
    public class CognitiveSearchFacets
    {
        [JsonPropertyName("CognitiveSearchResultList")]
        public List<CognitiveSearchFacet> CognitiveSearchFacetList { get; set; }
    }

    public class CognitiveSearchFacet
    {
        public string facetName { get; set; }
        public List<CognitiveSearchFacetResult> cognitiveSearchFacetResults { get; set; }
    }

    public class CognitiveSearchFacetResult
    {
        public string facetResultName { get; set; }
        public long? facetResultCount { get; set; }
    }
}