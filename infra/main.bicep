targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Name of the resource group')
param resourceGroupName string = ''

@description('Name of the Azure AI Search service')
param searchServiceName string = ''

@description('SKU for the Azure AI Search service')
@allowed([
  'basic'
  'standard'
  'standard2'
  'standard3'
  'storage_optimized_l1'
  'storage_optimized_l2'
])
param searchServiceSku string = 'standard'

@description('Number of replicas for the search service')
@minValue(1)
@maxValue(12)
param searchServiceReplicaCount int = 2

@description('Number of partitions for the search service')
@minValue(1)
@maxValue(12)
param searchServicePartitionCount int = 2

@description('Public network access setting')
@allowed([
  'enabled'
  'disabled'
])
param searchServicePublicNetworkAccess string = 'enabled'

@description('Id of the user or app to assign application roles')
param principalId string = ''

// Tags that should be applied to all resources
var tags = {
  'azd-env-name': environmentName
}

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// Azure AI Search
module search 'core/search/search-services.bicep' = {
  name: 'search'
  scope: rg
  params: {
    name: !empty(searchServiceName) ? searchServiceName : '${abbrs.searchSearchServices}${environmentName}'
    location: location
    tags: tags
    sku: {
      name: searchServiceSku
    }
    replicaCount: searchServiceReplicaCount
    partitionCount: searchServicePartitionCount
    publicNetworkAccess: searchServicePublicNetworkAccess
    principalId: principalId
  }
}

// Outputs
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name

output AZURE_SEARCH_SERVICE_NAME string = search.outputs.name
output AZURE_SEARCH_SERVICE_ENDPOINT string = search.outputs.endpoint
output AZURE_SEARCH_SERVICE_ID string = search.outputs.id

// Load abbreviations from JSON file
var abbrs = loadJsonContent('./abbreviations.json')
