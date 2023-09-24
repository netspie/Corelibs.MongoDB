using Corelibs.Basic.Blocks;
using Corelibs.Basic.DDD;
using Corelibs.Basic.UseCases;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public class MongoDbEventStore : IEventStore
{
    private readonly MongoConnection _connection;
    private readonly string _collectionName;

    public MongoDbEventStore(
        MongoConnection connection)
    {
        _connection = connection;
        _collectionName = "events";
    }

    async Task<Result> IEventStore.Save(BaseDomainEvent ev)
    {
        var collection = _connection.Database.GetCollection<BaseDomainEvent>(_collectionName);

        try
        {
            await collection.InsertOneAsync(ev);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return Result.Failure(ex);
        }

        return Result.Success();
    }

    async Task<Result> IEventStore.Save(IEnumerable<BaseDomainEvent> events)
    {
        var collection = _connection.Database.GetCollection<BaseDomainEvent>(_collectionName);

        try
        {
            await collection.InsertManyAsync(events);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return Result.Failure(ex);
        }

        return Result.Success();
    }

    async Task<Result<BaseDomainEvent[]>> IEventStore.Dequeue(int count = 1)
    {
        var collection = _connection.Database.GetCollection<BaseDomainEvent>(_collectionName);

        try
        {
            var hint = new BsonDocument(nameof(BaseDomainEvent.Timestamp), 1);

            var filter = Builders<BaseDomainEvent>.Filter.Empty;
            var items = await collection.Find(filter, new FindOptions { Hint = hint }).Limit(count).ToListAsync();

            return Result<BaseDomainEvent[]>.Success(items.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return Result<BaseDomainEvent[]>.Failure(ex);
        }
    }
}
