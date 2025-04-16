# Coffee Tracker

## Setup MCP Server

## Create project 

I created the aspire default web app project in visual studio and moved the projects to match my requirements

- infrastructure
  - AppHost
  - ServiceDefaults
- src
  - ApiService
  - web

  this was run inside vscode to ensure it runs correctly

## add swagger config to api so we can see the endpoints

Create issue and start work, this creates a branch for us to work on, and use as a PR into the main when we are ready and finished

then asked Copilot to do the actual coding with 
```add swagger to the API service```

had to add the dependencies running on the command line

```dotnet add package Swashbuckle.AspNetCore```

run and retested the app, check in and do the PR