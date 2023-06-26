using MongoDB.Driver;

namespace Corelibs.MongoDB;

public class MongoConnection
{
    public IClientSessionHandle Session { get; set; }
    public IMongoDatabase Database { get; set; }
}
