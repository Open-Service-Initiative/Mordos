using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Mordos.API.Models.DTOs;
using Mordos.API.Services;
using System.Net;

namespace Mordos.API.Functions;

/// <summary>
/// Azure Functions for managing Bicep templates
/// </summary>
public class BicepTemplatesFunctions(IBicepTemplateService bicepTemplateService, ILogger<BicepTemplatesFunctions> logger)
{

    /// <summary>
    /// Get all Bicep templates
    /// </summary>
    [Function("GetBicepTemplates")]
    [OpenApiOperation(operationId: "GetBicepTemplates", tags: ["BicepTemplates"], Summary = "Get all Bicep templates", Description = "Retrieves a list of all Bicep templates with optional filtering", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "nameFilter", In = ParameterLocation.Query, Required = false, Type = typeof(string), Summary = "Name filter", Description = "Filter templates by name (case-insensitive partial match)", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "tagFilter", In = ParameterLocation.Query, Required = false, Type = typeof(string), Summary = "Tag filter", Description = "Filter templates by tag (case-insensitive partial match)", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<BicepTemplateResponse>), Summary = "Success", Description = "List of Bicep templates")]
    public async Task<IActionResult> GetBicepTemplates(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "templates")] HttpRequest req)
    {
        try
        {
            var nameFilter = req.Query["nameFilter"].FirstOrDefault();
            var tagFilter = req.Query["tagFilter"].FirstOrDefault();

            logger.LogInformation("Getting Bicep templates with filters - Name: {NameFilter}, Tag: {TagFilter}", nameFilter, tagFilter);

            var templates = await bicepTemplateService.GetAllTemplatesAsync(nameFilter, tagFilter);
            return new OkObjectResult(templates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving Bicep templates");
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Get a specific Bicep template by ID
    /// </summary>
    [Function("GetBicepTemplate")]
    [OpenApiOperation(operationId: "GetBicepTemplate", tags: ["BicepTemplates"], Summary = "Get Bicep template by ID", Description = "Retrieves a specific Bicep template by its unique identifier", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Template ID", Description = "The unique identifier of the template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BicepTemplateResponse), Summary = "Success", Description = "The requested Bicep template")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Summary = "Not Found", Description = "Template not found")]
    public async Task<IActionResult> GetBicepTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "templates/{id}")] HttpRequest req,
        string id)
    {
        try
        {
            logger.LogInformation("Getting Bicep template with ID: {Id}", id);

            var template = await bicepTemplateService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return new NotFoundObjectResult($"Template with ID '{id}' not found");
            }

            return new OkObjectResult(template);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving Bicep template with ID: {Id}", id);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Create a new Bicep template
    /// </summary>
    [Function("CreateBicepTemplate")]
    [OpenApiOperation(operationId: "CreateBicepTemplate", tags: ["BicepTemplates"], Summary = "Create new Bicep template", Description = "Creates a new Bicep template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateBicepTemplateRequest), Required = true, Description = "The template data to create")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(BicepTemplateResponse), Summary = "Created", Description = "The created Bicep template")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Summary = "Bad Request", Description = "Invalid request data")]
    public async Task<IActionResult> CreateBicepTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "templates")] HttpRequest req)
    {
        try
        {
            logger.LogInformation("Creating new Bicep template");

            var request = await req.ReadFromJsonAsync<CreateBicepTemplateRequest>();
            if (request == null)
            {
                return new BadRequestObjectResult("Invalid request body");
            }

            // Validate the request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new BadRequestObjectResult("Template name is required");
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return new BadRequestObjectResult("Template content is required");
            }

            var template = await bicepTemplateService.CreateTemplateAsync(request);
            return new ObjectResult(template) { StatusCode = 201 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Bicep template");
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Update an existing Bicep template
    /// </summary>
    [Function("UpdateBicepTemplate")]
    [OpenApiOperation(operationId: "UpdateBicepTemplate", tags: ["BicepTemplates"], Summary = "Update Bicep template", Description = "Updates an existing Bicep template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Template ID", Description = "The unique identifier of the template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateBicepTemplateRequest), Required = true, Description = "The template data to update")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BicepTemplateResponse), Summary = "Success", Description = "The updated Bicep template")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Summary = "Not Found", Description = "Template not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Summary = "Bad Request", Description = "Invalid request data")]
    public async Task<IActionResult> UpdateBicepTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "templates/{id}")] HttpRequest req,
        string id)
    {
        try
        {
            logger.LogInformation("Updating Bicep template with ID: {Id}", id);

            var request = await req.ReadFromJsonAsync<UpdateBicepTemplateRequest>();
            if (request == null)
            {
                return new BadRequestObjectResult("Invalid request body");
            }

            var template = await bicepTemplateService.UpdateTemplateAsync(id, request);
            if (template == null)
            {
                return new NotFoundObjectResult($"Template with ID '{id}' not found");
            }

            return new OkObjectResult(template);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Bicep template with ID: {Id}", id);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Delete a Bicep template
    /// </summary>
    [Function("DeleteBicepTemplate")]
    [OpenApiOperation(operationId: "DeleteBicepTemplate", tags: ["BicepTemplates"], Summary = "Delete Bicep template", Description = "Deletes a Bicep template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Template ID", Description = "The unique identifier of the template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "No Content", Description = "Template deleted successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Summary = "Not Found", Description = "Template not found")]
    public async Task<IActionResult> DeleteBicepTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "templates/{id}")] HttpRequest req,
        string id)
    {
        try
        {
            logger.LogInformation("Deleting Bicep template with ID: {Id}", id);

            // Check if template exists first
            var existingTemplate = await bicepTemplateService.GetTemplateByIdAsync(id);
            if (existingTemplate == null)
            {
                return new NotFoundObjectResult($"Template with ID '{id}' not found");
            }

            var success = await bicepTemplateService.DeleteTemplateAsync(id);
            if (success)
            {
                return new NoContentResult();
            }

            return new ObjectResult("Failed to delete template") { StatusCode = 500 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Bicep template with ID: {Id}", id);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Validate a Bicep template
    /// </summary>
    [Function("ValidateBicepTemplate")]
    [OpenApiOperation(operationId: "ValidateBicepTemplate", tags: ["BicepTemplates"], Summary = "Validate Bicep template", Description = "Validates a Bicep template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Template ID", Description = "The unique identifier of the template", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Success", Description = "Validation result")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Summary = "Not Found", Description = "Template not found")]
    public async Task<IActionResult> ValidateBicepTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "templates/{id}/validate")] HttpRequest req,
        string id)
    {
        try
        {
            logger.LogInformation("Validating Bicep template with ID: {Id}", id);

            // Check if template exists first
            var existingTemplate = await bicepTemplateService.GetTemplateByIdAsync(id);
            if (existingTemplate == null)
            {
                return new NotFoundObjectResult($"Template with ID '{id}' not found");
            }

            var success = await bicepTemplateService.ValidateTemplateAsync(id);
            return new OkObjectResult(new { Success = success, Message = success ? "Template validation passed" : "Template validation failed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating Bicep template with ID: {Id}", id);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }
}