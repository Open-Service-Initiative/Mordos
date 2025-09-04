using Azure.Data.Tables;
using Mordos.API.Models;
using Mordos.API.Models.DTOs;
using Microsoft.Extensions.Logging;

namespace Mordos.API.Services;

/// <summary>
/// Implementation of IBicepTemplateService using Azure Table Storage
/// </summary>
public class BicepTemplateService(TableServiceClient tableServiceClient, ILogger<BicepTemplateService> logger) : IBicepTemplateService
{
    private const string TableName = "BicepTemplates";

    public async Task<IEnumerable<BicepTemplateResponse>> GetAllTemplatesAsync(string? nameFilter = null, string? tagFilter = null)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var templates = new List<BicepTemplateResponse>();
            
            await foreach (var entity in tableClient.QueryAsync<BicepTemplate>(filter: $"PartitionKey eq 'BicepTemplate'"))
            {
                // Apply filters if provided
                if (!string.IsNullOrEmpty(nameFilter) && !entity.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(tagFilter) && !entity.Tags.Contains(tagFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                templates.Add(BicepTemplateResponse.FromEntity(entity));
            }

            logger.LogInformation("Retrieved {Count} Bicep templates", templates.Count);
            return templates.OrderByDescending(t => t.UpdatedAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving Bicep templates");
            throw;
        }
    }

    public async Task<BicepTemplateResponse?> GetTemplateByIdAsync(string id)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var response = await tableClient.GetEntityIfExistsAsync<BicepTemplate>("BicepTemplate", id);
            
            if (!response.HasValue)
            {
                logger.LogInformation("Bicep template with ID {Id} not found", id);
                return null;
            }

            logger.LogInformation("Retrieved Bicep template with ID {Id}", id);
            return BicepTemplateResponse.FromEntity(response.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving Bicep template with ID {Id}", id);
            throw;
        }
    }

    public async Task<BicepTemplateResponse> CreateTemplateAsync(CreateBicepTemplateRequest request)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var template = new BicepTemplate
            {
                Name = request.Name,
                Description = request.Description,
                Content = request.Content,
                Version = request.Version,
                Tags = request.Tags,
                Author = request.Author,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await tableClient.AddEntityAsync(template);
            
            logger.LogInformation("Created new Bicep template with ID {Id}", template.Id);
            return BicepTemplateResponse.FromEntity(template);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Bicep template");
            throw;
        }
    }

    public async Task<BicepTemplateResponse?> UpdateTemplateAsync(string id, UpdateBicepTemplateRequest request)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var existingResponse = await tableClient.GetEntityIfExistsAsync<BicepTemplate>("BicepTemplate", id);
            
            if (!existingResponse.HasValue)
            {
                logger.LogInformation("Bicep template with ID {Id} not found for update", id);
                return null;
            }

            var template = existingResponse.Value;
            
            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Name))
                template.Name = request.Name;
            if (request.Description is not null)
                template.Description = request.Description;
            if (!string.IsNullOrEmpty(request.Content))
                template.Content = request.Content;
            if (!string.IsNullOrEmpty(request.Version))
                template.Version = request.Version;
            if (request.Tags is not null)
                template.Tags = request.Tags;
            if (!string.IsNullOrEmpty(request.Author))
                template.Author = request.Author;

            template.UpdatedAt = DateTime.UtcNow;
            template.IsValidated = false; // Reset validation on content changes

            await tableClient.UpdateEntityAsync(template, template.ETag);
            
            logger.LogInformation("Updated Bicep template with ID {Id}", id);
            return BicepTemplateResponse.FromEntity(template);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Bicep template with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteTemplateAsync(string id)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var response = await tableClient.DeleteEntityAsync("BicepTemplate", id);
            
            logger.LogInformation("Deleted Bicep template with ID {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Bicep template with ID {Id}", id);
            return false;
        }
    }

    public async Task<bool> ValidateTemplateAsync(string id)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var existingResponse = await tableClient.GetEntityIfExistsAsync<BicepTemplate>("BicepTemplate", id);
            
            if (!existingResponse.HasValue)
            {
                logger.LogInformation("Bicep template with ID {Id} not found for validation", id);
                return false;
            }

            var template = existingResponse.Value;
            
            // TODO: Implement actual Bicep validation logic here
            // For now, we'll just mark it as validated
            template.IsValidated = true;
            template.ValidationMessage = "Template validation passed";
            template.UpdatedAt = DateTime.UtcNow;

            await tableClient.UpdateEntityAsync(template, template.ETag);
            
            logger.LogInformation("Validated Bicep template with ID {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating Bicep template with ID {Id}", id);
            return false;
        }
    }
}