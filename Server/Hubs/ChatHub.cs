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

        public ChatHub(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendQuery(string query, List<OpenAIChatMessage> previousMessages)
        {
            var openAIHelper = new OpenAIHelper(_config);

            var response = await openAIHelper.QueryOpenAIWithPrompts(query, previousMessages, Clients.Caller);
        }

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
    }
}