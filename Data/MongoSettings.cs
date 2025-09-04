namespace IDV_Templates_Mongo_API.Data;
public class MongoSettings
{
    public string ConnectionString { get; set; } = "";
    public string Database { get; set; } = "idv_demo";
    public string TemplatesCollection { get; set; } = "templates";
    public string CountersCollection { get; set; } = "counters";
}
