using BlazorApp.Components;
using BlazorApp.Models;
using BlazorApp.Services;

using BlazorApp.Services.Strategies;
using BlazorApp.Services.Agentic;
using BlazorApp.Services.Agentic.Tools;
using BlazorApp.Services.Embeddings;
using BlazorApp.Services.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpClient factory
builder.Services.AddHttpClient();

// Configure Azure settings
builder.Services.Configure<AzureSettings>(
    builder.Configuration.GetSection("AzureSettings"));

// Add connection test service
builder.Services.AddScoped<ConnectionTestService>();

// Add Azure Search Index service
builder.Services.AddScoped<IAzureSearchIndexService, AzureSearchIndexService>();

// Add AI Model Service
builder.Services.AddScoped<IAiModelService, AiModelService>();

// Add Search Strategies
builder.Services.AddScoped<ISearchStrategy, SemanticSearchStrategy>();
builder.Services.AddScoped<SearchStrategyFactory>();

// Add Agentic Services
// Register tools as concrete types first so they can be injected individually
builder.Services.AddScoped<SearchTool>();
builder.Services.AddScoped<LookupTool>();
builder.Services.AddScoped<StatsTool>();
builder.Services.AddScoped<HybridSearchTool>();

// Register them as IAgenticTool for the standard AgenticSearchService
builder.Services.AddScoped<IAgenticTool>(sp => sp.GetRequiredService<SearchTool>());
builder.Services.AddScoped<IAgenticTool>(sp => sp.GetRequiredService<LookupTool>());
builder.Services.AddScoped<IAgenticTool>(sp => sp.GetRequiredService<StatsTool>());

builder.Services.AddScoped<AgenticSearchService>();
builder.Services.AddScoped<HybridAgenticSearchService>();

// Add Hybrid Data Services
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IHybridFileService, HybridFileService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
