var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database with improved reliability settings
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume(isReadOnly: false) // Use a named volume for persistent data    
    .WithPgAdmin();

var weatherDb = postgres.AddDatabase("weatherdb");

// add migrations projct that runs first and exits
var migrations = builder.AddProject<Projects.CoffeeTracker_Migrations>("migrations")
    .WithReference(weatherDb)
    .WaitFor(postgres);

var apiService = builder.AddProject<Projects.CoffeeTracker_ApiService>("apiservice")
    .WithReference(weatherDb)
    .WaitFor(migrations);

builder.AddProject<Projects.CoffeeTracker_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(migrations);

builder.Build().Run();
