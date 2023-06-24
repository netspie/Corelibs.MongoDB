using Corelibs.Basic.Blocks;
using Corelibs.Basic.DDD;
using Corelibs.Basic.Repository;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Corelibs.MongoDB
{
    public class MongoDbRepositoryT<TEntity, TEntityId> : MongoDbRepository, IRepository<TEntity, TEntityId>
        where TEntity : IEntity<TEntityId>
    {
        public MongoDbRepositoryT(
            string connectionString, string databaseName, string collectionName, string eventStoreCollectionName = default) 
            : base(connectionString, databaseName, collectionName, eventStoreCollectionName) { }

        private IMongoDatabase GetOrCreateDatabase()
        {
            var mongoClient = new MongoClient(ConnectionString);
            return mongoClient.GetDatabase(DatabaseName);
        }

        private IMongoCollection<TEntity> GetOrCreateCollection()
        {
            var database = GetOrCreateDatabase();
            return database.GetCollection<TEntity>(CollectionName);
        }

        async Task<Result<TEntity>> IRepository<TEntity, TEntityId>.GetBy(TEntityId id)
        {
            var collection = GetOrCreateCollection();

            var filter = Builders<TEntity>.Filter.Eq("_id", id);
            var findResult = await collection.FindAsync(filter);
            var item = findResult.FirstOrDefault();

            return Result<TEntity>.Success(item);
        }

        public Task<Result<TEntity[]>> GetBy(IList<TEntityId> ids)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TEntity>> GetOfName(string name, Func<TEntity, string> getName)
        {
            throw new NotImplementedException();
        }

        async Task<Result<TEntity[]>> IRepository<TEntity, TEntityId>.GetAll()
        {
            var collection = GetOrCreateCollection();
            var findResult = await collection.FindAsync(Builders<TEntity>.Filter.Empty);
            var findResultList = await findResult.ToListAsync();
            
            return Result<TEntity[]>.Success(findResultList.ToArray());
        }

        public Task<Result<TEntity[]>> GetAll(Action<int> setProgress, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        async public Task<Result> Save(TEntity item)
        {
            var collection = GetOrCreateCollection();

            var filter = Builders<TEntity>.Filter.Eq("_id", item.Id);
            var findResult = await collection.FindAsync(filter);
            var itemFound = findResult.FirstOrDefault();
            if (itemFound == null)
                await collection.InsertOneAsync(item);
            else
            {
                var id = item.Id;
                var oldVersion = item.Version;

                item.Version++;

                var res = await collection.ReplaceOneAsync(c => c.Id.ToString() == id.ToString() && c.Version == oldVersion, item,
                    new ReplaceOptions { IsUpsert = false });

                var isSuccess = res.ModifiedCount > 0;
                if (!isSuccess)
                    return Result.Failure();
            }

            return Result.Success();
        }

        public Task<Result> Clear()
        {
            throw new NotImplementedException();
        }

        public async Task<Result> Delete(TEntityId id)
        {
            var collection = GetOrCreateCollection();
            var filter = Builders<TEntity>.Filter.Eq("_id", id);
            var res = await collection.DeleteOneAsync(filter);

            var isSuccess = res.DeletedCount > 0;
            if (!isSuccess)
                return Result.Failure();

            return Result.Success();
        }

        public Task<Result<bool>> ExistsOfName(string name, Func<TEntity, string> getName)
        {
            throw new NotImplementedException();
        }
    }
}
