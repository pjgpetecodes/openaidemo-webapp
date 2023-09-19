using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.SignalR;
using openaidemo_webapp.Server.Helpers;
using openaidemo_webapp.Shared;

namespace openaidemo_webapp.Server.Hubs
{
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
        public async Task SendCogServiceQuery(string query, List<OpenAIChatMessage> previousMessages, string company, string year)
        {
            var cognitiveSearchHelper = new CognitiveSearchHelper(_config);

            List<CognitiveSearchResult> cogSearchResults = await cognitiveSearchHelper.SingleVectorSearch(query, 6, company, year);

            CognitiveSearchResults cognitiveSearchResults = new CognitiveSearchResults();
            cognitiveSearchResults.CognitiveSearchResultList = cogSearchResults;

            await Clients.Caller.SendAsync("CognitiveSearchResults", cognitiveSearchResults);

            var openAIHelper = new OpenAIHelper(_config);

            var response = await openAIHelper.QueryOpenAIWithPromptAndSources(query, cogSearchResults, previousMessages,  Clients.Caller);
        }

        //
        // Get a list of the Cognitive Service Index Facets
        //
        public async Task GetCogServiceFacets(string query = "")
        {
            var cognitiveSearchHelper = new CognitiveSearchHelper(_config);

            CognitiveSearchFacets cognitiveSearchFacetResults = await cognitiveSearchHelper.GetAllFacets();

            await Clients.Caller.SendAsync("CognitiveSearchFacetResults", cognitiveSearchFacetResults);

        }


    }
}