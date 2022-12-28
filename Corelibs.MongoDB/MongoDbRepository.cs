namespace Corelibs.MongoDB
{
    public abstract class MongoDbRepository
    {
        internal string ConnectionString { get; }
        internal string DatabaseName { get; }
        internal string CollectionName { get; }
        internal string EventStoreCollectionName { get; }

        public MongoDbRepository(
            string connectionString, string databaseName, string collectionName, string eventStoreCollectionName)
        {
            ConnectionString = connectionString;
            DatabaseName = databaseName;
            CollectionName = collectionName;
            EventStoreCollectionName = eventStoreCollectionName;
        }
    }
}
