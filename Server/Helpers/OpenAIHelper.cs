using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.SignalR;
using openaidemo_webapp.Shared;
using System.Linq;
using openaidemo_webapp.Client.Pages;

namespace openaidemo_webapp.Server.Helpers
{
    public class OpenAIHelper
    {
        private readonly IConfiguration _config;
        private ISingleClientProxy _signalrClient;
        private ConcurrentQueue<OpenAIChatMessage> messageQueue = new ConcurrentQueue<OpenAIChatMessage>();
        private Timer messageTimer;

        public OpenAIHelper(IConfiguration config)
        {
            _config = config;
            messageTimer = new Timer(ProcessQueue, null, 0, 25);
        }

        //
        // Query OpenAI with the supplied prompt.
        // Passing in the previousMessages and the SignalR client to stream back the response to the front end.
        //
        public async Task<String> QueryOpenAIWithPrompts(String prompt, List<OpenAIChatMessage> previousMessages, ISingleClientProxy signalrClient)
        {
            this._signalrClient = signalrClient;

            string key = _config["OpenAI:Key"] ?? string.Empty;
            string instanceName = _config["OpenAI:InstanceName"] ?? string.Empty;
            string endpoint = $"https://{instanceName}.openai.azure.com/";
            string deploymentName = _config["OpenAI:DeploymentName"] ?? string.Empty;

            // Generating a GUID for this message and send a temporary holding message
            String responseGuid = System.Guid.NewGuid().ToString();
            await this._signalrClient.SendAsync("ReceiveMessageToken", responseGuid, "ai", "...", true, null);

            // Create a new Azure OpenAI Client
            var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            
            System.Diagnostics.Debug.Print($"Input: {prompt}");
            // Prepare the Completions Options
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    //new ChatRequestSystemMessage("You are a helpful assistant. You will talk like a pirate."),
                    new ChatRequestSystemMessage("You are a helpful assistant"),
                    new ChatRequestUserMessage(prompt),
                },
                Temperature = (float)0.7,
                MaxTokens = 800,
                NucleusSamplingFactor = (float)0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
            };

            // Add in the previous messages
            int messagesToSkip = previousMessages.Count - 10;
            if (messagesToSkip < 0) messagesToSkip = 0;

