using Corelibs.Basic.DDD;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public class MongoDbUnitOfWork<TEntity, TEntityId>
    where TEntity : IEntity<TEntityId>
{
    private readonly string _connectionString;
    private readonly MongoDbRepository[] _repositories;

    private readonly List<TEntity> _registeredToAdd = new List<TEntity>();
    private readonly List<TEntity> _registeredToDelete = new List<TEntity>();
    private readonly List<TEntity> _registeredToModify = new List<TEntity>();

    public MongoDbUnitOfWork(string connectionString, params MongoDbRepository[] repositories)
    {
        _connectionString = connectionString;
        _repositories = repositories;
    }

    public void RegisterAdded(object obj)
    {
        if (obj is Entity root)
            _registeredToAdd.Add(root);
    }

    public void RegisterDeleted(object obj)
    {
        if (obj is Entity root)
            _registeredToDelete.Add(root);
    }

    public void RegisterModified(object obj)
    {
        if (obj is Entity root)
            _registeredToModify.Add(root);
    }

    public async Task Commit()
    {
        var client = new MongoClient(_connectionString);

        await CreateNewCollections(client);

        using (var session = await client.StartSessionAsync())
        {
            session.StartTransaction();

            await AddToMongo(session);
            if (!await DeleteFromMongo(session))
                throw new Exception("Could not delete from mongo db");

            if (!await ModifyInMongo(session))
                throw new Exception("Could not modify in mongo db");

            await SaveEvents(session);
            await session.CommitTransactionAsync();
        }

        _registeredToAdd.Clear();
        _registeredToModify.Clear();
        _registeredToDelete.Clear();
    }

    private async Task SaveEvents(IClientSessionHandle session)
    {
        foreach (var root in _registeredToAdd)
        {
            var collection = GetEventCollectionBy(root, session.Client);
            if (collection == null)
                continue;

            var bsons = root.DomainEvents.Select(ev => ev.ToBsonDocument());

            //var ev = BsonSerializer.Deserialize<GroupCreatedEvent>(bsons.ToArray()[0]);
            await collection.InsertManyAsync(session, bsons);
        }
    }

    private async Task CreateNewCollections(MongoClient client)
    {
        foreach (var root in _registeredToAdd)
        {
            var repository = GetRepositoryBy(root);
            var database = client.GetDatabase(repository.DatabaseName);
            if (!await DoesCollectionExists(database, repository.CollectionName))
                await database.CreateCollectionAsync(repository.CollectionName);

            if (string.IsNullOrEmpty(repository.EventStoreCollectionName))
                continue;

            if (!await DoesCollectionExists(database, repository.EventStoreCollectionName))
                await database.CreateCollectionAsync(repository.EventStoreCollectionName);
        }
    }

    private static async Task<bool> DoesCollectionExists(IMongoDatabase database, string collectionName)
    {
        var filter = new BsonDocument("name", collectionName);
        var options = new ListCollectionNamesOptions { Filter = filter };

        var list = await database.ListCollectionNamesAsync(options);
        return list.Any();
    }

    private async Task AddToMongo(IClientSessionHandle session)
    {
        foreach (var root in _registeredToAdd)
        {
            var collection = GetCollectionBy(root, session.Client);
            await collection.InsertOneAsync(session, root);
        }
    }

    private async Task<bool> DeleteFromMongo(IClientSessionHandle session)
    {
        foreach (var root in _registeredToDelete)
        {
            var collection = GetCollectionBy(root, session.Client);
            var res = await collection.DeleteOneAsync(session, r => r.Id == root.Id);
            if (res.DeletedCount == 0)
                return false;
        }

        return true;
    }

    private async Task<bool> ModifyInMongo(IClientSessionHandle session)
    {
        foreach (var root in _registeredToModify)
        {
            var collection = GetCollectionBy(root, session.Client);
            var oldVersion = root.Version++;
            var res = await collection.ReplaceOneAsync(session, r => r.Id == root.Id && r.Version == oldVersion, root, new ReplaceOptions { IsUpsert = false });
            if (res.ModifiedCount == 0)
                return false;
        }

        return true;
    }

    private MongoDbRepository GetRepositoryBy(TEntity root)
    {
        return _repositories.Where(r => r.GetType().GetGenericArguments().Contains(root.GetType())).FirstOrDefault();
    }

    private IMongoCollection<TEntity> GetCollectionBy(TEntity document, IMongoClient client)
    {
        MongoDbRepository repository = GetRepositoryBy(document);

        var database = client.GetDatabase(repository.DatabaseName);
        return database.GetCollection<TEntity>(repository.CollectionName);
    }

    private IMongoCollection<BsonDocument> GetEventCollectionBy(TEntity document, IMongoClient client)
    {
        MongoDbRepository repository = GetRepositoryBy(document);
        if (string.IsNullOrEmpty(repository.EventStoreCollectionName))
            return null;

        var database = client.GetDatabase(repository.DatabaseName);
        return database.GetCollection<BsonDocument>(repository.EventStoreCollectionName);
    }
}