using Mediator;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public class MongoDbCommandNoTransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBaseCommand
{
    private readonly MongoConnection _mongoConnection;
    private readonly MongoClient _client;

    public MongoDbCommandNoTransactionBehaviour(
        MongoClient client,
        MongoConnection mongoConnection)
    {
        _client = client;
        _mongoConnection = mongoConnection;
    }

    public async ValueTask<TResponse> Handle(TRequest command, CancellationToken ct, MessageHandlerDelegate<TRequest, TResponse> next)
    {
        using (var session = await _client.StartSessionAsync())
        {
            TResponse response = default;
            try
            {
                _mongoConnection.Session = session;
                _mongoConnection.Database = _client.GetDatabase(_mongoConnection.DatabaseName);

                return await next(command, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return response;
            }
        }
    }
}
