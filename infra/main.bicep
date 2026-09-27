targetScope = 'subscription'

@description('Base name of the resource group. A shared unique suffix is appended to this and the Foundry resource name.')
param resourceGroupName string

@description('Location for the resource group and Microsoft Foundry resource.')
param location string

@allowed([
  'S0'
])
param sku string = 'S0'

var uniqueSuffix = uniqueString(subscription().id, resourceGroupName)

resource resourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: '${resourceGroupName}-${uniqueSuffix}'
  location: location
}

module foundry 'foundry.bicep' = {
  name: 'foundry-${uniqueSuffix}'
  scope: resourceGroup
  params: {
    name: 'aif-${uniqueSuffix}'
    location: location
    sku: sku
  }
}

output resourceGroupName string = resourceGroup.name
output foundryName string = foundry.outputs.name
