metadata description = 'Creates an Azure AI Search instance.'

param name string
param location string = resourceGroup().location
param tags object = {}

param sku object = {
  name: 'standard'
}

@description('Number of replicas')
param replicaCount int = 1

@description('Number of partitions')
param partitionCount int = 1

@description('Hosting mode')
@allowed([
  'default'
  'highDensity'
])
param hostingMode string = 'default'

@description('Public network access')
@allowed([
  'enabled'
  'disabled'
])
param publicNetworkAccess string = 'enabled'

@description('Authentication options')
param authOptions object = {}

@description('Disable local authentication')
param disableLocalAuth bool = false

@description('Semantic search')
@allowed([
  'disabled'
  'free'
  'standard'
])
param semanticSearch string = 'disabled'

@description('Principal ID to assign roles to')
param principalId string = ''

resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    hostingMode: hostingMode
    partitionCount: partitionCount
    replicaCount: replicaCount
    publicNetworkAccess: publicNetworkAccess
    authOptions: !empty(authOptions) ? authOptions : null
    disableLocalAuth: disableLocalAuth
    semanticSearch: semanticSearch
  }
  sku: sku
}

// Assign Search Index Data Contributor role to the principal if provided
resource searchIndexDataContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(search.id, principalId, '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
  scope: search
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
    principalType: 'User'
  }
}

// Assign Search Service Contributor role to the principal if provided
resource searchServiceContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(search.id, principalId, '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
  scope: search
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
    principalType: 'User'
  }
}

output id string = search.id
output name string = search.name
output endpoint string = 'https://${name}.search.windows.net'
output principalId string = search.identity.principalId
