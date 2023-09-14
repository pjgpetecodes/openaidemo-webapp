using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;

namespace openaidemo_webapp.Server.Helpers
{
    public class CognitiveSearchHelper
    {
        private readonly IConfiguration _config;

        public CognitiveSearchHelper(IConfiguration config)
        {
            _config = config;
        }

        public async Task<String> QueryOpenAIWithPrompts(String prompt)
        {
            string key = _config["OpenAI:Key"];
            string instanceName = _config["OpenAI:InstanceName"];
            string endpoint = $"https://{instanceName}.openai.azure.com/";
            string deploymentName = _config["OpenAI:DeploymentName"];

            // Create a new Azure OpenAI Client
            var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));            
            

            return "";
        }
    }
}