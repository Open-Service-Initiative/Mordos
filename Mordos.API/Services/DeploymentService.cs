using Azure.Data.Tables;
using Mordos.API.Models;
using Mordos.API.Models.DTOs;
using Microsoft.Extensions.Logging;

namespace Mordos.API.Services;

/// <summary>
/// Implementation of IDeploymentService using Azure Table Storage
/// </summary>
public class DeploymentService(
    TableServiceClient tableServiceClient,
    IBicepTemplateService bicepTemplateService,
    ILogger<DeploymentService> logger) : IDeploymentService
{
    private const string TableName = "Deployments";

    public async Task<IEnumerable<DeploymentResponse>> GetAllDeploymentsAsync(string? templateIdFilter = null, DeploymentStatus? statusFilter = null)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var deployments = new List<DeploymentResponse>();
            
            await foreach (var entity in tableClient.QueryAsync<Deployment>(filter: $"PartitionKey eq 'Deployment'"))
            {
                // Apply filters if provided
                if (!string.IsNullOrEmpty(templateIdFilter) && entity.TemplateId != templateIdFilter)
                    continue;

                if (statusFilter.HasValue && entity.Status != statusFilter.Value)
                    continue;

                deployments.Add(DeploymentResponse.FromEntity(entity));
            }

            logger.LogInformation("Retrieved {Count} deployments", deployments.Count);
            return deployments.OrderByDescending(d => d.CreatedAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deployments");
            throw;
        }
    }

    public async Task<DeploymentResponse?> GetDeploymentByIdAsync(string id)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var response = await tableClient.GetEntityIfExistsAsync<Deployment>("Deployment", id);
            
            if (!response.HasValue)
            {
                logger.LogInformation("Deployment with ID {Id} not found", id);
                return null;
            }

            logger.LogInformation("Retrieved deployment with ID {Id}", id);
            return DeploymentResponse.FromEntity(response.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deployment with ID {Id}", id);
            throw;
        }
    }

    public async Task<DeploymentResponse> CreateDeploymentAsync(CreateDeploymentRequest request)
    {
        try
        {
            // Validate that the template exists
            var template = await bicepTemplateService.GetTemplateByIdAsync(request.TemplateId) ?? throw new ArgumentException($"Template with ID {request.TemplateId} not found", nameof(request.TemplateId));
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var deployment = new Deployment
            {
                TemplateId = request.TemplateId,
                Name = request.Name,
                TargetSubscriptionId = request.TargetSubscriptionId,
                TargetResourceGroup = request.TargetResourceGroup,
                TargetRegion = request.TargetRegion,
                Parameters = request.Parameters,
                InitiatedBy = request.InitiatedBy,
                Status = DeploymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await tableClient.AddEntityAsync(deployment);
            
            logger.LogInformation("Created new deployment with ID {Id} for template {TemplateId}", deployment.Id, request.TemplateId);
            return DeploymentResponse.FromEntity(deployment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating deployment for template {TemplateId}", request.TemplateId);
            throw;
        }
    }

    public async Task<DeploymentResponse?> UpdateDeploymentAsync(string id, UpdateDeploymentRequest request)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var existingResponse = await tableClient.GetEntityIfExistsAsync<Deployment>("Deployment", id);
            
            if (!existingResponse.HasValue)
            {
                logger.LogInformation("Deployment with ID {Id} not found for update", id);
                return null;
            }

            var deployment = existingResponse.Value;
            var statusChanged = false;
            
            // Update only provided fields
            if (request.Status.HasValue && deployment.Status != request.Status.Value)
            {
                deployment.Status = request.Status.Value;
                statusChanged = true;

                // Update timestamps based on status changes
                if (request.Status.Value == DeploymentStatus.InProgress && !deployment.StartedAt.HasValue)
                {
                    deployment.StartedAt = DateTime.UtcNow;
                }
                else if ((request.Status.Value == DeploymentStatus.Succeeded || 
                         request.Status.Value == DeploymentStatus.Failed || 
                         request.Status.Value == DeploymentStatus.Cancelled) && 
                         !deployment.CompletedAt.HasValue)
                {
                    deployment.CompletedAt = DateTime.UtcNow;
                }
            }

            if (request.Outputs is not null)
                deployment.Outputs = request.Outputs;
            if (request.ErrorMessage is not null)
                deployment.ErrorMessage = request.ErrorMessage;
            if (request.Logs is not null)
                deployment.Logs = request.Logs;
            if (!string.IsNullOrEmpty(request.AzureDeploymentId))
                deployment.AzureDeploymentId = request.AzureDeploymentId;

            deployment.UpdatedAt = DateTime.UtcNow;

            await tableClient.UpdateEntityAsync(deployment, deployment.ETag);
            
            logger.LogInformation("Updated deployment with ID {Id}. Status changed: {StatusChanged}", id, statusChanged);
            return DeploymentResponse.FromEntity(deployment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating deployment with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteDeploymentAsync(string id)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var response = await tableClient.DeleteEntityAsync("Deployment", id);
            
            logger.LogInformation("Deleted deployment with ID {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting deployment with ID {Id}", id);
            return false;
        }
    }

    public async Task<bool> StartDeploymentAsync(string id)
    {
        try
        {
            var updateRequest = new UpdateDeploymentRequest
            {
                Status = DeploymentStatus.InProgress
            };

            var result = await UpdateDeploymentAsync(id, updateRequest);
            return result != null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting deployment with ID {Id}", id);
            return false;
        }
    }

    public async Task<IEnumerable<DeploymentResponse>> GetDeploymentsByTemplateIdAsync(string templateId)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();

            var deployments = new List<DeploymentResponse>();
            
            await foreach (var entity in tableClient.QueryAsync<Deployment>(filter: $"PartitionKey eq 'Deployment' and TemplateId eq '{templateId}'"))
            {
                deployments.Add(DeploymentResponse.FromEntity(entity));
            }

            logger.LogInformation("Retrieved {Count} deployments for template {TemplateId}", deployments.Count, templateId);
            return deployments.OrderByDescending(d => d.CreatedAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deployments for template {TemplateId}", templateId);
            throw;
        }
    }
}