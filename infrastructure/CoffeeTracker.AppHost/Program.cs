var builder = DistributedApplication.CreateBuilder(args);

// add postgresql database
var postgres = builder.AddPostgres("coffeedbserver")
    .WithDataVolume()
    .WithPgAdmin(resource =>
          {
              resource.WithUrlForEndpoint("http", u => u.DisplayText = "PG Admin");
          });
                       
var database = postgres.AddDatabase("coffeetrackerdb");

var apiService = builder.AddProject<Projects.CoffeeTracker_ApiService>("apiservice")
    .WithReference(database);

builder.AddProject<Projects.CoffeeTracker_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
