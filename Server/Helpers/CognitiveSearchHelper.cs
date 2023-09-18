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

        //
        // Create or Update an Index for the supplied PDF Extraction Results
        //
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
                var indexDocuments = await ProcessExtractionsAsync(openAIClient, extractionResult.ExtractedParagraphs, extractionResult.FileName, extractionResult.Company, extractionResult.Year);
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

        //
        // Get a SearchIndex object with the specified name.  
        //
        internal static SearchIndex GetIndex(string name)
        {
            // Define the name of the vector search configuration.  
            string vectorSearchConfigName = "my-vector-config";

            // Create a new SearchIndex object with the specified name.  
            SearchIndex searchIndex = new(name)
            {
                // Configure the vector search settings.  
                VectorSearch = new()
                {
                    AlgorithmConfigurations =
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
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField("title") { IsFilterable = true, IsSortable = true },
                    new SearchableField("content") { IsFilterable = true },
                    new SearchableField("location") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField("company") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField("year") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField("fileName") { IsFilterable = true, IsSortable = true, IsFacetable = true },  
                    // Configure the vector search fields for title and content.  
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

            // Return the configured SearchIndex object.  
            return searchIndex;
        }

        //
        // Generate Embeddings for all of the extracted PDF Paragraphs
        //
        internal async Task<List<SearchDocument>> ProcessExtractionsAsync(OpenAIClient openAIClient, List<ExtractedParagraph> extractedParagraphs, String FileName, String Company, String Year)
        {
            List<SearchDocument> searchDocuments = new List<SearchDocument>();

            foreach (ExtractedParagraph extraction in extractedParagraphs)
            {
                string title = extraction.Title?.ToString() ?? string.Empty;
                string content = extraction.Content?.ToString() ?? string.Empty;

                float[] contentEmbeddings = (await GenerateEmbeddings(content, openAIClient)).ToArray();

                extraction.ContentVector = contentEmbeddings;

                // Create a dictionary from the Extracted Paragraph object  
                IDictionary<string, object> extractionDict = new Dictionary<string, object>
                {
                    { "Id", extraction.Id },
                    { "FileName", FileName },
                    { "Location", extraction.Location },
                    { "Title", extraction.Title },
                    { "Content", extraction.Content },
                    { "Company", Company },
                    { "Year", Year },
                    { "ContentVector", extraction.ContentVector }
                };

                // Create a new Cognitive Search Search Document from the Extracted Paragraph
                searchDocuments.Add(new SearchDocument(extractionDict));
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

                // Generate the embedding for the query  
                var queryEmbeddings = await GenerateEmbeddings(query, openAIClient);

                // Perform the vector similarity search  
                var searchOptions = new SearchOptions
                {
                    Vectors = { new() { Value = queryEmbeddings.ToArray(), KNearestNeighborsCount = 6, Fields = { "contentVector" } } },
                    Size = k,
                    Select = { "title", "content", "company", "location", "fileName", "year" },
                };

                // Add any filters passed in
                if (company != "")
                {
                    searchOptions.Filter = $"company eq '{company}'";

                    if (year != "")
                    {
                        searchOptions.Filter = $" and year eq '{year}'";

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
                    cognitiveSearchResult.Score = result.Score.ToString();
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

                return cognitiveSearchResults;
            }
            catch (Exception ex)
            {
                throw;
            }
            
        }

    }
}