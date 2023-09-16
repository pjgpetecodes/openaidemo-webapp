# Instructions

Create a secrets.json file with the following content (In visual studio, right click on the Server project and click Manage User Secrets);

{
  "OpenAI": {
    "Key": "<OpenAI Primary Key>",
    "APIVersion": "2023-05-15",
    "InstanceName": "<OpenAI Instance Name>",
    "DeploymentName": "<OpenAI Model Deployment Name>",
    "EmbedDeploymentName": "<OpenAI Embedding Model Deployment Name>"
  },
  "CognitiveSearch": {
    "Key": "<Cognitive Search Key>",
    "IndexName": "<Cognitive Search Index Name>",
    "InstanceName": "<Cognitive Search Instance Name>"
  }
}