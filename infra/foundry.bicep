@description('Name of the Microsoft Foundry resource.')
param name string

@description('Location of the Microsoft Foundry resource.')
param location string

@allowed([
  'S0'
])
param sku string

resource foundry 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: name
  location: location
  sku: {
    name: sku
  }
  kind: 'AIServices'
  properties: {
    allowProjectManagement: true
    customSubDomainName: name
    apiProperties: {
      statisticsEnabled: false
    }
  }
}

output name string = foundry.name
