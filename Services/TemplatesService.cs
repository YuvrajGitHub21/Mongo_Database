using System;
using IDV_Templates_Mongo_API.Data;
using IDV_Templates_Mongo_API.Models;
using IDV_Templates_Mongo_API.DTOs;
using MongoDB.Driver;
using MongoDB.Bson;

namespace IDV_Templates_Mongo_API.Services
{
    public class TemplatesService : ITemplatesService
    {
        private readonly MongoContext _ctx;
        public TemplatesService(MongoContext ctx) => _ctx = ctx;

        public async Task<List<Template>> ListAsync(string? search, int page, int pageSize, CancellationToken ct)
        {
            var fb = Builders<Template>.Filter;
            var filter = fb.Empty;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var rx = new BsonRegularExpression(search.Trim(), "i");
                filter &= fb.Or(
                    fb.Regex(x => x.nameOfTemplate, rx),
                    fb.Regex("Doc_verification.Countries_array.country_name", rx)
                );
            }

            return await _ctx.Templates
                .Find(filter)
                .SortByDescending(t => t.template_id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(ct);
        }

        public async Task<long> CountAsync(string? search, CancellationToken ct)
        {
            var fb = Builders<Template>.Filter;
            var filter = fb.Empty;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var rx = new BsonRegularExpression(search.Trim(), "i");
                filter &= fb.Or(
                    fb.Regex(x => x.nameOfTemplate, rx),
                    fb.Regex("Doc_verification.Countries_array.country_name", rx)
                );
            }
            return await _ctx.Templates.CountDocumentsAsync(filter, cancellationToken: ct);
        }

        public async Task<Template?> GetByIdAsync(string id, CancellationToken ct)
        {
            return await _ctx.Templates.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        }

        public async Task<Template?> GetByTemplateIdAsync(int templateId, CancellationToken ct)
        {
            return await _ctx.Templates.Find(x => x.template_id == templateId).FirstOrDefaultAsync(ct);
        }

        public async Task<Template> CreateAsync(Template input, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(input.Id))
                input.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

            if (input.template_id <= 0)
            {
                input.template_id = await _ctx.GetNextTemplateIdAsync(ct);
            }

            // Defaults for new fields
            if (input.invitees == null) input.invitees = new System.Collections.Generic.List<Invitee>();
            if (input.created_template_date == default) input.created_template_date = DateTime.UtcNow;
            if (input.last_updated == default) input.last_updated = DateTime.UtcNow;

            await _ctx.Templates.InsertOneAsync(input, cancellationToken: ct);
            return input;
        }

        public async Task<bool> ReplaceAsync(string id, Template input, CancellationToken ct)
        {
            input.Id = id;
            input.last_updated = DateTime.UtcNow;
            var res = await _ctx.Templates.ReplaceOneAsync(x => x.Id == id, input, cancellationToken: ct);
            return res.ModifiedCount == 1;
        }

        public async Task<bool> PatchAsync(string id, Dictionary<string, object> patch, CancellationToken ct)
        {
            if (patch is null || patch.Count == 0) return false;

            var flattened = PatchFlattener.Flatten(patch);
            var updates = new List<UpdateDefinition<Template>>();
            foreach (var kv in flattened)
            {
                updates.Add(Builders<Template>.Update.Set(kv.Key, BsonValue.Create(kv.Value)));
            }

            var update = Builders<Template>.Update
                .Combine(updates)
                .Set(x => x.last_updated, DateTime.UtcNow);

            var res = await _ctx.Templates.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
            return res.ModifiedCount == 1;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken ct)
        {
            var res = await _ctx.Templates.DeleteOneAsync(x => x.Id == id, ct);
            return res.DeletedCount == 1;
        }

        // Used by discrete section PUT endpoints
        public async Task<bool> UpdateRawAsync(string id, UpdateDefinition<Template> updates, CancellationToken ct)
        {
            var updates2 = updates.Set(x => x.last_updated, DateTime.UtcNow);
            var res = await _ctx.Templates.UpdateOneAsync(x => x.Id == id, updates2, cancellationToken: ct);
            return res.ModifiedCount == 1;
        }
    }
}
