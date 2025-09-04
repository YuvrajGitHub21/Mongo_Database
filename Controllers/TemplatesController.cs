using System.Linq;
using System;
using System.Security.Claims;
using IDV_Templates_Mongo_API.Data;
using IDV_Templates_Mongo_API.DTOs;
using IDV_Templates_Mongo_API.Models;
using IDV_Templates_Mongo_API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;

namespace IDV_Templates_Mongo_API.Controllers;

[ApiController]
[Route("api/templates")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplatesService _svc;
    private readonly MongoContext _ctx;
    public TemplatesController(ITemplatesService svc, MongoContext ctx) { _svc = svc; _ctx = ctx; }

    [HttpGet]
    public async Task<ActionResult<PageResult<Template>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 200) pageSize = 20;
        var items = await _svc.ListAsync(search, page, pageSize, ct);
        var total = await _svc.CountAsync(search, ct);
        return Ok(new PageResult<Template> { Page = page, PageSize = pageSize, Total = total, Items = items });
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Template>> GetById(string id, CancellationToken ct)
    {
        var doc = await _svc.GetByIdAsync(id, ct);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpGet("by-template-id/{templateId:int}")]
    public async Task<ActionResult<Template>> GetByTemplateId(int templateId, CancellationToken ct)
    {
        var doc = await _svc.GetByTemplateIdAsync(templateId, ct);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Template>> Create([FromBody] Template input, CancellationToken ct)
    {
        // Stamp meta
        try { input.created_by = await ResolveCurrentUserNameAsync(ct); } catch {}
        if (input.invitees == null) input.invitees = new List<Invitee>();
        if (input.created_template_date == default) input.created_template_date = DateTime.UtcNow;
        input.last_updated = DateTime.UtcNow;

        var created = await _svc.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ===== NEW: create-by-name with defaults =====
    public class CreateTemplateByNameDto
    {
        public string nameOfTemplate { get; set; } = string.Empty;
        public List<string>? sections_order { get; set; }
    }

    [HttpPost("create-min")]
    [Authorize]
    public async Task<ActionResult<Template>> CreateMin([FromBody] CreateTemplateByNameDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.nameOfTemplate))
            return BadRequest("nameOfTemplate is required.");

        var t = DefaultTemplateFactory.CreateBlank(dto.nameOfTemplate);

        // Stamp meta
        try { t.created_by = await ResolveCurrentUserNameAsync(ct); } catch { t.created_by = "Unknown"; }
        t.created_template_date = DateTime.UtcNow;
        t.last_updated = DateTime.UtcNow;
        t.invitees = t.invitees ?? new List<Invitee>();

        if (dto.sections_order != null && dto.sections_order.Count == 3)
            t.sections_order = dto.sections_order;

        var created = await _svc.CreateAsync(t, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:length(24)}")]
    [Authorize]
    public async Task<IActionResult> Replace(string id, [FromBody] Template input, CancellationToken ct)
    {
        input.last_updated = DateTime.UtcNow;
        var ok = await _svc.ReplaceAsync(id, input, ct);
        return ok ? NoContent() : NotFound();
    }

    // ===== NEW: discrete saves per section + order =====
    public class SaveOrderDto { public List<string> sections_order { get; set; } = new(); public int? current_step { get; set; } }

    [HttpPut("{id:length(24)}/personal")]
    [Authorize]
    public async Task<IActionResult> PutPersonal(string id, [FromBody] PersonalInfo personal, [FromQuery] int? currentStep, CancellationToken ct = default)
    {
        // enforce mandatory fields remain true (server guard)
        personal.firstName = true;
        personal.LastName = true;
        personal.Email = true;

        var updates = Builders<Template>.Update.Set(x => x.Personal_info, personal);
        if (currentStep.HasValue) updates = updates.Set(x => x.current_step, currentStep.Value);
        var res = await _svc.UpdateRawAsync(id, updates, ct);
        return res ? NoContent() : NotFound();
    }

    [HttpPut("{id:length(24)}/docs")]
    [Authorize]
    public async Task<IActionResult> PutDocs(string id, [FromBody] DocVerification docs, [FromQuery] int? currentStep, CancellationToken ct = default)
    {
        var updates = Builders<Template>.Update.Set(x => x.Doc_verification, docs);
        if (currentStep.HasValue) updates = updates.Set(x => x.current_step, currentStep.Value);
        var res = await _svc.UpdateRawAsync(id, updates, ct);
        return res ? NoContent() : NotFound();
    }

    [HttpPut("{id:length(24)}/biometric")]
    [Authorize]
    public async Task<IActionResult> PutBiometric(string id, [FromBody] BiometricVerification bio, [FromQuery] int? currentStep, CancellationToken ct = default)
    {
        var updates = Builders<Template>.Update.Set(x => x.Biometric_verification, bio);
        if (currentStep.HasValue) updates = updates.Set(x => x.current_step, currentStep.Value);
        var res = await _svc.UpdateRawAsync(id, updates, ct);
        return res ? NoContent() : NotFound();
    }

    [HttpPut("{id:length(24)}/order")]
    [Authorize]
    public async Task<IActionResult> PutOrder(string id, [FromBody] SaveOrderDto body, CancellationToken ct = default)
    {
        if (body.sections_order == null || body.sections_order.Count != 3)
            return BadRequest("sections_order must have exactly 3 items.");
        var updates = Builders<Template>.Update.Set(x => x.sections_order, body.sections_order);
        if (body.current_step.HasValue) updates = updates.Set(x => x.current_step, body.current_step.Value);
        var res = await _svc.UpdateRawAsync(id, updates, ct);
        return res ? NoContent() : NotFound();
    }

    [HttpPatch("{id:length(24)}")]
    [Authorize]
    public async Task<IActionResult> Patch(string id, [FromBody] Dictionary<string, object> patch, CancellationToken ct)
    {
        if (patch is null || patch.Count == 0) return BadRequest("Empty patch.");
        var ok = await _svc.PatchAsync(id, patch, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:length(24)}")]
    [Authorize]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    private async Task<string> ResolveCurrentUserNameAsync(CancellationToken ct)
    {
        var name = User.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(name)) return name!;

        var first = User.FindFirstValue(ClaimTypes.GivenName);
        var last = User.FindFirstValue(ClaimTypes.Surname);
        if (!string.IsNullOrWhiteSpace(first) || !string.IsNullOrWhiteSpace(last))
        {
            return string.Join(" ", new[]{first, last}.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        var email = User.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrWhiteSpace(email)) return email!;

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(sub))
        {
            var u = await _ctx.Users.Find(x => x.Id == sub).FirstOrDefaultAsync(ct);
            if (u != null) return string.Join(" ", new[]{u.FirstName, u.LastName}.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        return "Unknown";
    }
}
