using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace Mordos.API.Functions;

/// <summary>
/// API status and health check functions
/// </summary>
public class StatusFunctions(ILogger<StatusFunctions> logger)
{
    /// <summary>
    /// Get API status and health
    /// </summary>
    [Function("GetStatus")]
    [OpenApiOperation(operationId: "GetStatus", tags: ["Status"], Summary = "Get API status", Description = "Returns the current status and health of the Mordos API", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Success", Description = "API status information")]
    public IActionResult GetStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status")] HttpRequest req)
    {
        logger.LogInformation("Status endpoint called");

        var status = new
        {
            Status = "Healthy",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development",
            Timestamp = DateTime.UtcNow,
            Message = "Mordos API - Azure Infrastructure Management for MSPs and CSPs",
            Endpoints = new
            {
                Templates = "/api/templates",
                Deployments = "/api/deployments",
                OpenApiDocument = "/api/openapi/v3.json",
                SwaggerUI = "/api/swagger/ui"
            }
        };

        return new OkObjectResult(status);
    }
}