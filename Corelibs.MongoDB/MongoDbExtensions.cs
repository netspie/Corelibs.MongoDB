using Corelibs.Basic.DDD;
using Corelibs.Basic.Reflection;
using Corelibs.Basic.Repository;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System;
using MongoDB.Driver;

namespace Corelibs.MongoDB
{
    public static class MongoDbExtensions
    {
        public static void AddMongoRepository<TAggregateRoot, TEntityId>(
            this IServiceCollection services,
            string connectionString, string databaseName, string collectionName)
        where TAggregateRoot : IAggregateRoot<TEntityId>
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
            var aggregateRootTypes = AssemblyExtensionsEx.GetCurrentDomainTypesImplementing<IAggregateRoot>(entitiesAssembly);
            foreach (var type in aggregateRootTypes)
            {
                var collectionName = (string)type.GetField(nameof(IAggregateRoot.DefaultCollectionName), BindingFlags.Static | BindingFlags.Public).GetValue(type);
                services.AddMongoRepository(type, connectionString, databaseName, collectionName);
            }
        }

        public static void AddMongoRepository(
            this IServiceCollection services,
            Type aggregateRootType,
            string connectionString, string databaseName, string collectionName)
        {
            services.AddSingleton(sp => new MongoClient(connectionString));

            services.AddScoped<MongoConnection>(sp => new(databaseName));

            var repositoryInterfaceType = typeof(IRepository<,>);
            var aggregateRootInterfaceType = aggregateRootType.GetInterface(typeof(IAggregateRoot<>).Name);
            var entityIdType = aggregateRootInterfaceType.GetGenericArguments()[0];

            var repositoryInterfaceConcreteType = repositoryInterfaceType.MakeGenericType(aggregateRootType, entityIdType);
            services.AddScoped(repositoryInterfaceConcreteType, sp =>
            {
                var connection = sp.GetRequiredService<MongoConnection>();

                var repoImplConcreteType = typeof(MongoDbRepository<,>).MakeGenericType(aggregateRootType, entityIdType);
                return Activator.CreateInstance(repoImplConcreteType, connection, collectionName);
            });
        }
    }
}
