using IDV_Templates_Mongo_API.Data;
using IDV_Templates_Mongo_API.DTOs;
using IDV_Templates_Mongo_API.Models;
using IDV_Templates_Mongo_API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace IDV_Templates_Mongo_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MongoContext _ctx;
    private readonly IAuthService _auth;

    public AuthController(MongoContext ctx, IAuthService auth) { _ctx = ctx; _auth = auth; }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.email) || string.IsNullOrWhiteSpace(req.password))
            return BadRequest(new { message = "Email and password required." });
        if (req.confirmPassword != null && req.password != req.confirmPassword)
            return BadRequest(new { message = "Passwords do not match." });

        var existing = await _ctx.Users.Find(u => u.Email == req.email).FirstOrDefaultAsync(ct);
        if (existing != null) return Conflict(new { message = "Email already registered." });

        var (hash, salt) = _auth.HashPassword(req.password);
        var u = new User {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            FirstName = req.firstName,
            LastName = req.lastName,
            Email = req.email,
            PasswordHash = hash,
            PasswordSalt = salt,
            DocumentVerification = req.documentVerification == null ? null :
                System.Text.Json.JsonSerializer.Deserialize<DocumentVerificationPref>(
                    System.Text.Json.JsonSerializer.Serialize(req.documentVerification))
        };

        await _ctx.Users.InsertOneAsync(u, cancellationToken: ct);
        return Ok(new { message = "Registered" });
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await _ctx.Users.Find(u => u.Email == req.email).FirstOrDefaultAsync(ct);
        if (user == null) return Unauthorized(new { message = "Invalid credentials" });
        if (!_auth.VerifyPassword(req.password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(new { message = "Invalid credentials" });

        var token = _auth.GenerateJwt(user);
        return Ok(new ApiResponse<object> {
            data = new {
                accessToken = token,
                user = new { firstName = user.FirstName, lastName = user.LastName, email = user.Email }
            }
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly MongoContext _ctx;
    public UserController(MongoContext ctx) { _ctx = ctx; }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await _ctx.Users
            .Find(Builders<User>.Filter.Empty)
            .Project(u => new { id = u.Id, firstName = u.FirstName, lastName = u.LastName, email = u.Email })
            .ToListAsync(ct);
        return Ok(new { data = items });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(sub)) return Unauthorized();
        var u = await _ctx.Users.Find(x => x.Id == sub).FirstOrDefaultAsync(ct);
        if (u == null) return NotFound();
        return Ok(new { data = new { firstName = u.FirstName, lastName = u.LastName, email = u.Email } });
    }
}
