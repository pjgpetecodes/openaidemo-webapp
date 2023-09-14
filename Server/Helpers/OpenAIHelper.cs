using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.SignalR;
using openaidemo_webapp.Shared;

namespace openaidemo_webapp.Server.Helpers
{
    public class OpenAIHelper
    {
        private readonly IConfiguration _config;
        private ISingleClientProxy signalrClient;
        private ConcurrentQueue<OpenAIChatMessage> messageQueue = new ConcurrentQueue<OpenAIChatMessage>();
        private Timer messageTimer;

        public OpenAIHelper(IConfiguration config)
        {
            _config = config;
            messageTimer = new Timer(ProcessQueue, null, 0, 25);
        }

        public async Task<String> QueryOpenAIWithPrompts(String prompt, ISingleClientProxy signalrClient)
        {
            this.signalrClient = signalrClient;

            string key = _config["OpenAI:Key"];
            string instanceName = _config["OpenAI:InstanceName"];
            string endpoint = $"https://{instanceName}.openai.azure.com/";
            string deploymentName = _config["OpenAI:DeploymentName"];

            // Generating a GUID for this message and send a temporary holoding message
            String responseGuid = System.Guid.NewGuid().ToString();
            await signalrClient.SendAsync("ReceiveMessageToken", responseGuid, "ai", "...", true);

            // Create a new Azure OpenAI Client
            var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            
            System.Diagnostics.Debug.Print($"Input: {prompt}");
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    //new ChatMessage(ChatRole.System, "You are a helpful assistant. You will talk like a pirate."),
                    new ChatMessage(ChatRole.System, "You are a helpful assistant."),
                    new ChatMessage(ChatRole.User, prompt),
                }
            };

            Response<StreamingChatCompletions> response = await client.GetChatCompletionsStreamingAsync(
                deploymentOrModelName: deploymentName,
                chatCompletionsOptions);
            using StreamingChatCompletions streamingChatCompletions = response.Value;

            var completion = "";

            await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
            {
                await foreach (ChatMessage message in choice.GetMessageStreaming())
                {
                    try
                    {
                        if (message.Content != null)
                        {
                            completion += message.Content.ToString();
                            messageQueue.Enqueue(new OpenAIChatMessage { ChatBubbleId = responseGuid, Type = "AI", Content = message.Content.ToString(), IsTemporaryResponse = false });
                            System.Diagnostics.Debug.Print(message.Content);
                        }
                    }
                    catch (Exception)
                    {
                        // ...  
                    }
                }
            }

            await signalrClient.SendAsync("ReceiveMessage", responseGuid, "ai", completion);

            return completion;
        }

        private async void ProcessQueue(object state)
        {
            if (this.signalrClient == null)
            {
                return;
            }

            if (messageQueue.TryDequeue(out OpenAIChatMessage messageInfo))
            {
                await this.signalrClient.SendAsync("ReceiveMessageToken", messageInfo.ChatBubbleId, messageInfo.Type, messageInfo.Content, messageInfo.IsTemporaryResponse);
            }
        }
    }
}