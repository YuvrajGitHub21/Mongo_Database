// Controller for managing Template resources
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
    // Service for template operations
    private readonly ITemplatesService _svc;
    // MongoDB context
    private readonly MongoContext _ctx;
    public TemplatesController(ITemplatesService svc, MongoContext ctx) { _svc = svc; _ctx = ctx; }

    // GET api/templates
    // Lists templates with optional search and pagination
    [HttpGet]
    public async Task<ActionResult<PageResult<Template>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 200) pageSize = 20;
        var items = await _svc.ListAsync(search, page, pageSize, ct);
        var total = await _svc.CountAsync(search, ct);
        return Ok(new PageResult<Template> { Page = page, PageSize = pageSize, Total = total, Items = items });
    }

    // GET api/templates/{id}
    // Gets a template by MongoDB ObjectId
    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Template>> GetById(string id, CancellationToken ct)
    {
        var doc = await _svc.GetByIdAsync(id, ct);
        return doc is null ? NotFound() : Ok(doc);
    }

    // GET api/templates/by-template-id/{templateId}
    // Gets a template by integer templateId
    [HttpGet("by-template-id/{templateId:int}")]
    public async Task<ActionResult<Template>> GetByTemplateId(int templateId, CancellationToken ct)
    {
        var doc = await _svc.GetByTemplateIdAsync(templateId, ct);
        return doc is null ? NotFound() : Ok(doc);
    }

    // POST api/templates
    // Creates a new template (full object)
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

    // DTO for minimal template creation
    public class CreateTemplateByNameDto
    {
        public string nameOfTemplate { get; set; } = string.Empty;
        public List<string>? sections_order { get; set; }
    }

    // POST api/templates/create-min
    // Creates a minimal template by name
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

        // Allow any number of sections in sections_order
        if (dto.sections_order != null)
            t.sections_order = dto.sections_order;

        var created = await _svc.CreateAsync(t, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT api/templates/{id}
    // Replaces a template by id
    [HttpPut("{id:length(24)}")]
    [Authorize]
    public async Task<IActionResult> Replace(string id, [FromBody] Template input, CancellationToken ct)
    {
        input.last_updated = DateTime.UtcNow;
        var ok = await _svc.ReplaceAsync(id, input, ct);
        return ok ? NoContent() : NotFound();
    }

    // DTO for saving section order
    public class SaveOrderDto { public List<string> sections_order { get; set; } = new(); public int? current_step { get; set; } }

    // PUT api/templates/{id}/personal
    // Updates personal info section
    [HttpPut("{id:length(24)}/personal")]
    [Authorize]
    public async Task<IActionResult> PutPersonal(string id, [FromBody] PersonalInfo personal, [FromQuery] int? currentStep, CancellationToken ct = default)
    {
        try
        {
            Console.WriteLine($"=== PUT /personal START ===");
            Console.WriteLine();
            Console.WriteLine($"ID: {id}");
            Console.WriteLine($"CurrentStep: {currentStep}");
            Console.WriteLine($"Personal data received: {System.Text.Json.JsonSerializer.Serialize(personal)}");
            
            // enforce mandatory fields remain true (server guard)
            personal.firstName = true;
            personal.LastName = true;
            personal.Email = true;

            // First, let's check if the document exists
            var existingDoc = await _svc.GetByIdAsync(id, ct);
            if (existingDoc == null)
            {
                Console.WriteLine($"ERROR: Document with ID {id} not found!");
                return NotFound();
            }
            Console.WriteLine($"Document found. Current Section_status.persoanl_info: {existingDoc.Section_status.persoanl_info}");
            
            // Use the correct property name 'persoanl_info' as defined in SectionStatus model
            var updates = Builders<Template>.Update
                .Set(x => x.Personal_info, personal)
                .Set("Section_status.persoanl_info", true);
            if (currentStep.HasValue) updates = updates.Set(x => x.current_step, currentStep.Value);
            
            Console.WriteLine($"About to execute MongoDB update...");
            var res = await _svc.UpdateRawAsync(id, updates, ct);
            Console.WriteLine($"MongoDB update result: {res}");
            
            // Verify the update by fetching the document again
            var updatedDoc = await _svc.GetByIdAsync(id, ct);
            if (updatedDoc != null)
            {
                Console.WriteLine($"After update - Section_status.persoanl_info: {updatedDoc.Section_status.persoanl_info}");
                Console.WriteLine($"After update - current_step: {updatedDoc.current_step}");
            }
            else
            {
                Console.WriteLine($"ERROR: Could not fetch updated document!");
            }
            
            Console.WriteLine($"=== PUT /personal END ===");
            return res ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in PutPersonal: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    // PUT api/templates/{id}/docs
    // Updates document verification section
    [HttpPut("{id:length(24)}/docs")]
    [Authorize]
    public async Task<IActionResult> PutDocs(string id, [FromBody] DocVerification docs, [FromQuery] int? currentStep, CancellationToken ct = default)
    {
        Console.WriteLine($"PUT /docs called for id={id}, currentStep={currentStep}");
        
        // Update Doc_verification and Section_status.doc_verification to true
        var updates = Builders<Template>.Update
            .Set(x => x.Doc_verification, docs)
            .Set("Section_status.doc_verification", true);
        if (currentStep.HasValue) updates = updates.Set(x => x.current_step, currentStep.Value);
        var res = await _svc.UpdateRawAsync(id, updates, ct);
        Console.WriteLine($"PUT /docs for id={id}: Section_status.doc_verification set to true, update result: {res}");
        return res ? NoContent() : NotFound();
    }

    // PUT api/templates/{id}/biometric
    // Updates biometric verification section
    [HttpPut("{id:length(24)}/biometric")]
    [Authorize]
    public async Task<IActionResult> PutBiometric(string id, [FromBody] BiometricVerification bio, [FromQuery] int? currentStep, CancellationToken ct = default)
    {
        Console.WriteLine($"PUT /biometric called for id={id}, currentStep={currentStep}");
        
        // Update Biometric_verification and Section_status.Biometric_verification to true
        var updates = Builders<Template>.Update
            .Set(x => x.Biometric_verification, bio)
            .Set("Section_status.Biometric_verification", true);
        if (currentStep.HasValue) updates = updates.Set(x => x.current_step, currentStep.Value);
        var res = await _svc.UpdateRawAsync(id, updates, ct);
        Console.WriteLine($"PUT /biometric for id={id}: Section_status.Biometric_verification set to true, update result: {res}");
        return res ? NoContent() : NotFound();
    }

    // PUT api/templates/{id}/order
    // Updates the order of sections
    [HttpPut("{id:length(24)}/order")]
    [Authorize]
    public async Task<IActionResult> PutOrder(string id, [FromBody] SaveOrderDto body, CancellationToken ct = default)
    {
        // Allow any number of sections in sections_order
        if (body.sections_order == null)
            return BadRequest("sections_order is required.");
        var updates = Builders<Template>.Update.Set(x => x.sections_order, body.sections_order);
        if (body.current_step.HasValue) updates = updates.Set(x => x.current_step, body.current_step.Value);
        var res = await _svc.UpdateRawAsync(id, updates, ct);
        return res ? NoContent() : NotFound();
    }

    // PATCH api/templates/{id}
    // Partially updates a template
    [HttpPatch("{id:length(24)}")]
    [Authorize]
    public async Task<IActionResult> Patch(string id, [FromBody] Dictionary<string, object> patch, CancellationToken ct)
    {
        if (patch is null || patch.Count == 0) return BadRequest("Empty patch.");
        var ok = await _svc.PatchAsync(id, patch, ct);
        return ok ? NoContent() : NotFound();
    }

    // DELETE api/templates/{id}
    // Deletes a template by id
    [HttpDelete("{id:length(24)}")]
    [Authorize]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    // Helper: Resolve the current user's display name
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
