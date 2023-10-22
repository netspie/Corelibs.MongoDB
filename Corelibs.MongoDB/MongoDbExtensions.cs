using Corelibs.Basic.Collections;
using Corelibs.Basic.DDD;
using Corelibs.Basic.Reflection;
using Corelibs.Basic.Repository;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public static class MongoDbExtensions
{
    public static void AddMongoRepository<TAggregateRoot, TEntityId>(
        this IServiceCollection services,
        string connectionString, string databaseName, string collectionName)
        where TAggregateRoot : IAggregateRoot<TEntityId>
        where TEntityId : EntityId
    {
        services.AddSingleton(sp => new MongoClient(connectionString));

        services.AddScoped<MongoConnection>(sp => new(databaseName));

        services.AddScoped<IRepository<TAggregateRoot, TEntityId>>(sp =>
        {
            var connection = sp.GetRequiredService<MongoConnection>();
            return new MongoDbRepository<TAggregateRoot, TEntityId>(connection, collectionName);
        });
    }

    public static void AddMongoRepositories(
        this IServiceCollection services,
        Assembly entitiesAssembly,
        string connectionString, string databaseName)
    {
        services.AddSingleton(sp => new MongoClient(connectionString));
        services.AddScoped<MongoConnection>(sp => new(databaseName));

        using var serviceProvider = services.BuildServiceProvider();
        var aggregateRootTypes = AssemblyExtensionsEx.GetCurrentDomainTypesImplementing<IAggregateRoot>(entitiesAssembly);
        foreach (var type in aggregateRootTypes)
        {
            var collectionName = (string)type.GetProperty(nameof(IAggregateRoot<EntityId>.DefaultCollectionName), BindingFlags.Static | BindingFlags.Public).GetValue(type);
            services.AddMongoRepository(serviceProvider, type, connectionString, databaseName, collectionName);
        }
    }

    public static void AddMongoRepository(
        this IServiceCollection services,
        IServiceProvider serviceProvider,
        Type aggregateRootType,
        string connectionString, string databaseName, string collectionName)
    {
        if (connectionString.IsNullOrEmpty())
            Console.WriteLine("Mongo database connection string cannot be empty!");

        var repositoryInterfaceType = typeof(IRepository<,>);
        var aggregateRootInterfaceType = aggregateRootType.GetInterface(typeof(IAggregateRoot<>).Name);
        var entityIdType = aggregateRootInterfaceType.GetGenericArguments()[0];

        var repositoryInterfaceConcreteType = repositoryInterfaceType.MakeGenericType(aggregateRootType, entityIdType);
        if (serviceProvider.GetService(repositoryInterfaceConcreteType) is not null)
            return;

        services.AddScoped(repositoryInterfaceConcreteType, sp =>
        {
            var connection = sp.GetRequiredService<MongoConnection>();

            var repoImplConcreteType = typeof(MongoDbRepository<,>).MakeGenericType(aggregateRootType, entityIdType);
            return Activator.CreateInstance(repoImplConcreteType, connection, collectionName);
        });
    }

    public static bool CanHaveTransactions(string connectionString)
    {
        try
        {
            var result = new MongoClient(connectionString)
                .GetDatabase("admin")
                .RunCommand(new BsonDocumentCommand<BsonDocument>(new BsonDocument("replSetGetStatus", 1)));

            return result.Contains("replSet");
        }
        catch (MongoException)
        {
            return false;
        }
    }

    public static async Task CreateIndex<T>(
        this IMongoDatabase database,
        string collectionName,
        Func<IndexKeysDefinitionBuilder<T>, IndexKeysDefinition<T>> buildIndex)
    {
        var indexKeys = buildIndex(Builders<T>.IndexKeys);
        var indexModel = new CreateIndexModel<T>(indexKeys);
        var eventsCollection = database.GetCollection<T>(collectionName);
        await eventsCollection.Indexes.CreateOneAsync(indexModel);
    }
}
