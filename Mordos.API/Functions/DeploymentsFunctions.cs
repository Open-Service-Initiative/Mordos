using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Mordos.API.Models;
using Mordos.API.Models.DTOs;
using Mordos.API.Services;
using System.Net;

namespace Mordos.API.Functions;

/// <summary>
/// Azure Functions for managing deployments
/// </summary>
public class DeploymentsFunctions(IDeploymentService deploymentService, ILogger<DeploymentsFunctions> logger)
{

    /// <summary>
    /// Get all deployments
    /// </summary>
    [Function("GetDeployments")]
    [OpenApiOperation(operationId: "GetDeployments", tags: ["Deployments"], Summary = "Get all deployments", Description = "Retrieves a list of all deployments with optional filtering", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "templateId", In = ParameterLocation.Query, Required = false, Type = typeof(string), Summary = "Template ID filter", Description = "Filter deployments by template ID", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "status", In = ParameterLocation.Query, Required = false, Type = typeof(DeploymentStatus), Summary = "Status filter", Description = "Filter deployments by status", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<DeploymentResponse>), Summary = "Success", Description = "List of deployments")]
    public async Task<IActionResult> GetDeployments(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "deployments")] HttpRequest req)
    {
        try
        {
            var templateIdFilter = req.Query["templateId"].FirstOrDefault();
            var statusFilter = req.Query["status"].FirstOrDefault();

            DeploymentStatus? status = null;
            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<DeploymentStatus>(statusFilter, true, out var parsedStatus))
            {
                status = parsedStatus;
            }

            logger.LogInformation("Getting deployments with filters - TemplateId: {TemplateId}, Status: {Status}", templateIdFilter, status);

            var deployments = await deploymentService.GetAllDeploymentsAsync(templateIdFilter, status);
            return new OkObjectResult(deployments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deployments");
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Get a specific deployment by ID
    /// </summary>
    [Function("GetDeployment")]
    [OpenApiOperation(operationId: "GetDeployment", tags: ["Deployments"], Summary = "Get deployment by ID", Description = "Retrieves a specific deployment by its unique identifier", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Deployment ID", Description = "The unique identifier of the deployment", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DeploymentResponse), Summary = "Success", Description = "The requested deployment")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Summary = "Not Found", Description = "Deployment not found")]
    public async Task<IActionResult> GetDeployment(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "deployments/{id}")] HttpRequest req,
        string id)
    {
        try
        {
            logger.LogInformation("Getting deployment with ID: {Id}", id);

            var deployment = await deploymentService.GetDeploymentByIdAsync(id);
            if (deployment == null)
            {
                return new NotFoundObjectResult($"Deployment with ID '{id}' not found");
            }

            return new OkObjectResult(deployment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deployment with ID: {Id}", id);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Create a new deployment
    /// </summary>
    [Function("CreateDeployment")]
    [OpenApiOperation(operationId: "CreateDeployment", tags: ["Deployments"], Summary = "Create new deployment", Description = "Creates a new deployment", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateDeploymentRequest), Required = true, Description = "The deployment data to create")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(DeploymentResponse), Summary = "Created", Description = "The created deployment")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Summary = "Bad Request", Description = "Invalid request data")]
    public async Task<IActionResult> CreateDeployment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "deployments")] HttpRequest req)
    {
        try
        {
            logger.LogInformation("Creating new deployment");

            var request = await req.ReadFromJsonAsync<CreateDeploymentRequest>();
            if (request == null)
            {
                return new BadRequestObjectResult("Invalid request body");
            }

            // Validate the request
            if (string.IsNullOrWhiteSpace(request.TemplateId))
            {
                return new BadRequestObjectResult("Template ID is required");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new BadRequestObjectResult("Deployment name is required");
            }

            if (string.IsNullOrWhiteSpace(request.TargetSubscriptionId))
            {
                return new BadRequestObjectResult("Target subscription ID is required");
            }

            if (string.IsNullOrWhiteSpace(request.TargetResourceGroup))
            {
                return new BadRequestObjectResult("Target resource group is required");
            }

            if (string.IsNullOrWhiteSpace(request.TargetRegion))
            {
                return new BadRequestObjectResult("Target region is required");
            }

            var deployment = await deploymentService.CreateDeploymentAsync(request);
            return new ObjectResult(deployment) { StatusCode = 201 };
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument when creating deployment");
            return new BadRequestObjectResult(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating deployment");
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Update an existing deployment
    /// </summary>
    [Function("UpdateDeployment")]
    [OpenApiOperation(operationId: "UpdateDeployment", tags: ["Deployments"], Summary = "Update deployment", Description = "Updates an existing deployment", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Deployment ID", Description = "The unique identifier of the deployment", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateDeploymentRequest), Required = true, Description = "The deployment data to update")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DeploymentResponse), Summary = "Success", Description = "The updated deployment")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Summary = "Not Found", Description = "Deployment not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Summary = "Bad Request", Description = "Invalid request data")]
    public async Task<IActionResult> UpdateDeployment(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "deployments/{id}")] HttpRequest req,
        string id)
    {
        try
        {
            logger.LogInformation("Updating deployment with ID: {Id}", id);

            var request = await req.ReadFromJsonAsync<UpdateDeploymentRequest>();
            if (request == null)
            {
                return new BadRequestObjectResult("Invalid request body");
            }

            var deployment = await deploymentService.UpdateDeploymentAsync(id, request);
            if (deployment == null)
            {
                return new NotFoundObjectResult($"Deployment with ID '{id}' not found");
            }

            return new OkObjectResult(deployment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating deployment with ID: {Id}", id);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Delete a deployment
    /// </summary>
    [Function("DeleteDeployment")]
    [OpenApiOperation(operationId: "DeleteDeployment", tags: ["Deployments"], Summary = "Delete deployment", Description = "Deletes a deployment", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Deployment ID", Description = "The unique identifier of the deployment", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "No Content", Description = "Deployment deleted successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Summary = "Not Found", Description = "Deployment not found")]
    public async Task<IActionResult> DeleteDeployment(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "deployments/{id}")] HttpRequest req,
        string id)
    {
        try
        {
            logger.LogInformation("Deleting deployment with ID: {Id}", id);

            // Check if deployment exists first
            var existingDeployment = await deploymentService.GetDeploymentByIdAsync(id);
            if (existingDeployment == null)
            {
                return new NotFoundObjectResult($"Deployment with ID '{id}' not found");
            }

            var success = await deploymentService.DeleteDeploymentAsync(id);
            if (success)
            {
                return new NoContentResult();
            }

            return new ObjectResult("Failed to delete deployment") { StatusCode = 500 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting deployment with ID: {Id}", id);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Start a deployment
    /// </summary>
    [Function("StartDeployment")]
    [OpenApiOperation(operationId: "StartDeployment", tags: ["Deployments"], Summary = "Start deployment", Description = "Starts a deployment (changes status to InProgress)", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Deployment ID", Description = "The unique identifier of the deployment", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Success", Description = "Deployment started successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Summary = "Not Found", Description = "Deployment not found")]
    public async Task<IActionResult> StartDeployment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "deployments/{id}/start")] HttpRequest req,
        string id)
    {
        try
        {
            logger.LogInformation("Starting deployment with ID: {Id}", id);

            // Check if deployment exists first
            var existingDeployment = await deploymentService.GetDeploymentByIdAsync(id);
            if (existingDeployment == null)
            {
                return new NotFoundObjectResult($"Deployment with ID '{id}' not found");
            }

            var success = await deploymentService.StartDeploymentAsync(id);
            if (success)
            {
                return new OkObjectResult(new { Success = true, Message = "Deployment started successfully" });
            }

            return new ObjectResult("Failed to start deployment") { StatusCode = 500 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting deployment with ID: {Id}", id);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Get deployments by template ID
    /// </summary>
    [Function("GetDeploymentsByTemplate")]
    [OpenApiOperation(operationId: "GetDeploymentsByTemplate", tags: ["Deployments"], Summary = "Get deployments by template", Description = "Retrieves all deployments for a specific template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "templateId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Template ID", Description = "The unique identifier of the template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<DeploymentResponse>), Summary = "Success", Description = "List of deployments for the template")]
    public async Task<IActionResult> GetDeploymentsByTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "templates/{templateId}/deployments")] HttpRequest req,
        string templateId)
    {
        try
        {
            logger.LogInformation("Getting deployments for template ID: {TemplateId}", templateId);

            var deployments = await deploymentService.GetDeploymentsByTemplateIdAsync(templateId);
            return new OkObjectResult(deployments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deployments for template ID: {TemplateId}", templateId);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }
}