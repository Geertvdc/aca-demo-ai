
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

#pragma warning disable SKEXP0070
builder.Services.AddOllamaChatCompletion(
    modelId: "phi3",
    endpoint: new Uri("http://localhost:11434")
);
builder.Services.AddTransient((serviceProvider)=> new Kernel(serviceProvider));

builder.Services.AddSingleton<CodeInterpreterClient>();

var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Minimal API for chat completion
app.MapPost("/v1/chat/completions", async ([FromServices] Kernel kernel, [FromBody] ChatCompletionRequest req) =>
{
    if (req.Messages is null || req.Messages.Count == 0)
    return Results.BadRequest("messages are required");

    var chat = kernel.GetRequiredService<IChatCompletionService>();

    var history = new ChatHistory();
    foreach (var m in req.Messages)
    {
        switch (m.Role.ToLowerInvariant())
        {
            case "user":       history.AddUserMessage(m.Content);       break;
            case "assistant":  history.AddAssistantMessage(m.Content);  break;
            case "system":     history.AddSystemMessage(m.Content);     break;
            default:           continue;
        }
    }

    var result = await chat.GetChatMessageContentAsync(history);
    return Results.Ok(new { content = result.Content });
});

app.MapPost("/exec", async (HttpRequest http, CodeInterpreterClient cic) =>
{
    using var reader = new StreamReader(http.Body);
    var userCode = await reader.ReadToEndAsync();

    var result = await cic.RunPythonAsync("chat123", userCode);
    return Results.Text(result, "text/plain");
});

app.MapDefaultEndpoints();

app.Run();

public record ChatMessage(string Role, string Content);
public record ChatCompletionRequest(
    string Model,
    List<ChatMessage> Messages,
    double Temperature = 0.7
);
