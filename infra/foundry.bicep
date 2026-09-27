@description('Name of the Microsoft Foundry resource.')
param foundryName string

@description('Name of the Microsoft Foundry project.')
param projectName string

@description('Location of the Microsoft Foundry resource.')
param location string

@allowed([
  'S0'
])
param sku string

resource foundry 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: foundryName
  location: location
  sku: {
    name: sku
  }
  kind: 'AIServices'
  properties: {
    allowProjectManagement: true
    customSubDomainName: foundryName
    disableLocalAuth: false
  }
}

resource aiProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  name: projectName
  parent: foundry
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01'= {
  parent: foundry
  name: 'gpt-6-luna'
  sku : {
    capacity: 1
    name: 'GlobalStandard'
  }
  properties: {
    model:{
      name: 'gpt-6-luna'
      format: 'OpenAI'
      version: '2026-09-22'
    }
  }
}

output foundryName string = foundry.name
output projectName string = aiProject.name
