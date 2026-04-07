using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Contracts.ApiKeys;
using PaymentGateway.Contracts.Common;
using PaymentGateway.Gateway.Api.Data;
using PaymentGateway.Gateway.Api.Services;

namespace PaymentGateway.Gateway.Api.Controllers;

[ApiController]
[Route("api/apikeys")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly ApiKeyService _apiKeyService;
    private readonly TenantContext _tenant;

    public ApiKeysController(ApiKeyService apiKeyService, TenantContext tenant)
    {
        _apiKeyService = apiKeyService;
        _tenant = tenant;
    }

    private Guid OrgId => _tenant.OrganizationId
        ?? throw new UnauthorizedAccessException("Organization not found in claims");

    [HttpGet]
    public async Task<IActionResult> GetApiKeys([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var keys = await _apiKeyService.GetApiKeys(OrgId);
        var paged = keys.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPages = (int)Math.Ceiling(keys.Count / (double)pageSize);
        return Ok(new PagedResult<ApiKeyDto>(paged, keys.Count, page, pageSize, totalPages));
    }

    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        var result = await _apiKeyService.CreateApiKey(OrgId, request);
        return Created($"/api/apikeys/{result.Id}", result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeApiKey(Guid id)
    {
        try
        {
            await _apiKeyService.RevokeApiKey(OrgId, id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/rotate")]
    public async Task<IActionResult> RotateApiKey(Guid id)
    {
        try
        {
            var result = await _apiKeyService.RotateApiKey(OrgId, id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
