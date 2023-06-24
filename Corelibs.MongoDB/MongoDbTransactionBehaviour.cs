using Corelibs.Basic.Blocks;
using Mediator;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public class MongoDbTransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
{
    private readonly MongoClient _context;

    public MongoDbTransactionBehaviour(MongoClient client)
    {
        _context = client;
    }

    public async ValueTask<TResponse> Handle(TRequest command, CancellationToken ct, MessageHandlerDelegate<TRequest, TResponse> next)
    {
        using (var session = await _context.StartSessionAsync())
        {
            try
            {
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