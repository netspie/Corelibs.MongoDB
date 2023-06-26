using Corelibs.Basic.Blocks;
using Corelibs.Basic.Repository;
using Mediator;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public class MongoDbTransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
{
    private readonly MongoConnection _mongoConnection;
    private readonly MongoClient _client;

    public MongoDbTransactionBehaviour(
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
            try
            {
                _mongoConnection.Session = session;
                _mongoConnection.Database = _client.GetDatabase("MyApp_dev");

                session.StartTransaction();

                var response = await next(command, ct);
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
                throw ex;
                return default;
            }
        }
    }
}
