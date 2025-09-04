using IDV_Templates_Mongo_API.Models;
using IDV_Templates_Mongo_API.DTOs;
using MongoDB.Driver;

namespace IDV_Templates_Mongo_API.Services;
public interface ITemplatesService
{
    Task<List<Template>> ListAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<long> CountAsync(string? search, CancellationToken ct);
    Task<Template?> GetByIdAsync(string id, CancellationToken ct);
    Task<Template?> GetByTemplateIdAsync(int templateId, CancellationToken ct);
    Task<Template> CreateAsync(Template input, CancellationToken ct);
    Task<bool> ReplaceAsync(string id, Template input, CancellationToken ct);
    Task<bool> PatchAsync(string id, Dictionary<string, object> patch, CancellationToken ct);
    Task<bool> DeleteAsync(string id, CancellationToken ct);

    // NEW: low-level update hook for section PUT endpoints
    Task<bool> UpdateRawAsync(string id, UpdateDefinition<Template> updates, CancellationToken ct);
}
