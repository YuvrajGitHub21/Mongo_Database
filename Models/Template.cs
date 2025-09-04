using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace IDV_Templates_Mongo_API.Models;

public partial class Template
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("template_id")]
    public int template_id { get; set; }

    [BsonElement("nameOfTemplate")]
    public string nameOfTemplate { get; set; } = string.Empty;

    [BsonElement("Personal_info")]
    public PersonalInfo Personal_info { get; set; } = new();

    [BsonElement("Doc_verification")]
    public DocVerification Doc_verification { get; set; } = new();

    [BsonElement("Biometric_verification")]
    public BiometricVerification Biometric_verification { get; set; } = new();

    [BsonElement("Section_status")]
    public SectionStatus Section_status { get; set; } = new();

    [BsonElement("Template_status")]
    public bool Template_status { get; set; }

    // NEW: keep the order of the three sections
    [BsonElement("sections_order")]
    public List<string> sections_order { get; set; } = new()
    {
        "Personal_info", "Doc_verification", "Biometric_verification"
    };

    // NEW: track current step (1..3 for admin flow)
    [BsonElement("current_step")]
    public int current_step { get; set; } = 1;
}

public class PersonalInfo
{
    [BsonElement("section_id")] public int section_id { get; set; }
    public bool firstName { get; set; }
    public bool LastName { get; set; }
    public bool Email { get; set; }
    [BsonElement("Added_fields")] public AddedFields Added_fields { get; set; } = new();
}

public class AddedFields
{
    public bool dob { get; set; }
    public bool Current_address { get; set; }
    public bool permanent_address { get; set; }
    public bool Gender { get; set; }
}

public class DocVerification
{
    [BsonElement("section_id")] public int section_id { get; set; }
    [BsonElement("user_uploads")] public UserUploads user_uploads { get; set; } = new();
    [BsonElement("Unreadable_docs")] public UnreadableDocs Unreadable_docs { get; set; } = new();
    [BsonElement("Countries_array")] public List<CountryDocs> Countries_array { get; set; } = new();
}

public class UserUploads
{
    [BsonElement("Allow_uploads")] public bool Allow_uploads { get; set; }
    [BsonElement("allow_capture")] public bool allow_capture { get; set; }
}

public class UnreadableDocs
{
    [BsonElement("reject_immediately")] public bool reject_immediately { get; set; }
    [BsonElement("Allow_retries")] public bool Allow_retries { get; set; }
}

public class CountryDocs
{
    [BsonElement("country_name")] public string country_name { get; set; } = string.Empty;
    [BsonElement("listOfdocs")] public Dictionary<string, bool> listOfdocs { get; set; } = new();
}

public class BiometricVerification
{
    [BsonElement("section_id")] public int section_id { get; set; }
    [BsonElement("number_of_retries")] public List<int> number_of_retries { get; set; } = new();
    public Liveness liveness { get; set; } = new();
    [BsonElement("biometric_data_retention")] public BiometricDataRetention biometric_data_retention { get; set; } = new();
}

public class Liveness
{
    [BsonElement("try_again")] public bool try_again { get; set; }
    [BsonElement("Block_further")] public bool Block_further { get; set; }
}

public class BiometricDataRetention
{
    public List<string> duration { get; set; } = new();
}

public class SectionStatus
{
    [BsonElement("persoanl_info")] public bool persoanl_info { get; set; }
    [BsonElement("doc_verification")] public bool doc_verification { get; set; }
    [BsonElement("Biometric_verification")] public bool Biometric_verification { get; set; }

    // ===== Added fields for dashboard =====
    [BsonElement("invitees")]
    public List<Invitee> invitees { get; set; } = new();

    [BsonElement("created_template_date")]
    public DateTime created_template_date { get; set; }

    [BsonElement("created_by")]
    public string created_by { get; set; } = string.Empty;

    [BsonElement("last_updated")]
    public DateTime last_updated { get; set; }

}


public class Invitee
{
    [BsonElement("id")]
    public string id { get; set; } = string.Empty;

    [BsonElement("user_name")]
    public string user_name { get; set; } = string.Empty;
}
