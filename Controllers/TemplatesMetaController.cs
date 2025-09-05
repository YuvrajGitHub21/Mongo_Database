using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using IDV_Templates_Mongo_API.Services;
using IDV_Templates_Mongo_API.Models; // <-- Template model

namespace IDV_Templates_Mongo_API.Controllers
{
    /// <summary>
    /// Endpoints for small metadata-only updates (status, name) on templates.
    /// </summary>
    [ApiController]
    [Route("api/templates")]
    public class TemplatesMetaController : ControllerBase
    {
        private readonly ITemplatesService _templates;

        public TemplatesMetaController(ITemplatesService templates)
        {
            _templates = templates;
        }

        public class UpdateStatusDto
        {
            public bool? Template_status { get; set; }
        }

        public class UpdateNameDto
        {
            public string? template_name { get; set; }
            public string? nameOfTemplate { get; set; }
        }

        /// <summary>
        /// PUT /api/templates/{id}/status
        /// Body: { "Template_status": true|false }
        /// Updates ONLY the Template_status field.
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            string id,
            [FromBody] UpdateStatusDto body,
            CancellationToken ct)
        {
            if (body == null || body.Template_status == null)
                return BadRequest("Body must be { \"Template_status\": true | false }.");

            // Use UpdateDefinition<Template> (service requires Template generic)
            var update = Builders<Template>.Update.Set("Template_status", body.Template_status.Value);

            var ok = await _templates.UpdateRawAsync(id, update, ct);
            if (!ok) return NotFound();

            return Ok(new { Template_status = body.Template_status.Value });
        }

        /// <summary>
        /// PUT /api/templates/{id}/template_name
        /// Body: { "template_name": "New Name" } or { "nameOfTemplate": "New Name" }
        /// Updates ONLY the nameOfTemplate field.
        /// </summary>
        [HttpPut("{id}/template_name")]
        public async Task<IActionResult> UpdateTemplateName(
            string id,
            [FromBody] UpdateNameDto body,
            CancellationToken ct)
        {
            var name = body?.nameOfTemplate ?? body?.template_name;
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Body must contain a non-empty 'template_name' (or 'nameOfTemplate') string.");

            var update = Builders<Template>.Update.Set("nameOfTemplate", name);

            var ok = await _templates.UpdateRawAsync(id, update, ct);
            if (!ok) return NotFound();

            return Ok(new { nameOfTemplate = name });
        }
    }
}
