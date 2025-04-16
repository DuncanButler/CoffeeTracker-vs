var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.CoffeeTracker_ApiService>("apiservice");

builder.AddProject<Projects.CoffeeTracker_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
