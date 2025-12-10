# AVEVA AI Search Optimize

A Blazor application for testing and optimizing Azure AI Search with Azure OpenAI models, featuring Agentic Workflows and Hybrid Search.

## Features

### 1. Agentic Chat
- **Intelligent Orchestration**: Uses GPT-4 to dynamically select tools based on user intent.
- **Tools**:
  - `search_index`: Semantic search for concepts.
  - `lookup_asset`: Direct retrieval by ID.
  - `get_stats`: Aggregations and facet analysis.
- **Transparency**: UI displays exact tool calls and executed queries.

### 2. Hybrid Search (Vector + Keyword)
- **Architecture**: Combines BM25 (Keyword) and HNSW (Vector) algorithms.
- **Embeddings**: Uses `text-embedding-3-large` (1536 dimensions).
- **Implementation**: `HybridSearchTool` automatically vectorizes user queries for conceptual matching.

### 3. Data Preparation
- **Built-in Tool**: `PrepareHybridData` page to generate embeddings for JSON datasets.
- **Azure Integration**: Direct connection to Azure OpenAI for batch embedding generation.

## Architecture

- **Frontend**: Blazor Web App (.NET 9)
- **AI Orchestration**: Azure OpenAI (GPT-4.1)
- **Search Engine**: Azure AI Search (Standard S1)
- **Security**: Managed Identity / RBAC (No API Keys)

## Getting Started

1.  **Deploy Infrastructure**:
    ```bash
    azd up
    ```

2.  **Configure Local Settings**:
    Update `src/BlazorApp/appsettings.Development.json` with your Azure endpoints.

3.  **Run Application**:
    ```bash
    dotnet watch run --project src/BlazorApp/BlazorApp.csproj
    ```

4.  **Setup Hybrid Search**:
    - Create Index: Copy JSON from `indexes/hybrid/` to the "AI Search Index" page.
    - Generate Embeddings: Use the "Prepare Hybrid Data" page.
    - Upload Data: Use the "Upload Documents" page.
