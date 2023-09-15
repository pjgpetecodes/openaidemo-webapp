using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using openaidemo_webapp.Server.Helpers;

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
    }
}