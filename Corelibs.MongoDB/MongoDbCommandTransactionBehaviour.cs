using Corelibs.Basic.Blocks;
using Mediator;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public class MongoDbCommandTransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBaseCommand
{
    private readonly MongoConnection _mongoConnection;
    private readonly MongoClient _client;

    public MongoDbCommandTransactionBehaviour(
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

                session.StartTransaction();

                response = await next(command, ct);
                if (response is Result result && !result.IsSuccess)
                    return response;

                bool isCommand = typeof(TRequest).GetInterface(typeof(IBaseCommand).Name) != null;
                if (isCommand)
                    await session.CommitTransactionAsync();

                return response;
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                Console.WriteLine(ex.ToString());
                return response;
            }
        }
    }
}
