#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0110

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.AspNetCore.Mvc;
using Azure.Identity;
using Azure.AI.Projects;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Agents;
using Azure;


var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton(sp =>
{
    var cs     = builder.Configuration["AAIAS:ProjectConnectionString"];
    var client = AzureAIAgent.CreateAzureAIClient(cs, new DefaultAzureCredential());
    return client;                     // 1️⃣ register client
});

builder.Services.AddSingleton<AgentsClient>(sp =>
    sp.GetRequiredService<AIProjectClient>().GetAgentsClient());
    

AzureAIAgent BuildAgent(IServiceProvider sp, string agentName)
{
    var agentsClient = sp.GetRequiredService<AgentsClient>();
    var def          = agentsClient.GetAgentAsync(agentName).GetAwaiter().GetResult();
    return new AzureAIAgent(def, agentsClient);
}

builder.Services
    .AddSingleton(sp => BuildAgent(sp, "asst_BhWFyYBUavNqTFRU9EltoXJh")) //FlightAgent
    .AddSingleton(sp => BuildAgent(sp, "asst_KKh2pfaChOtIPzEhFLPvbpan")) //HotelAgent
    .AddSingleton(sp => BuildAgent(sp, "asst_kkTFbS3bIuTNDkOrSdQyFdPU")); //CostAgent

var kernel = builder.Services.AddKernel();

//kernel.Plugins.AddFromPromptDirectory("./Prompts");

kernel.AddAzureOpenAIChatCompletion(         
        "gpt-35-turbo",
        "https://dotnetsaturday4795076499.openai.azure.com/",
        "5cbfhZTKuPdWLgWjjIsXikJAFKFLt8bUsjX6kqFwMbQypJjGDNOEJQQJ99BEACfhMk5XJ3w3AAAAACOG3jhw");

builder.Services.AddSingleton<TravelPlanner>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/v1/chat/completions",
    async (ChatCompletionRequest req, TravelPlanner planner) =>
{
    var answer = await planner.ExecuteAsync(req);
    return Results.Ok(new { content = answer });
});

app.MapDefaultEndpoints();

app.Run();

public record ChatMessage(string Role, string Content);
public record ChatCompletionRequest(
    string Model,
    List<ChatMessage> Messages,
    double Temperature = 0.7
);
