using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using openaidemo_webapp.Shared;
using Microsoft.AspNetCore.SignalR;

namespace openaidemo_webapp.Server.Helpers
{
    public class CognitiveSearchHelper
    {
        private readonly IConfiguration _config;
        private const int ModelDimensions = 1536;
        private const string vectorSearchProfileName = "my-vector-profile";
        private const string vectorSearchConfigName = "my-vector-config";
        private const string SemanticSearchConfigName = "my-semantic-config";

        private string cognitiveSearchKey;
        private string cognitiveSearchEndpoint;
        private string indexName;

        private string openAIApiKey;
        private string openAIEndpoint;
        private string embeddingModelDeploymentName;

        private AzureKeyCredential openAICredential;
        private OpenAIClient openAIClient;

        private AzureKeyCredential searchCredential;
        private SearchIndexClient indexClient;
        private SearchClient searchClient;

        //
        // Class Newed Up
        //
        public CognitiveSearchHelper(IConfiguration config)
        {
            _config = config;

            InitialiseSearchObjects();
        }

        //
        // Initialise the Search and OpenAI Objects
        //
        private void InitialiseSearchObjects()
        {
            // Cognitive Search Environment Variables
            cognitiveSearchKey = _config["CognitiveSearch:Key"] ?? string.Empty;
            cognitiveSearchEndpoint = $"https://{_config["CognitiveSearch:InstanceName"]}.search.windows.net";
            indexName = _config["CognitiveSearch:IndexName"] ?? string.Empty;

            // Open AI Environment Variables
            openAIApiKey = _config["OpenAI:Key"] ?? string.Empty;
            openAIEndpoint = $"https://{_config["OpenAI:InstanceName"]}.openai.azure.com";
            embeddingModelDeploymentName = _config["OpenAI:EmbedDeploymentName"] ?? string.Empty;

            // Initialize OpenAI client  
            openAICredential = new AzureKeyCredential(openAIApiKey);
            openAIClient = new OpenAIClient(new Uri(openAIEndpoint), openAICredential);

            // Initialize Azure Cognitive Search clients  
            searchCredential = new AzureKeyCredential(cognitiveSearchKey);
            indexClient = new SearchIndexClient(new Uri(cognitiveSearchEndpoint), searchCredential);
            searchClient = indexClient.GetSearchClient(indexName);
        }

        //
        // Create or Update an Index for the supplied PDF Extraction Results
        //
        public async Task<ExtractionResult> CreateOrUpdateIndex(ExtractionResult extractionResult, ISingleClientProxy signalRClient)
        {
            try
            {
                
                // Create the search index  
                indexClient.CreateOrUpdateIndex(ComposeIndex(indexName));

                // Create the Vectors for the paragraphs
                var indexDocuments = await ProcessExtractionsAsync(openAIClient, extractionResult.ExtractedParagraphs, extractionResult.FileName, extractionResult.Company, extractionResult.Year, signalRClient);
                await searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(indexDocuments));

                // Convert sampleDocuments back to ExtractionResult  
                ExtractionResult result = new ExtractionResult
                {
                    FileName = extractionResult.FileName,
                    Company = extractionResult.Company,
                    Year = extractionResult.Year,
                };

                result.ExtractedParagraphs = indexDocuments.Select(document =>
                {
                    return new ExtractedParagraph
                    {
                        Id = document["Id"].ToString(),
                        Location = document["Location"].ToString() ?? string.Empty,
                        Title = document["Title"].ToString() ?? string.Empty,
                        Content = document["Content"].ToString() ?? string.Empty,
                        ContentVector = document["ContentVector"] as float[] ?? new float[] { },
                    };
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"Error Creating Index: {ex}");
                throw;
            }            

        }

