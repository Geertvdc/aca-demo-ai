var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposePublisher();

var apiService = builder.AddProject<Projects.aca_demo_app_ApiService>("apiservice")
    .WithHttpsHealthCheck("/health");

// Stateful-less chat UI
var chatUi = builder.AddContainer("chatui", "yidadaa/chatgpt-next-web:latest")
       .WithEnvironment("OPENAI_API_KEY", "demo-key")
       .WithEnvironment("BASE_URL", "http://host.docker.internal:5597")                
       .WithHttpEndpoint(3000,3000);

builder.Build().Run();
