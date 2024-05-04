using Microsoft.AspNetCore.SignalR.Client;
using openaidemo_webapp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

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

        private string _selectedCompany;
        public string SelectedCompany
        {
            get => _selectedCompany;
            set
            {
                _selectedCompany = value;
                OnPropertyChanged(nameof(SelectedCompany));
            }
        }

        private string _selectedYear;
        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged(nameof(SelectedYear));
            }
        }

        private ObservableCollection<string> _companies = new ObservableCollection<string>();
        public ObservableCollection<string> Companies
        {
            get => _companies;
            set
            {
                _companies = value;
                OnPropertyChanged(nameof(Companies));
            }
        }

        private ObservableCollection<string> _years = new ObservableCollection<string>();
        public ObservableCollection<string> Years
        {
            get => _years;
            set
            {
                _years = value;
                OnPropertyChanged(nameof(Years));
            }
        }

        private List<CognitiveSearchResult> _cognitiveSearchResults = new List<CognitiveSearchResult>();
        public List<CognitiveSearchResult> CognitiveSearchResults
        {
            get => _cognitiveSearchResults;
            set
            {
                _cognitiveSearchResults = value;
                OnPropertyChanged(nameof(CognitiveSearchResults));
            }
        }

        private ObservableCollection<CognitiveSearchFacet> _cognitiveSearchFacetResults = new ObservableCollection<CognitiveSearchFacet>();
        public ObservableCollection<CognitiveSearchFacet> CognitiveSearchFacetResults
        {
            get => _cognitiveSearchFacetResults;
            set
            {
                _cognitiveSearchFacetResults = value;
                OnPropertyChanged(nameof(CognitiveSearchFacetResults));
            }
        }


        private HubConnection? hubConnection;
        private bool isLoadingSources = false;
        private bool isLoadingFilters = false;
        private bool includePreviousMessages = true;
        public event Action ChatMessageAdded;

        public ChatViewModel()
        {

            // Create some dummy CognitiveSearchResults
            CognitiveSearchResults.Add(new CognitiveSearchResult { Id = 1, Title = "Title 1", Content = "Description 1", FileName="xyz-retail-2024-1"});
            CognitiveSearchResults.Add(new CognitiveSearchResult { Id = 2, Title = "Title 2", Content = "Description 2", FileName = "xyz-retail-2024-2" });
            CognitiveSearchResults.Add(new CognitiveSearchResult { Id = 3, Title = "Title 3", Content = "Description 3", FileName = "xyz-retail-2024-3" });

            // Create some OpenAIChatMessages and add them to the ChatMessages list
            AddChatMessage(new OpenAIChatMessage { ChatBubbleId = "1", Content = "Hello, how can I help you?", Type = "human" });
            AddChatMessage(new OpenAIChatMessage { ChatBubbleId = "2", Content = "I am an AI [DOC 3 - xyz-retail-2024-3]", Type = "ai", Sources= CognitiveSearchResults });
            SelectedYear = "";
            SelectedCompany = "";
        }

        /// <summary>
        /// Setup the SignalR Hub Connection
        /// </summary>
        public async void SetupConnection()
        {
            string accessToken = "";

            /*
            try
            {
                var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<ChatViewModel>();

                IConfiguration configuration = builder.Build();

                var clientId = configuration["clientId"];
                var clientName = configuration["clientName"];
                var tenantId = configuration["tenantId"];

                var pca = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithRedirectUri($"msal{clientName}://auth")
                    .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                    .Build();

                var scopes = new string[] { "user.read" };
                var result = await pca.AcquireTokenInteractive(scopes).ExecuteAsync();

                accessToken = result.AccessToken;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            */

            String url = "";

            // If we're debugging locally then use localhost
            if (System.Diagnostics.Debugger.IsAttached)
            {
                url = "https://localhost:7063/chathub";
                //url = "https://10.0.2.2:7063/chathub";
            }
            else
            {
                url = "https://pjgopenaiwebapp.azurewebsites.net/chathub";
            }

            hubConnection = new HubConnectionBuilder()
                   .WithUrl(url, conf =>
                   {
                       //conf.AccessTokenProvider = () => Task.FromResult(accessToken);

                       conf.HttpMessageHandlerFactory = (x) => new HttpClientHandler
                       {
                           ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                       };
                   })
                   .WithAutomaticReconnect()
                   .Build();

            try
            {
                hubConnection.On<string, string, string>("ReceiveMessage", async (responseGuid, user, message) =>
                {
                    Application.Current.MainPage.Dispatcher.Dispatch(async () =>
                    {
                        ChatMessageAdded?.Invoke();
                    });
                });

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
                                AddChatMessage(new OpenAIChatMessage { ChatBubbleId = chatBubbleId, Content = messageToken, Type = "ai", IsTemporaryResponse = isTemporaryResponse, Sources = sources });
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
                        CognitiveSearchResults = new List<CognitiveSearchResult>(cogSearchResults.CognitiveSearchResultList);
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
                        CognitiveSearchFacetResults = new ObservableCollection<CognitiveSearchFacet>(cogSearchFacetResults.CognitiveSearchFacetList);

                        // Fill the Companies dropdown
                        CognitiveSearchFacet? companyFacet = CognitiveSearchFacetResults.FirstOrDefault(f => f.facetName == "company");

                        if (companyFacet != null)
                        {
                            // Sort the facet results and add them to Companies list
                            var sortedCompanies = companyFacet.cognitiveSearchFacetResults
                                .Select(result => result.facetResultName)
                                .OrderBy(name => name)
                                .ToList();

                            Application.Current.MainPage.Dispatcher.Dispatch(() =>
                            {
                                // Clear the existing Companies list
                                Companies.Clear();

                                foreach (var company in sortedCompanies)
                                {
                                    Companies.Add(company);
                                }
                            });
                        }

                        SelectedCompany = "";

                        // Fill the Years dropdown
                        CognitiveSearchFacet? yearFacet = CognitiveSearchFacetResults.FirstOrDefault(f => f.facetName == "year");

                        if (yearFacet != null)
                        {
                            // Sort the facet results and add them to Years list
                            var sortedYears = yearFacet.cognitiveSearchFacetResults
                                .Select(result => result.facetResultName)
                                .OrderBy(name => name)
                                .ToList();

                            Application.Current.MainPage.Dispatcher.Dispatch(() =>
                            {
                                // Clear the existing Years list
                                Years.Clear();

                                foreach (var year in sortedYears)
                                {
                                    Years.Add(year);
                                }
                            });
                        }

                        SelectedYear = "";
                        isLoadingFilters = false;
                        //StateHasChanged();
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                });

                //
                // Start the connection
                //
                await hubConnection.StartAsync();

                //
                // Fill all of the Filter Facets
                //
                await GetFilters();

            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw;
            }
        }

        /// <summary>
        /// Add a Chat Message to the ChatMessages list
        /// </summary>
        /// <param name="message"></param>
        public void AddChatMessage(OpenAIChatMessage message)
        {
            Application.Current.MainPage.Dispatcher.Dispatch(async () =>
            {
                ChatMessages.Add(message);
                ChatMessageAdded?.Invoke();
            });
        }

        //
        // Get the Search Filters
        //
        private async Task GetFilters()
        {
            Companies = new ObservableCollection<string>();
            SelectedCompany = "";
            Years = new ObservableCollection<string>();
            SelectedYear = "";

            isLoadingFilters = true;

            if (hubConnection != null)
            {
                await hubConnection.SendAsync("GetCogSearchFacets", "");
            }
        }

        /// <summary>
        /// Send the Query Message to the SignalR Hub
        /// </summary>
        /// <returns></returns>
        public async Task SendMessage()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ChatInput))
                {
                    CognitiveSearchResults = new List<CognitiveSearchResult>();
                    isLoadingSources = true;
                    String chatMessageGuid = System.Guid.NewGuid().ToString();

                    ObservableCollection<OpenAIChatMessage> previousMessages = new ObservableCollection<OpenAIChatMessage>();
                    foreach (var message in ChatMessages)
                    {
                        previousMessages.Add(message);
                    }

                    AddChatMessage(new OpenAIChatMessage { ChatBubbleId = chatMessageGuid, Content = ChatInput, Type = "human" });

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

                //throw;
            }
        }

        /// <summary>
        /// Handle the INotifyPropertyChanged event
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception)
            {

                //throw;
            }
            
        }


    }
}
