resource openai_resource 'Microsoft.CognitiveServices/accounts@2021-04-30' = {
  name: 'openai-resource'
  location: 'westus'
  kind: 'CognitiveServices'
  sku: {
    name: 'S0'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    apiProperties: {
      skuName: 'S0'
      provisioningState: 'Creating'
      endpoint: 'https://api.openai.com'
    }
    resourceProperties: {
      apiKeys: [
        {
          name: 'default'
          value: '<your-api-key>'
        }
      ]
      parameters: {
        model: 'gpt-3-5-turbo'
      }
    }
  }
}

resource server_farm 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: 'myserverfarm'
  location: 'westus'
  sku: {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    family: 'B'
    capacity: 1
  }
  properties: {
    name: 'basic'
    numberOfWorkers: 1
  }
}

resource webapp 'Microsoft.Web/sites@2021-02-01' = {
  name: 'mywebapp'
  location: 'westus'
  kind: 'app'
  properties: {
    serverFarmId: resourceId('Microsoft.Web/serverfarms', 'myserverfarm')
    siteConfig: {
      appSettings: [
        {
          name: 'CognitiveSearch__IndexName'
          value: 'openai-demo'
          slotSetting: false
        }
        {
          name: 'CognitiveSearch__InstanceName'
          value: 'mycognitivesearch'
          slotSetting: false
        }
        {
          name: 'CognitiveSearch__Key'
          value: listKeys(resourceId('Microsoft.CognitiveServices/accounts', 'openai-resource'), '2021-04-30-preview').key1
          slotSetting: false
        }
        {
          name: 'OpenAI__APIVersion'
          value: '2023-05-15'
          slotSetting: false
        }
        {
          name: 'OpenAI__DeploymentName'
          value: 'myopenai'
          slotSetting: false
        }
        {
          name: 'OpenAI__EmbedDeploymentName'
          value: 'myopenai-embed'
          slotSetting: false
        }
        {
          name: 'OpenAI__InstanceName'
          value: 'myopenai'
          slotSetting: false
        }
        {
          name: 'OpenAI__Key'
          value: listKeys(resourceId('Microsoft.CognitiveServices/accounts', 'openai-resource'), '2021-04-30-preview').key1
          slotSetting: false
        }
      ]
    }
    sourceControl: {
      location: 'westus'
      repoUrl: 'https://github.com/pjgpetecodes/openaidemo-webapp'
      branch: 'main'
      publishRunbook: null
      isManualIntegration: false
      deploymentRollbackEnabled: false
      apiType: 'GitHub'
      branchRef: null
      repositoryToken: null
    }
  }
  dependsOn: [
    resourceId('Microsoft.Web/serverfarms', 'myserverfarm')
    resourceId('Microsoft.CognitiveServices/accounts', 'openai-resource')
    resourceId('Microsoft.Search/searchServices', 'mysearch')
  ]
}
