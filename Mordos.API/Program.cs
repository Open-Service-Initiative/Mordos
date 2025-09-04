using Azure.Core.Serialization;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mordos.API.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register business services
builder.Services.AddHttpClient()
                .AddScoped<IBicepTemplateService, BicepTemplateService>()
                .AddScoped<IDeploymentService, DeploymentService>();

builder.Build().Run();