using Mediator;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public class MongoDbQueryBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IQuery<TResponse>
{
    private readonly MongoConnection _mongoConnection;
    private readonly MongoClient _client;

    public MongoDbQueryBehaviour(
        MongoClient client,
        MongoConnection mongoConnection)
    {
        _client = client;
        _mongoConnection = mongoConnection;
    }

    public async ValueTask<TResponse> Handle(TRequest command, CancellationToken ct, MessageHandlerDelegate<TRequest, TResponse> next)
    {
        _mongoConnection.Database = _client.GetDatabase(_mongoConnection.DatabaseName);
        return await next(command, ct);
    }
}
