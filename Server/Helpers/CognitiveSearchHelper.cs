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

namespace openaidemo_webapp.Server.Helpers
{
    public class CognitiveSearchHelper
    {
        private readonly IConfiguration _config;
        private const int ModelDimensions = 1536;
        private const string SemanticSearchConfigName = "my-semantic-config";
        
        public CognitiveSearchHelper(IConfiguration config)
        {
            _config = config;
        }

        public async Task<ExtractionResult> CreateOrUpdateIndex(ExtractionResult extractionResult)
        {

            try
            {
                // Cognitive Search Environment Variables
                var cognitiveSearchKey = _config["CognitiveSearch:Key"] ?? string.Empty;
                var cognitiveSearchEndpoint = $"https://{_config["CognitiveSearch:InstanceName"]}.search.windows.net";
                var indexName = _config["CognitiveSearch:IndexName"] ?? string.Empty;

                // Open AI Environment Variables
                var openAIApiKey = _config["OpenAI:Key"] ?? string.Empty;
                var openAIEndpoint = $"https://{_config["OpenAI:InstanceName"]}.openai.azure.com";
                var embeddingModelDeploymentName = _config["OpenAI:EmbedDeploymentName"] ?? string.Empty;

                // Initialize OpenAI client  
                var credential = new AzureKeyCredential(openAIApiKey);
                var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), credential);

                // Initialize Azure Cognitive Search clients  
                var searchCredential = new AzureKeyCredential(cognitiveSearchKey);
                var indexClient = new SearchIndexClient(new Uri(cognitiveSearchEndpoint), searchCredential);
                var searchClient = indexClient.GetSearchClient(indexName);

                // Create the search index  
                indexClient.CreateOrUpdateIndex(GetIndex(indexName));

                // Create the Vectors for the paragraphs
                var indexDocuments = await ProcessExtractionsAsync(openAIClient, extractionResult.ExtractedParagraphs);
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
                        Location = document["Location"].ToString(),
                        Title = document["Title"].ToString(),
                        Content = document["Content"].ToString(),
                        ContentVector = document["ContentVector"] as float[]
                    };
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {

                throw;
            }            

        }

        internal static SearchIndex GetIndex(string name)
        {
            string vectorSearchConfigName = "my-vector-config";

            SearchIndex searchIndex = new(name)
            {
                VectorSearch = new()
                {
                    AlgorithmConfigurations =
                    {
                        new HnswVectorSearchAlgorithmConfiguration(vectorSearchConfigName)
                    }
                },
                SemanticSettings = new()
                {
                    Configurations =
                    {
                       new SemanticConfiguration(SemanticSearchConfigName, new()
                       {
                           TitleField = new(){ FieldName = "title" },
                           ContentFields =
                           {
                               new() { FieldName = "content" }
                           },
                           KeywordFields =
                           {
                               new() { FieldName = "location" },
                               new() { FieldName = "company" },
                               new() { FieldName = "year" }
                           }

                       })

                    },
                },
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField("title") { IsFilterable = true, IsSortable = true },
                    new SearchableField("content") { IsFilterable = true },
                    new SearchableField("location") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField("company") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField("year") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchField("titleVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ModelDimensions,
                        VectorSearchConfiguration = vectorSearchConfigName
                    },
                    new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ModelDimensions,
                        VectorSearchConfiguration = vectorSearchConfigName
                    }
                }
            };

            return searchIndex;
        }

        private async Task<IReadOnlyList<float>> GenerateEmbeddings(string text, OpenAIClient openAIClient)
        {
            var embeddingModelDeploymentName = _config["OpenAI:EmbedDeploymentName"];

            var response = await openAIClient.GetEmbeddingsAsync(embeddingModelDeploymentName, new EmbeddingsOptions(text));
            return response.Value.Data[0].Embedding;
        }

        internal async Task<List<SearchDocument>> ProcessExtractionsAsync(OpenAIClient openAIClient, List<ExtractedParagraph> extractedParagraphs)
        {
            List<SearchDocument> searchDocuments = new List<SearchDocument>();

            foreach (ExtractedParagraph extraction in extractedParagraphs)
            {
                string title = extraction.Title?.ToString() ?? string.Empty;
                string content = extraction.Content?.ToString() ?? string.Empty;

                float[] contentEmbeddings = (await GenerateEmbeddings(content, openAIClient)).ToArray();

                extraction.ContentVector = contentEmbeddings;

                // Create a dictionary from the ExtractedParagraph object  
                IDictionary<string, object> extractionDict = new Dictionary<string, object>
                {
                    { "Id", extraction.Id },
                    { "Location", extraction.Location },
                    { "Title", extraction.Title },
                    { "Content", extraction.Content },
                    { "ContentVector", extraction.ContentVector }
                };

                // Pass the dictionary to the SearchDocument constructor  
                searchDocuments.Add(new SearchDocument(extractionDict));
            }

            return searchDocuments;
        }

        public async Task<SearchResults<SearchDocument>> SingleVectorSearch(string query, int k = 3)
        {
            try
            {
                var cognitiveSearchKey = _config["CognitiveSearch:Key"] ?? string.Empty;
                var cognitiveSearchEndpoint = $"https://{_config["CognitiveSearch:InstanceName"]}.search.windows.net";
                var indexName = _config["CognitiveSearch:IndexName"] ?? string.Empty;

                // Open AI Environment Variables
                var openAIApiKey = _config["OpenAI:Key"] ?? string.Empty;
                var openAIEndpoint = $"https://{_config["OpenAI:InstanceName"]}.openai.azure.com";
                var embeddingModelDeploymentName = _config["OpenAI:EmbedDeploymentName"] ?? string.Empty;

                // Initialize OpenAI client  
                var credential = new AzureKeyCredential(openAIApiKey);
                var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), credential);

                // Initialize Azure Cognitive Search clients  
                var searchCredential = new AzureKeyCredential(cognitiveSearchKey);
                var indexClient = new SearchIndexClient(new Uri(cognitiveSearchEndpoint), searchCredential);
                var searchClient = indexClient.GetSearchClient(indexName);

                // Generate the embedding for the query  
                var queryEmbeddings = await GenerateEmbeddings(query, openAIClient);

                // Perform the vector similarity search  
                var searchOptions = new SearchOptions
                {
                    Vectors = { new() { Value = queryEmbeddings.ToArray(), KNearestNeighborsCount = 3, Fields = { "contentVector" } } },
                    Size = k,
                    Select = { "title", "content", "company", "location" },
                };

                SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(null, searchOptions);

                int count = 0;
                await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
                {
                    count++;
                    System.Diagnostics.Debug.Print($"Title: {result.Document["title"]}");
                    System.Diagnostics.Debug.Print($"Location: {result.Document["location"]}\n");
                    System.Diagnostics.Debug.Print($"Score: {result.Score}\n");
                    System.Diagnostics.Debug.Print($"Content: {result.Document["content"]}");
                    System.Diagnostics.Debug.Print($"Company: {result.Document["company"]}\n");
                }
                Console.WriteLine($"Total Results: {count}");

                return response;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }



    }
}