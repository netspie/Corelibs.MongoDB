using Common.Basic.Blocks;
using Common.Basic.DDD;
using Common.Basic.Repository;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Corelibs.MongoDB
{
    public class MongoDbRepositoryT<TRoot> : MongoDbRepository, IRepository<TRoot>
        where TRoot : Entity
    {
        public MongoDbRepositoryT(
            string connectionString, string databaseName, string collectionName, string eventStoreCollectionName = default) 
            : base(connectionString, databaseName, collectionName, eventStoreCollectionName) { }

        private IMongoDatabase GetOrCreateDatabase()
        {
            var mongoClient = new MongoClient(ConnectionString);
            return mongoClient.GetDatabase(DatabaseName);
        }

        private IMongoCollection<TRoot> GetOrCreateCollection()
        {
            var database = GetOrCreateDatabase();
            return database.GetCollection<TRoot>(CollectionName);
        }

        async Task<Result<TRoot>> IRepository<TRoot>.GetBy(string id)
        {
            var collection = GetOrCreateCollection();

            var filter = Builders<TRoot>.Filter.Eq("_id", id);
            var findResult = await collection.FindAsync(filter);
            var item = findResult.FirstOrDefault();

            return Result<TRoot>.Success(item);
        }

        public Task<Result<TRoot[]>> GetBy(IList<string> ids)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TRoot>> GetOfName(string name, Func<TRoot, string> getName)
        {
            throw new NotImplementedException();
        }

        async Task<Result<TRoot[]>> IRepository<TRoot>.GetAll()
        {
            var collection = GetOrCreateCollection();
            var findResult = await collection.FindAsync(Builders<TRoot>.Filter.Empty);
            var findResultList = await findResult.ToListAsync();
            
            return Result<TRoot[]>.Success(findResultList.ToArray());
        }

        public Task<Result<TRoot[]>> GetAll(Action<int> setProgress, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        async public Task<Result> Save(TRoot item)
        {
            var collection = GetOrCreateCollection();

            var filter = Builders<TRoot>.Filter.Eq("_id", item.ID);
            var findResult = await collection.FindAsync(filter);
            var itemFound = findResult.FirstOrDefault();
            if (itemFound == null)
                await collection.InsertOneAsync(item);
            else
            {
                var id = item.ID;
                var oldVersion = item.Version;

                item.Version++;

                var res = await collection.ReplaceOneAsync(c => c.ID == id && c.Version == oldVersion, item,
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

        public async Task<Result> Delete(string id)
        {
            var collection = GetOrCreateCollection();
            var filter = Builders<TRoot>.Filter.Eq("_id", id);
            var res = await collection.DeleteOneAsync(filter);

            var isSuccess = res.DeletedCount > 0;
            if (!isSuccess)
                return Result.Failure();

            return Result.Success();
        }

        public Task<Result<bool>> ExistsOfName(string name, Func<TRoot, string> getName)
        {
            throw new NotImplementedException();
        }
    }
}
