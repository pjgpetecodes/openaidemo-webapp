﻿using Microsoft.AspNetCore.SignalR.Client;
using openaidemo_webapp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace maui.ViewModels
{
    class ChatViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<OpenAIChatMessage> _chatMessages = new ObservableCollection<OpenAIChatMessage>();
        public ObservableCollection<OpenAIChatMessage> ChatMessages
        {
            get => _chatMessages;
            set
            {
                _chatMessages = value;
                OnPropertyChanged(nameof(ChatMessages)); // Notify the UI of the change
            }
        }

        private String _chatInput;
        public String ChatInput{ 
            get => _chatInput;
            set
            {
                _chatInput = value;
                OnPropertyChanged(nameof(ChatInput));
            }
        }

        public String SelectedCompany { get; set; }

        public String SelectedYear { get; set; }
        public ObservableCollection<String> Companies { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<String> Years { get; set; } = new ObservableCollection<String>();

        List<CognitiveSearchResult> cognitiveSearchResults = new List<CognitiveSearchResult>();
        ObservableCollection<CognitiveSearchFacet> cognitiveSearchFacetResults = new ObservableCollection<CognitiveSearchFacet>();

        private HubConnection? hubConnection;
        private bool isLoadingSources = false;
        private bool isLoadingFilters = false;
        private bool includePreviousMessages = true;
        
            
        public ChatViewModel()
        {

            // Create some dummy CognitiveSearchResults
            cognitiveSearchResults.Add(new CognitiveSearchResult { Id = 1, Title = "Title 1", Content = "Description 1", FileName="xyz-retail-2024-1"});
            cognitiveSearchResults.Add(new CognitiveSearchResult { Id = 2, Title = "Title 2", Content = "Description 2", FileName = "xyz-retail-2024-2" });
            cognitiveSearchResults.Add(new CognitiveSearchResult { Id = 3, Title = "Title 3", Content = "Description 3", FileName = "xyz-retail-2024-3" });

            // Create some OpenAIChatMessages and add them to the ChatMessages list
            ChatMessages.Add(new OpenAIChatMessage { ChatBubbleId = "1", Content = "Hello, how can I help you?", Type = "human" });
            ChatMessages.Add(new OpenAIChatMessage { ChatBubbleId = "2", Content = "I am an AI [DOC 3 - xyz-retail-2024-3]", Type = "ai", Sources=cognitiveSearchResults });
            SelectedYear = "";
            SelectedCompany = "";
        }

        public async void SetupConnection()
        {
            hubConnection = new HubConnectionBuilder()
                   .WithUrl("https://localhost:7063/chathub", conf =>
                   {
                       conf.HttpMessageHandlerFactory = (x) => new HttpClientHandler
                       {
                           ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                       };
                   })
                   .WithAutomaticReconnect()
                   .Build();

            try
            {
                hubConnection.On<string, string, string, bool, List<CognitiveSearchResult>>("ReceiveMessageToken", async (chatBubbleId, user, messageToken, isTemporaryResponse, sources) =>
                {

                    try
                    {
                        // Find the chat message with the supplied chatBubbleId
                        var chatMessage = ChatMessages.Where(chatMessageItem => chatMessageItem.ChatBubbleId == chatBubbleId).FirstOrDefault();

                        if (chatMessage != null)
                        {
                            if (chatMessage.IsTemporaryResponse)
                            {
                                chatMessage.Content = "";
                                chatMessage.IsTemporaryResponse = false;
                            }

                            Application.Current.MainPage.Dispatcher.Dispatch(async () =>
                            {
                                chatMessage.Content = chatMessage.Content + messageToken;
                                OnPropertyChanged(nameof(ChatMessages));
                            });
                        }
                        else
                        {
                            Application.Current.MainPage.Dispatcher.Dispatch(async () =>
                            {
                                ChatMessages.Add(new OpenAIChatMessage { ChatBubbleId = chatBubbleId, Content = messageToken, Type = "ai", IsTemporaryResponse = isTemporaryResponse, Sources = sources });
                                OnPropertyChanged(nameof(ChatMessages));

                            });
                        }

                        
                        //await JS.InvokeVoidAsync("scrollToBottom", "messages");

                        //chatInput.Query = string.Empty;
                        //StateHasChanged();
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                    
                });

                //
                // The Cognitive Search results have been received
                //
                hubConnection.On<CognitiveSearchResults>("CognitiveSearchResults", (cogSearchResults) =>
                {

                    try
                    {
                        isLoadingSources = false;
                        cognitiveSearchResults = new List<CognitiveSearchResult>(cogSearchResults.CognitiveSearchResultList);
                        //StateHasChanged();
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                    
                });

                //
                // Receive all of the Cognitive Search Facets
                //
                hubConnection.On<CognitiveSearchFacets>("CognitiveSearchFacetResults", (cogSearchFacetResults) =>
                {

                    try
                    {
                        cognitiveSearchFacetResults = new ObservableCollection<CognitiveSearchFacet>(cogSearchFacetResults.CognitiveSearchFacetList);

                        //
                        // Fill the companies dropdown
                        //
                        CognitiveSearchFacet? companyFacet = cognitiveSearchFacetResults.FirstOrDefault(f => f.facetName == "company");

                        if (companyFacet != null)
                        {
                            // Iterate through the cognitiveSearchFacetResults and add the facetResultName to Companies list
                            foreach (CognitiveSearchFacetResult result in companyFacet.cognitiveSearchFacetResults)
                            {
                                Companies.Add(result.facetResultName);
                            }
                        }

                        List<string> sortedCompanies = new List<string>(Companies);
                        sortedCompanies.Sort();
                        Companies = new ObservableCollection<string>(sortedCompanies);

                        SelectedCompany = "";

                        //
                        // Fill the Years dropdown
                        //
                        CognitiveSearchFacet? yearFacet = cognitiveSearchFacetResults.FirstOrDefault(f => f.facetName == "year");

                        if (yearFacet != null)
                        {
                            // Iterate through the cognitiveSearchFacetResults and add the facetResultName to Years list
                            foreach (CognitiveSearchFacetResult result in yearFacet.cognitiveSearchFacetResults)
                            {
                                Years.Add(result.facetResultName);
                            }
                        }

                        List<string> sortedYears = new List<string>(Years);
                        sortedYears.Sort();
                        Years = new ObservableCollection<string>(sortedYears);

                        SelectedYear = "";

                        isLoadingFilters = false;
                        //StateHasChanged();
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                    
                });

                await hubConnection.StartAsync();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task SendMessage()
        {

            try
            {
                if (!string.IsNullOrWhiteSpace(ChatInput))
                {
                    cognitiveSearchResults = new List<CognitiveSearchResult>();
                    isLoadingSources = true;
                    String chatMessageGuid = System.Guid.NewGuid().ToString();

                    ObservableCollection<OpenAIChatMessage> previousMessages = new ObservableCollection<OpenAIChatMessage>();
                    foreach (var message in ChatMessages)
                    {
                        previousMessages.Add(message);
                    }

                    ChatMessages.Add(new OpenAIChatMessage { ChatBubbleId = chatMessageGuid, Content = ChatInput, Type = "human" });

                    OnPropertyChanged(nameof(ChatMessages));

                    if (hubConnection != null)
                    {
                        await hubConnection.SendAsync("SendCogSearchQuery", ChatInput, includePreviousMessages ? previousMessages : new List<OpenAIChatMessage>(), SelectedCompany, SelectedYear);
                    }

                    ChatInput = "";
                }
            }
            catch (Exception)
            {

                throw;
            }
            

        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception)
            {

                throw;
            }
            
        }


    }
}
