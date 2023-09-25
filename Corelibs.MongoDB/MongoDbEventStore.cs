using Corelibs.Basic.Blocks;
using Corelibs.Basic.Collections;
using Corelibs.Basic.DDD;
using Corelibs.Basic.UseCases.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public class MongoDbEventStore : IEventStore
{
    private readonly MongoConnection _connection;
    private readonly Dictionary<string, Type> _eventTypes;
    private readonly string _collectionName;

    public MongoDbEventStore(
        MongoConnection connection, Dictionary<string, Type> eventTypes)
    {
        _connection = connection;
        _eventTypes = eventTypes;
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
        if (events.IsEmpty())
            return Result.Success();

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

    async Task<Result<BaseDomainEvent[]>> IEventStore.Peek(int count = 1)
    {
        var collection = _connection.Database.GetCollection<BsonDocument>(_collectionName);

        try
        {
            var hint = new BsonDocument(nameof(BaseDomainEvent.Timestamp), 1);

            var filter = Builders<BsonDocument>.Filter.Empty;
            var docs = await collection.Find(filter, new FindOptions { Hint = hint }).Limit(count).ToListAsync();

            var items = docs.Select(document =>
            {
                var eventName = document["_t"].AsString;
                if (_eventTypes.ContainsKey(eventName))
                {
                    var targetType = _eventTypes[eventName];
                    var serializer = BsonSerializer.LookupSerializer(targetType);
                   
                    return (BaseDomainEvent) BsonSerializer.Deserialize(document, targetType);
                }
                else
                {
                    throw new Exception("Unknown discriminator value.");
                }
            }).ToArray();

            return Result<BaseDomainEvent[]>.Success(items.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return Result<BaseDomainEvent[]>.Failure(ex);
        }
    }

    async Task<Result> IEventStore.Delete(BaseDomainEvent ev)
    {
        var collection = _connection.Database.GetCollection<BaseDomainEvent>(_collectionName);

        try
        {
            var filter = Builders<BaseDomainEvent>.Filter.Eq("_id", ev.Id);
            var result = await collection.DeleteOneAsync(filter);
            if (result.DeletedCount > 0)
                return Result.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        return Result.Failure();
    }
}
