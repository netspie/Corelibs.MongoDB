using Common.Basic.DDD;
using System;
using System.Collections.Generic;

namespace Corelibs.MongoDB.Logging
{
    public class Log : Entity
    {
        public List<string> Messages { get; private set; } = new();

        public Log(string message) 
        {
            ID = Guid.NewGuid().ToString();
            Messages.Add(message);
        }

        public Log(object @object)
        {
            ID = Guid.NewGuid().ToString();
            Messages.Add(@object.ToString());
        }

        public Log(object @object, string message)
        {
            ID = Guid.NewGuid().ToString();
            Messages.Add(@object.ToString());
            Messages.Add(message);
        }
    }
}
