using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IDV_Templates_Mongo_API.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public DocumentVerificationPref? DocumentVerification { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DocumentVerificationPref
{
    public bool allowUploadFromDevice { get; set; }
    public bool allowCaptureWebcam { get; set; }
    public string? documentHandling { get; set; }
    public List<string> selectedCountries { get; set; } = new();
    public List<string> selectedDocuments { get; set; } = new();
}
