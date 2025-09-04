using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace IDV_Templates_Mongo_API.Data;

public class MongoContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<Models.Template> Templates { get; }
    public IMongoCollection<Models.User> Users { get; }
    public IMongoCollection<Counter> Counters { get; }

    public MongoContext(IOptions<MongoSettings> options)
    {
        var s = options.Value;
        var client = new MongoClient(s.ConnectionString);
        Database = client.GetDatabase(s.Database);
        Templates = Database.GetCollection<Models.Template>(s.TemplatesCollection);
        Users = Database.GetCollection<Models.User>("users");
        Counters = Database.GetCollection<Counter>(s.CountersCollection);

        // Indexes
        Templates.Indexes.CreateOne(new CreateIndexModel<Models.Template>(
            Builders<Models.Template>.IndexKeys.Ascending(t => t.template_id),
            new CreateIndexOptions { Unique = true }));

        Users.Indexes.CreateOne(new CreateIndexModel<Models.User>(
            Builders<Models.User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true }));
    }

    public class Counter
    {
        public string Id { get; set; } = default!; // counter name
        public long Seq { get; set; }
    }

    public async Task<int> GetNextTemplateIdAsync(System.Threading.CancellationToken ct = default)
    {
        var filter = Builders<Counter>.Filter.Eq(x => x.Id, "template_id");
        var update = Builders<Counter>.Update.Inc(x => x.Seq, 1);
        var opts = new FindOneAndUpdateOptions<Counter>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };
        var res = await Counters.FindOneAndUpdateAsync(filter, update, opts, ct);
        return (int)res.Seq;
    }
}