        //
        // Get a SearchIndex object with the specified name.  
        //
        internal static SearchIndex ComposeIndex(string name)
        {
            // Create a new SearchIndex object with the specified name.  
            SearchIndex searchIndex = new SearchIndex(name)
            {
                // Configure the vector search settings.  
                VectorSearch = new VectorSearch()
                {
                    Profiles =
                    {
                        new VectorSearchProfile(vectorSearchProfileName, vectorSearchConfigName)
                    },
                    Algorithms =
                    {  
                        // Use the HNSW vector search algorithm with the specified configuration name.  
                        new HnswVectorSearchAlgorithmConfiguration(vectorSearchConfigName)
                    }
                },
                // Configure the semantic search settings.  
                SemanticSettings = new()
                {
                    Configurations =
                    {
                        new SemanticConfiguration(SemanticSearchConfigName, new()
                        {  
                            // Define the fields used in semantic search.  
                            TitleField = new(){ FieldName = "title" },
                            ContentFields =
                            {
                                new() { FieldName = "content" }
                            },
                            KeywordFields =
                            {
                                new() { FieldName = "location" },
                                new() { FieldName = "company" },
                                new() { FieldName = "year" },
                                new() { FieldName = "fileName"}
                            }

                        })
                    },
                },
                // Define the fields used in the search index.  
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String) 
                    { 
                        IsKey = true, 
                        IsFilterable = true, 
                        IsSortable = true, 
                        IsFacetable = false 
                    },
                    new SearchableField("title") 
                    { 
                        IsFilterable = true, 
                        IsSortable = true 
                    },
                    new SearchableField("content") 
                    {
                        IsFilterable = true,
                        IsSortable = false,
                        IsFacetable = false
                          
                    },
                    new SearchableField("location") 
                    { 
                        IsFilterable = true, 
                        IsSortable = true, 
                        IsFacetable = false 
                    },
                    new SearchableField("company") 
                    { 
                        IsFilterable = true, 
                        IsSortable = true, 
                        IsFacetable = true 
                    },
                    new SearchableField("year") 
                    { 
                        IsFilterable = true, 
                        IsSortable = true, 
                        IsFacetable = true 
                    },
                    new SearchableField("fileName") 
                    { 
                        IsFilterable = true, 
                        IsSortable = true, 
                        IsFacetable = false 
                    },  
                    // Configure the vector search fields for title and content.  
                    new SearchField("titleVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        IsFacetable = false,
                        VectorSearchDimensions = ModelDimensions,
                        VectorSearchProfile = vectorSearchProfileName
                    },
                    new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        IsFacetable = false,
                        VectorSearchDimensions = ModelDimensions,
                        VectorSearchProfile = vectorSearchProfileName
                    }
                }
            };

            // Return the configured SearchIndex object.  
            return searchIndex;
        }

        //
        // Generate Embeddings for all of the extracted PDF Paragraphs
        //
        internal async Task<List<SearchDocument>> ProcessExtractionsAsync(OpenAIClient openAIClient, List<ExtractedParagraph> extractedParagraphs, String FileName, String Company, String Year, ISingleClientProxy signalRClient)
        {
            List<SearchDocument> searchDocuments = new List<SearchDocument>();

            await signalRClient.SendAsync("UpdateFileUploadStatus", $"Beginning processing of {extractedParagraphs.Count} extractions for {FileName}");

            var index = 0;

            foreach (ExtractedParagraph extraction in extractedParagraphs)
            {
                index++;
                string title = extraction.Title?.ToString() ?? string.Empty;
                string content = extraction.Content?.ToString() ?? string.Empty;

                float[] contentEmbeddings = (await GenerateEmbeddings(content, openAIClient)).ToArray();

                extraction.ContentVector = contentEmbeddings;

                // Create a dictionary from the Extracted Paragraph object  
                IDictionary<string, object> extractionDict = new Dictionary<string, object>
                {
                    { "Id", extraction.Id },
                    { "FileName", FileName ?? string.Empty },
                    { "Location", extraction.Location ?? string.Empty },
                    { "Title", extraction.Title ?? string.Empty },
                    { "Content", extraction.Content ?? string.Empty },
                    { "Company", Company ?? string.Empty },
                    { "Year", Year ?? string.Empty },
                    { "ContentVector", extraction.ContentVector }
                };

                // Create a new Cognitive Search Search Document from the Extracted Paragraph
                searchDocuments.Add(new SearchDocument(extractionDict));

                await signalRClient.SendAsync("UpdateFileUploadStatus", $"Processed {index} extractions of {extractedParagraphs.Count} for {FileName}");

            }

            return searchDocuments;
        }

        //
        // Generate OpenAI Embeddings for the given text
        //
        private async Task<IReadOnlyList<float>> GenerateEmbeddings(string text, OpenAIClient openAIClient)
        {
            var embeddingModelDeploymentName = _config["OpenAI:EmbedDeploymentName"];

            var response = await openAIClient.GetEmbeddingsAsync(embeddingModelDeploymentName, new EmbeddingsOptions(text));
            return response.Value.Data[0].Embedding;
        }

        //
        // Perform a single Vector Search against the supplied query string
        //
        public async Task<List<CognitiveSearchResult>> SingleVectorSearch(string query, int k = 6, string company = "", string year = "")
        {
            try
            {
                // Generate the embedding for the query  
                var queryEmbeddings = await GenerateEmbeddings(query, openAIClient);

                // Perform the vector similarity search  
                var searchOptions = new SearchOptions
                {
                   
                    VectorQueries = { new RawVectorQuery() { Vector = queryEmbeddings.ToArray(), KNearestNeighborsCount = k, Fields = { "contentVector" } } },
                    Size = k,
                    Select = { "title", "content", "company", "location", "fileName", "year" },
                };

                // Add any filters passed in
                if (company != "")
                {
                    searchOptions.Filter = $"company eq '{company}'";

                    if (year != "")
                    {
                        searchOptions.Filter += $" and year eq '{year}'";

                    }
                }
                else if (year != "")
                {
                    searchOptions.Filter = $"year eq '{year}'";
                }

                SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(null, searchOptions);

                int count = 0;
                List<CognitiveSearchResult> cognitiveSearchResults = new List<CognitiveSearchResult>();

                await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
                {
                    count++;
                    
                    CognitiveSearchResult cognitiveSearchResult = new CognitiveSearchResult();
                    cognitiveSearchResult.Id = count;
                    cognitiveSearchResult.FileName = result.Document["fileName"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Title = result.Document["title"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Location = result.Document["location"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Score = result.Score.ToString() ?? string.Empty;
                    cognitiveSearchResult.Content = result.Document["content"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Company = result.Document["company"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Year = result.Document["year"].ToString() ?? string.Empty;

                    cognitiveSearchResults.Add(cognitiveSearchResult);

                    System.Diagnostics.Debug.Print($"Title: {result.Document["title"]}");
                    System.Diagnostics.Debug.Print($"Location: {result.Document["location"]}\n");
                    System.Diagnostics.Debug.Print($"Score: {result.Score}\n");
                    System.Diagnostics.Debug.Print($"Content: {result.Document["content"]}\n");
                    System.Diagnostics.Debug.Print($"Company: {result.Document["company"]}\n");
                    System.Diagnostics.Debug.Print($"Year: {result.Document["year"]}\n");
                }

                System.Diagnostics.Debug.Print($"Total Results: {count}");

                // Filter and sort the sources by Score property in descending order  
                List<CognitiveSearchResult> filteredAndSortedCognitiveSearchResults = cognitiveSearchResults
                    .Where(source => Convert.ToDouble(source.Score) > 0.30)
                    .OrderByDescending(source => source.Score).ToList();

                System.Diagnostics.Debug.Print($"Filtered Results: {filteredAndSortedCognitiveSearchResults.Count}");

                //return filteredAndSortedCognitiveSearchResults;

                return cognitiveSearchResults;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"Error performing Vector Search: {ex}");
                throw;
            }
            
        }

        //
        // Perform a Hybrid (Standard + a Vector) Search
        //
        public async Task<List<CognitiveSearchResult>> SimpleHybridSearch(string query, int k = 6, string company = "", string year = "")
        {
            try
            {
                // Generate the embedding for the query  
                var queryEmbeddings = await GenerateEmbeddings(query, openAIClient);

                // Perform the vector similarity search  
                var searchOptions = new SearchOptions
                {
                    VectorQueries = { new RawVectorQuery() { Vector = queryEmbeddings.ToArray(), KNearestNeighborsCount = k, Fields = { "contentVector" } } },
                    Size = k,
                    Select = { "title", "content", "company", "location", "fileName", "year" },
                };

                // Add any filters passed in
                if (company != "")
                {
                    searchOptions.Filter = $"company eq '{company}'";

                    if (year != "")
                    {
                        searchOptions.Filter += $" and year eq '{year}'";

                    }
                }
                else if (year != "")
                {
                    searchOptions.Filter = $"year eq '{year}'";
                }

                SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(query, searchOptions);

                int count = 0;
                List<CognitiveSearchResult> cognitiveSearchResults = new List<CognitiveSearchResult>();

                await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
                {
                    count++;

                    CognitiveSearchResult cognitiveSearchResult = new CognitiveSearchResult();
                    cognitiveSearchResult.Id = count;
                    cognitiveSearchResult.FileName = result.Document["fileName"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Title = result.Document["title"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Location = result.Document["location"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Score = result.Score.ToString() ?? string.Empty;
                    cognitiveSearchResult.Content = result.Document["content"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Company = result.Document["company"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Year = result.Document["year"].ToString() ?? string.Empty;

                    cognitiveSearchResults.Add(cognitiveSearchResult);

                    System.Diagnostics.Debug.Print($"Title: {result.Document["title"]}");
                    System.Diagnostics.Debug.Print($"Location: {result.Document["location"]}\n");
                    System.Diagnostics.Debug.Print($"Score: {result.Score}\n");
                    System.Diagnostics.Debug.Print($"Content: {result.Document["content"]}\n");
                    System.Diagnostics.Debug.Print($"Company: {result.Document["company"]}\n");
                    System.Diagnostics.Debug.Print($"Year: {result.Document["year"]}\n");
                }

                System.Diagnostics.Debug.Print($"Total Results: {count}");

                // Filter and sort the sources by Score property in descending order  
                List<CognitiveSearchResult> filteredAndSortedCognitiveSearchResults = cognitiveSearchResults
                    .Where(source => Convert.ToDouble(source.Score) > 0.30)
                    .OrderByDescending(source => source.Score).ToList();

                System.Diagnostics.Debug.Print($"Filtered Results: {filteredAndSortedCognitiveSearchResults.Count}");

                //return filteredAndSortedCognitiveSearchResults;

                return cognitiveSearchResults;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"Error performing Vector Search: {ex}");
                throw;
            }
        }

        //
        // Perform a Semantic Hybrid Search
        //
        public async Task<List<CognitiveSearchResult>> SemanticHybridSearch(string query, int k = 10, string company = "", string year = "")
        {
            try
            {
                // Generate the embedding for the query  
                var queryEmbeddings = await GenerateEmbeddings(query, openAIClient);

                // Perform the vector similarity search  
                var searchOptions = new SearchOptions
                {
                    VectorQueries = { new RawVectorQuery() { Vector = queryEmbeddings.ToArray(), KNearestNeighborsCount = k, Fields = { "contentVector" } } },
                    Size = k,
                    QueryType = SearchQueryType.Semantic,
                    SemanticConfigurationName = SemanticSearchConfigName,
                    QueryCaption = QueryCaptionType.Extractive,
                    QueryAnswer = QueryAnswerType.Extractive,
                    QueryCaptionHighlightEnabled = true,
                    Select = { "title", "content", "company", "location", "fileName", "year" },
                };

                // Add any filters passed in
                if (company != "")
                {
                    searchOptions.Filter = $"company eq '{company}'";

                    if (year != "")
                    {
                        searchOptions.Filter += $" and year eq '{year}'";

                    }
                }
                else if (year != "")
                {
                    searchOptions.Filter = $"year eq '{year}'";
                }

                SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(query, searchOptions);

                int count = 0;
                List<CognitiveSearchResult> cognitiveSearchResults = new List<CognitiveSearchResult>();

                await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
                {
                    count++;

                    CognitiveSearchResult cognitiveSearchResult = new CognitiveSearchResult();
                    cognitiveSearchResult.Id = count;
                    cognitiveSearchResult.FileName = result.Document["fileName"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Title = result.Document["title"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Location = result.Document["location"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Score = result.Score.ToString() ?? string.Empty;
                    cognitiveSearchResult.Content = result.Document["content"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Company = result.Document["company"].ToString() ?? string.Empty;
                    cognitiveSearchResult.Year = result.Document["year"].ToString() ?? string.Empty;
                    cognitiveSearchResult.captionHighlight = result.Captions.FirstOrDefault().Highlights ?? string.Empty;
                    cognitiveSearchResult.captionText = result.Captions.FirstOrDefault().Text ?? string.Empty;

                    cognitiveSearchResults.Add(cognitiveSearchResult);

                    System.Diagnostics.Debug.Print($"Title: {result.Document["title"]}");
                    System.Diagnostics.Debug.Print($"Location: {result.Document["location"]}\n");
                    System.Diagnostics.Debug.Print($"Score: {result.Score}\n");
                    System.Diagnostics.Debug.Print($"Content: {result.Document["content"]}\n");
                    System.Diagnostics.Debug.Print($"Company: {result.Document["company"]}\n");
                    System.Diagnostics.Debug.Print($"Year: {result.Document["year"]}\n");
                }

                System.Diagnostics.Debug.Print($"Total Results: {count}");

                // Filter and sort the sources by Score property in descending order  
                List<CognitiveSearchResult> filteredAndSortedCognitiveSearchResults = cognitiveSearchResults
                    .Where(source => Convert.ToDouble(source.Score) > 0.30)
                    .OrderByDescending(source => source.Score).ToList();

                System.Diagnostics.Debug.Print($"Filtered Results: {filteredAndSortedCognitiveSearchResults.Count}");

                //return filteredAndSortedCognitiveSearchResults;

                return cognitiveSearchResults;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"Error performing Vector Search: {ex}");
                throw;
            }
        }

        //
        // Get all of the available Facets
        //
        public CognitiveSearchFacets GetAllFacets(string query="")
        {

            try
            {
                SearchOptions options;
                Response<SearchResults<CognitiveSearchResult>> response;
                IDictionary<string, IList<FacetResult>> facetResults;
                List<KeyValuePair<string, string>> facets = new List<KeyValuePair<string, string>>();

                options = new SearchOptions()
                {
                    IncludeTotalCount = true,
                    Filter = string.Empty,
                    OrderBy = { string.Empty },
                    QueryType = SearchQueryType.Simple
                };

                options.Facets.Add("company");
                options.Facets.Add("year");
                
                response = searchClient.Search<CognitiveSearchResult>("*", options);
                facetResults = response.Value.Facets;

                var cognitiveSearchFacets = new CognitiveSearchFacets();
                cognitiveSearchFacets.CognitiveSearchFacetList = new List<CognitiveSearchFacet>();

                // Foreach through each facetresult  
                foreach (KeyValuePair<string, IList<FacetResult>> facetEntry in facetResults)
                {
                    string entryKey = facetEntry.Key;
                    IList<FacetResult> entryFacetResults = facetEntry.Value;

                    CognitiveSearchFacet cognitiveSearchFacet = new CognitiveSearchFacet()
                    {
                        facetName = entryKey,
                        cognitiveSearchFacetResults = new List<CognitiveSearchFacetResult>()
                    };

                    foreach (FacetResult facetResult in entryFacetResults)
                    {
                        CognitiveSearchFacetResult cognitiveSearchFacetResult = new CognitiveSearchFacetResult()
                        {
                            facetResultName = facetResult.Value.ToString() ?? string.Empty,
                            facetResultCount = facetResult.Count
                        };
                        cognitiveSearchFacet.cognitiveSearchFacetResults.Add(cognitiveSearchFacetResult);
                    }

                    cognitiveSearchFacets.CognitiveSearchFacetList.Add(cognitiveSearchFacet);
                }

                return cognitiveSearchFacets;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                return new CognitiveSearchFacets();
            }


        }
    }
}