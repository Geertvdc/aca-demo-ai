var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.aca_demo_app_ApiService>("apiservice")
    .WithHttpsHealthCheck("/health");

builder.Build().Run();
