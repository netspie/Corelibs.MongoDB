using Corelibs.Basic.Blocks;
using Corelibs.Basic.Collections;
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
    public class MongoDbRepository<TEntity, TEntityId> : IRepository<TEntity, TEntityId>
        where TEntity : IEntity<TEntityId>
        where TEntityId : EntityId
    {
        private readonly string _collectionName;
        private readonly MongoConnection _mongoConnection;

        public MongoDbRepository(
            MongoConnection mongoConnection,
            string collectionName)
        {
            _mongoConnection = mongoConnection;
            _collectionName = collectionName;
        }
        
        private  IMongoCollection<TEntity> GetOrCreateCollection()
        {
            return _mongoConnection.Database.GetCollection<TEntity>(_collectionName);
        }

        async Task<Result<TEntity>> IRepository<TEntity, TEntityId>.GetBy(TEntityId id)
        {
            var collection = GetOrCreateCollection();

            var filter = Builders<TEntity>.Filter.Eq("_id", id);

            var findResult = await collection.FindAsync(filter);
            var item = findResult.FirstOrDefault();

            return Result<TEntity>.Success(item);
        }

        public async Task<Result<TEntity[]>> GetBy(IList<TEntityId> ids)
        {
            var collection = GetOrCreateCollection();

            var filter = Builders<TEntity>.Filter.In("_id", ids);

            var findResult = await collection.FindAsync(filter);
            var items = await findResult.ToListAsync();
            var itemsOrdered = items.OrderBy(i => ids.IndexOf(i.Id)).ToArray();

            return Result<TEntity[]>.Success(itemsOrdered.ToArray());
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

            try
            {
                var findResult = await collection.FindAsync(_mongoConnection.Session, filter).ConfigureAwait(false);
                var itemFound = await findResult.FirstOrDefaultAsync().ConfigureAwait(false);
                if (itemFound == null)
                    await collection.InsertOneAsync(item).ConfigureAwait(false);
                else
                {
                    var id = item.Id;
                    var oldVersion = item.Version;

                    item.Version++;

                    var res = await collection.ReplaceOneAsync(c => c.Id.Value == id.Value && c.Version == oldVersion, item,
                        new ReplaceOptions { IsUpsert = false }).ConfigureAwait(false);

                    var isSuccess = res.ModifiedCount > 0;
                    if (!isSuccess)
                        return Result.Failure();
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
            }

            return Result.Success();
        }

        public async Task<Result> Create(TEntity item)
        {
            if (item is null)
                return Result.Failure();

            var collection = GetOrCreateCollection();

            try
            {
                await collection.InsertOneAsync(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving item: {ex.Message}");
                return Result.Failure();
            }

            return Result.Success();
        }

        public async Task<Result> Create(IEnumerable<TEntity> items)
        {
            if (items is null)
                return Result.Failure();

            var collection = GetOrCreateCollection();

            try
            {
                await collection.InsertManyAsync(items);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving items: {ex.Message}");
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
            var res = await collection.DeleteOneAsync(_mongoConnection.Session, filter);

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
