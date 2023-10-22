using Corelibs.Basic.DDD;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Corelibs.Basic.Collections;
using System.Linq;
using MongoDB.Bson.Serialization;

namespace Corelibs.MongoDB;

public static class MongoProjectionExtensions
{
    public static async Task<TEntityProjection> Get<TEntity, TEntityId, TEntityProjection>(
        this IMongoCollection<TEntity> collection,
        TEntityId id,
        Func<ProjectionDefinition<TEntity>, ProjectionDefinition<TEntity>> projectionBuilder)
        where TEntity : IEntity<TEntityId>
        where TEntityId : EntityId
    {
        var filter = Builders<TEntity>.Filter.Eq("_id", id);
        var projection = projectionBuilder(Builders<TEntity>.Projection.Include(x => x.Id));
        var docs = await collection.Find(filter).Project(projection).ToListAsync();

        return docs
            .Select(doc => BsonSerializer.Deserialize<TEntityProjection>(doc))
            .FirstOrDefault();
    }

    public static async Task<TEntityProjection[]> Get<TEntity, TEntityId, TEntityProjection>(
        this IMongoCollection<TEntity> collection,
        IEnumerable<TEntityId> ids,
        Func<ProjectionDefinition<TEntity>, ProjectionDefinition<TEntity>> projectionBuilder)
        where TEntity : IEntity<TEntityId>
        where TEntityId : EntityId
    {
        if (ids.IsNullOrEmpty())
            return Array.Empty<TEntityProjection>();

        var filter = Builders<TEntity>.Filter.In(x => x.Id, ids);
        var projection = projectionBuilder(Builders<TEntity>.Projection.Include(x => x.Id));
        var docs = await collection.Find(filter).Project(projection).ToListAsync();

        return docs
            .Select(doc => BsonSerializer.Deserialize<TEntityProjection>(doc))
            .ToArray();
    }

    public static Task<TEntityProjection[]> Get<TEntity, TEntityId, TEntityProjection>(
        this IMongoDatabase database,
        string collectionName,
        IEnumerable<TEntityId> ids,
        Func<ProjectionDefinition<TEntity>, ProjectionDefinition<TEntity>> projectionBuilder)
        where TEntity : IEntity<TEntityId>
        where TEntityId : EntityId
    {
        var collection = database.GetCollection<TEntity>(collectionName);
        return collection.Get<TEntity, TEntityId, TEntityProjection>(ids, projectionBuilder);
    }
}