            foreach (var previousMessage in previousMessages.Skip(messagesToSkip))
            {
                if (previousMessage.Type == "ai")
                {
                    chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(previousMessage.Content));
                }
                else if (previousMessage.Type == "human")
                {
                    chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(previousMessage.Content));
                }
            }

            // Add the prompt message last  
            chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(prompt));

            var completion = "";
            
            //
            // Get the Completions from OpenAI
            //
            await foreach (StreamingChatCompletionsUpdate chatUpdate in client.GetChatCompletionsStreaming(chatCompletionsOptions))
            {
                if (!string.IsNullOrEmpty(chatUpdate.ContentUpdate))
                {
                    completion += chatUpdate.ContentUpdate.ToString();
                    messageQueue.Enqueue(new OpenAIChatMessage { ChatBubbleId = responseGuid, Type = "AI", Content = chatUpdate.ContentUpdate.ToString(), IsTemporaryResponse = false });
                    System.Diagnostics.Debug.Print(chatUpdate.ContentUpdate);
                }                
            }

            await this._signalrClient.SendAsync("ReceiveMessage", responseGuid, "ai", completion);

            return completion;
        }

        //
        // Query OpenAI and add sources of data to query against
        //
        public async Task<String> QueryOpenAIWithPromptAndSources(String prompt, List<CognitiveSearchResult> cognitiveSearchResults, List<OpenAIChatMessage> previousMessages, ISingleClientProxy signalrClient)
        {
            this._signalrClient = signalrClient;

            string key = _config["OpenAI:Key"] ?? string.Empty;
            string instanceName = _config["OpenAI:InstanceName"] ?? string.Empty;
            string endpoint = $"https://{instanceName}.openai.azure.com/";
            string deploymentName = _config["OpenAI:DeploymentName"] ?? string.Empty;

            // Generating a GUID for this message and send a temporary holding message
            String responseGuid = System.Guid.NewGuid().ToString();
            await signalrClient.SendAsync("ReceiveMessageToken", responseGuid, "ai", "...", true, cognitiveSearchResults);

            // Create a new Azure OpenAI Client
            var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

            System.Diagnostics.Debug.Print($"Input: {prompt}");
            
            var initPrompt = @"You are an AI Assistant by Pete Gallagher, you are an expert in supplied documents.
                              Your task is to help Pete Gallagher gain insights from the documents.
                              You will be given a question and extracted parts of documents from a variety of sources
                              You will be given a question and extracted parts of documents from a variety of sources
                              Provide a clear and structured answer based on the context provided.
                              Return any tables and relevant content as html.
                              When relevant, use bullet points and lists to structure your answers.";

            initPrompt += $"The current date is {DateTime.Now.ToShortDateString()}";

            var sourcesPrompt = @"When relevant, use facts and numbers from the following documents in your answer.
                              Whenever you use information from a referenced document, reference it at the end of the sentence (ex: [DOC 1 - FileName], [DOC 2 - Filename]).
                              You don't have to use all documents, only if it makes sense in the conversation.
                              If no documents are provided, then do not add a reference.
                              If no relevant information to answer the question is present in the documents,
                              just say you don't have enough information to answer.";


            // Filter and sort the sources by Score property in descending order  
            var filteredAndSortedCognitiveSearchResults = cognitiveSearchResults
                .Where(source => Convert.ToDouble(source.Score) > 0.80)
                .OrderByDescending(source => source.Score);

            // Add the filtered and sorted sources to the sourcesPrompt  
            var index = 0;
            foreach (var source in cognitiveSearchResults)
            {
                index++;
                sourcesPrompt += $"DOC {index} (FileName: {source.FileName}, Company: {source.Company}, Year: {source.Year}): {source.Content}\n";
            }


            // Prepare the Completions Options
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    //new ChatRequestSystemMessage("You are a helpful assistant. You will talk like a pirate."),
                    new ChatRequestSystemMessage(initPrompt + sourcesPrompt),
                    new ChatRequestUserMessage(prompt),
                },
                Temperature = (float)0,
                MaxTokens = 800,
                NucleusSamplingFactor = (float)0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
            };

            // If there are previous messages, then include up to 10 of them
            if (previousMessages.Count > 0)
            {
                // Add in the previous messages
                int messagesToSkip = previousMessages.Count - 10;
                if (messagesToSkip < 0) messagesToSkip = 0;

                foreach (var previousMessage in previousMessages.Skip(messagesToSkip))
                {
                    if (previousMessage.Type == "ai")
                    {
                        chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(previousMessage.Content));
                    }
                    else if (previousMessage.Type == "human")
                    {
                        chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(previousMessage.Content));
                    }
                }
            }

            // Add the prompt message last  
            chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(prompt));

            var completion = "";

            //
            // Get the Completions from OpenAI
            //
            await foreach (StreamingChatCompletionsUpdate chatUpdate in client.GetChatCompletionsStreaming(chatCompletionsOptions))
            {
                if (!string.IsNullOrEmpty(chatUpdate.ContentUpdate))
                {
                    completion += chatUpdate.ContentUpdate.ToString();
                    messageQueue.Enqueue(new OpenAIChatMessage { ChatBubbleId = responseGuid, Type = "AI", Content = chatUpdate.ContentUpdate.ToString(), IsTemporaryResponse = false });
                    System.Diagnostics.Debug.Print(chatUpdate.ContentUpdate);
                }
            }

            await this._signalrClient.SendAsync("ReceiveMessage", responseGuid, "ai", completion);

            return completion;
        }

        private async void ProcessQueue(object state)
        {
            if (this._signalrClient == null)
            {
                return;
            }

            if (messageQueue.TryDequeue(out OpenAIChatMessage messageInfo))
            {
                await this._signalrClient.SendAsync("ReceiveMessageToken", messageInfo.ChatBubbleId, messageInfo.Type, messageInfo.Content, messageInfo.IsTemporaryResponse, null);
            }
        }
    }
}