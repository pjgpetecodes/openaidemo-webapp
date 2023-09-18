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
        private ISingleClientProxy signalrClient;
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
            this.signalrClient = signalrClient;

            string key = _config["OpenAI:Key"] ?? string.Empty;
            string instanceName = _config["OpenAI:InstanceName"] ?? string.Empty;
            string endpoint = $"https://{instanceName}.openai.azure.com/";
            string deploymentName = _config["OpenAI:DeploymentName"] ?? string.Empty;

            // Generating a GUID for this message and send a temporary holoding message
            String responseGuid = System.Guid.NewGuid().ToString();
            await signalrClient.SendAsync("ReceiveMessageToken", responseGuid, "ai", "...", true);

            // Create a new Azure OpenAI Client
            var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            
            System.Diagnostics.Debug.Print($"Input: {prompt}");
            // Prepare the Completions Options
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    //new ChatMessage(ChatRole.System, "You are a helpful assistant. You will talk like a pirate."),
                    new ChatMessage(ChatRole.System, "You are a helpful assistant"),
                    new ChatMessage(ChatRole.User, prompt),
                },
                Temperature = (float)0,
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
                ChatRole chatRole = ChatRole.User;

                if (previousMessage.Type == "ai")
                {
                    chatRole = ChatRole.Assistant;
                }
                else if (previousMessage.Type == "human")
                {
                    chatRole = ChatRole.User;
                }

                chatCompletionsOptions.Messages.Add(new ChatMessage(chatRole, previousMessage.Content));
            }

            // Add the prompt message last  
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, prompt));

            //
            // Get the Completions from OpenAI
            //
            Response<StreamingChatCompletions> response = await client.GetChatCompletionsStreamingAsync(
                deploymentOrModelName: deploymentName,
                chatCompletionsOptions);
            using StreamingChatCompletions streamingChatCompletions = response.Value;

            var completion = "";

            //
            // Loop through each of the completion options that OpenAI has returned
            //
            await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
            {
                //
                // Get each streaming token and add it to the queue to send to the connected SignalR Client
                //
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
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print($"Error querying OpenAI: {ex}");
                        return null;
                    }
                }
            }

            await signalrClient.SendAsync("ReceiveMessage", responseGuid, "ai", completion);

            return completion;
        }

        //
        // Query OpenAI and add sources of data to query against
        //
        public async Task<String> QueryOpenAIWithPromptAndSources(String prompt, List<CognitiveSearchResult> cognitiveSearchResults, List<OpenAIChatMessage> previousMessages, ISingleClientProxy signalrClient)
        {
            this.signalrClient = signalrClient;

            string key = _config["OpenAI:Key"] ?? string.Empty;
            string instanceName = _config["OpenAI:InstanceName"] ?? string.Empty;
            string endpoint = $"https://{instanceName}.openai.azure.com/";
            string deploymentName = _config["OpenAI:DeploymentName"] ?? string.Empty;

            // Generating a GUID for this message and send a temporary holding message
            String responseGuid = System.Guid.NewGuid().ToString();
            await signalrClient.SendAsync("ReceiveMessageToken", responseGuid, "ai", "...", true);

            // Create a new Azure OpenAI Client
            var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

            System.Diagnostics.Debug.Print($"Input: {prompt}");
            
            var initPrompt = @"You are an AI Assistant by Pete Codes who is an expert in Accounting, Shareholding and Management.
                              Your task is to help Pete Codes gain insights from the financial documents.
                              You will be given a question and extracted parts of Annual Reports and Shareholders Letters
                              Provide a clear and structured answer based on the context provided.
                              Return any tables and relevant content as html.
                              When relevant, use bullet points and lists to structure your answers.";

            initPrompt += $"The current date is {DateTime.Now.ToShortDateString()}";

            var sourcesPrompt = @"When relevant, use facts and numbers from the following documents in your answer.
                              Whenever you use information from a document, reference it at the end of the sentence (ex: [DOC 2 - FileName]).
                              You don't have to use all documents, only if it makes sense in the conversation.
                              If no relevant information to answer the question is present in the documents,
                              just say you don't have enough information to answer.
                           
                              ";


            // Filter and sort the sources by Score property in descending order  
            var filteredAndSortedCognitiveSearchResults = cognitiveSearchResults
                .Where(source => Convert.ToDouble(source.Score) > 0.80)
                .OrderByDescending(source => source.Score);

            // Add the filtered and sorted sources to the sourcesPrompt  
            var index = 0;
            foreach (var source in filteredAndSortedCognitiveSearchResults)
            {
                index++;
                sourcesPrompt += $"DOC {index} (FileName: {source.FileName}, Company: {source.Company}, Year: {source.Year}): {source.Content}\n";
            }


            // Prepare the Completions Options
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

            // If there are previous messages, then include up to 10 of them
            if (previousMessages.Count > 0)
            {
                // Add in the previous messages
                int messagesToSkip = previousMessages.Count - 10;
                if (messagesToSkip < 0) messagesToSkip = 0;

                foreach (var previousMessage in previousMessages.Skip(messagesToSkip))
                {
                    ChatRole chatRole = ChatRole.User;

                    if (previousMessage.Type == "ai")
                    {
                        chatRole = ChatRole.Assistant;
                    }
                    else if (previousMessage.Type == "human")
                    {
                        chatRole = ChatRole.User;
                    }

                    chatCompletionsOptions.Messages.Add(new ChatMessage(chatRole, previousMessage.Content));
                }
            }            

            // Add the prompt message last  
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, prompt));

            // Begin the Query
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