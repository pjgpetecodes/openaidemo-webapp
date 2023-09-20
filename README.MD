# Azure OpenAI and Cognitive Search Demo

## Instructions

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

## File Upload Requirements

- Only PDFs are Supported
- Name your files as follows (4 digit year);

`<Company>-<Year>-<FileName>.pdf`

## Example Queries

What is Microsoft's mission?
What is GamePass?
How many schools has the Technology Education and Learning Support (TEALS) program helped?
How much financial and technology assistance has Microsoft committed to for Ukraine in 2022?
How many identity threats did Microsoft block in 2022?
In coming years how much will technology as a percentage of GDP increase?
In modern work, How many monthly active users of Microsoft Teams in 2022?
What dividends were declared by the Microsoft directors in 2022? Give the answer as an HTML table.
What ambitions drive Microsoft?
How close are Microsoft to doubling the number of Black and African American people managers?
How many people did microsoft employ on a full time basis in 2022?
In 2021, how much did racial and ethnic minority employees earn compared to their white counterparts?
What do Microsoft do to support a culture of Learning and Development?