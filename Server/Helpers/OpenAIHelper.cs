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

        public async Task<String> QueryOpenAIWithPromptAndSources(String prompt, List<CognitiveSearchResult> cognitiveSearchResults, ISingleClientProxy signalrClient)
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
            
            var initPrompt = @"You are an AI Assistant by Pete Codes.
      Your task is to help Pete Codes.
      You will be given a question and extracted parts of Microsoft Annual Reports and Shareholders Letters
      Provide a clear and structured answer based on the context provided.
      Return the content html encoded.
      When relevant, use bullet points and lists to structure your answers.";

            var sourcesPrompt = @"When relevant, use facts and numbers from the following documents in your answer.
      Whenever you use information from a document, reference it at the end of the sentence (ex: [DOC 2]).
      You don't have to use all documents, only if it makes sense in the conversation.
      If no relevant information to answer the question is present in the documents,
      just say you don't have enough information to answer.\n";

            var index = 0;
            foreach (var source in cognitiveSearchResults)
            {
                index++;
                sourcesPrompt += $"DOC {index}: {source.Content}";
            }

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    //new ChatMessage(ChatRole.System, "You are a helpful assistant. You will talk like a pirate."),
                    new ChatMessage(ChatRole.System, initPrompt + sourcesPrompt),
                    new ChatMessage(ChatRole.User, prompt),
                },
                Temperature = (float)0,
                MaxTokens = 800,
                NucleusSamplingFactor = (float)0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
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