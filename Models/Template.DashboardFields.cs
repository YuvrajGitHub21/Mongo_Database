using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace IDV_Templates_Mongo_API.Models
{
    // Partial adds dashboard fields to Template
    public partial class Template
    {
        [BsonElement("invitees")]
        public List<Invitee> invitees { get; set; } = new();

        [BsonElement("created_template_date")]
        public DateTime created_template_date { get; set; }

        [BsonElement("created_by")]
        public string created_by { get; set; } = string.Empty;

        [BsonElement("last_updated")]
        public DateTime last_updated { get; set; }
    }
}
