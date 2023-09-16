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

        public async Task SendMessage(string user, string message)
        {
            var openAIHelper = new OpenAIHelper(_config);

            var response = await openAIHelper.QueryOpenAIWithPrompts(message, Clients.Caller);
        }

        public async Task SendCogServiceMessage(string user, string message)
        {
            var cognitiveSearchHelper = new CognitiveSearchHelper(_config);

            List<CognitiveSearchResult> cogSearchResults = await cognitiveSearchHelper.SingleVectorSearch(message);

            CognitiveSearchResults cognitiveSearchResults = new CognitiveSearchResults();
            cognitiveSearchResults.CognitiveSearchResultList = cogSearchResults;

            await Clients.Caller.SendAsync("CognitiveSearchResults", cognitiveSearchResults);
        }
    }
}