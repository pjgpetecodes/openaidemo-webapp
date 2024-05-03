using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using openaidemo_webapp.Server.Helpers;
using openaidemo_webapp.Shared;

namespace openaidemo_webapp.Server.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IConfiguration _config;

        //
        // Initialise the Configuration
        //
        public ChatHub(IConfiguration config)
        {
            _config = config;
        }

        //
        // Send a Query to Azure OpenAI using the Helper
        //
        public async Task SendQuery(string query, List<OpenAIChatMessage> previousMessages)
        {
            var openAIHelper = new OpenAIHelper(_config);

            var response = await openAIHelper.QueryOpenAIWithPrompts(query, previousMessages, Clients.Caller);
        }

        //
        // Send a Query to both Azure Cognitive Services and OpenAI using the Helpers
        //
        public async Task SendCogSearchQuery(string query, List<OpenAIChatMessage> previousMessages, string company, string year)
        {
            var cognitiveSearchHelper = new CognitiveSearchHelper(_config);

            List<CognitiveSearchResult> cogSearchResults = await cognitiveSearchHelper.SemanticHybridSearch(query, 6, company, year);

            CognitiveSearchResults cognitiveSearchResults = new CognitiveSearchResults();
            cognitiveSearchResults.CognitiveSearchResultList = cogSearchResults;

            await Clients.Caller.SendAsync("CognitiveSearchResults", cognitiveSearchResults);

            var openAIHelper = new OpenAIHelper(_config);

            var response = await openAIHelper.QueryOpenAIWithPromptAndSources(query, cogSearchResults, previousMessages,  Clients.Caller);
        }

        //
        // Query Cognitive Search on its own
        //
        public async Task QueryCogSearch(string query, string searchType)
        {
            var cognitiveSearchHelper = new CognitiveSearchHelper(_config);

            List<CognitiveSearchResult> cogSearchResults = new List<CognitiveSearchResult>();

            switch (searchType)
            {
                case "VectorSearch":

                    cogSearchResults = await cognitiveSearchHelper.SingleVectorSearch(query, 6);
                    break;

                case "SimpleHybridSearch":

                    cogSearchResults = await cognitiveSearchHelper.SimpleHybridSearch(query, 6);
                    break;

                case "SemanticHybridSearch":

                    cogSearchResults = await cognitiveSearchHelper.SemanticHybridSearch(query, 6);
                    break;
            }

            CognitiveSearchResults cognitiveSearchResults = new CognitiveSearchResults();
            cognitiveSearchResults.CognitiveSearchResultList = cogSearchResults;

            await Clients.Caller.SendAsync("CognitiveSearchResults", cognitiveSearchResults);

        }

        //
        // Get a list of the Cognitive Service Index Facets
        //
        public async Task GetCogSearchFacets(string query = "")
        {
            var cognitiveSearchHelper = new CognitiveSearchHelper(_config);

            CognitiveSearchFacets cognitiveSearchFacetResults = cognitiveSearchHelper.GetAllFacets();

            await Clients.Caller.SendAsync("CognitiveSearchFacetResults", cognitiveSearchFacetResults);

        }

    }
}