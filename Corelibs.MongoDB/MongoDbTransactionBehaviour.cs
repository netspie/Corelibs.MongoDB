﻿using Corelibs.Basic.Blocks;
using Mediator;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Corelibs.MongoDB;

public class MongoDbTransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
{
    private readonly IClientSessionHandle _session;

    public MongoDbTransactionBehaviour(IClientSessionHandle session)
    {
        _session = session;
    }

    public async ValueTask<TResponse> Handle(TRequest command, CancellationToken ct, MessageHandlerDelegate<TRequest, TResponse> next)
    {
        try
        {
            _session.StartTransaction();

            var response = await next(command, ct);
            if (response is Result result && !result.IsSuccess)
                return response;

            bool isCommand = typeof(TRequest).GetInterface(typeof(IBaseCommand).Name) != null;
            if (isCommand)
                await _session.CommitTransactionAsync();

            return response;
        }
        catch (Exception ex)
        {
            await _session.AbortTransactionAsync();
            Console.WriteLine(ex.ToString());
            throw ex;
            return default;
        }
        finally
        {
            _session.Dispose();
        }
    }
}
