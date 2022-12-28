using Common.Basic.Repository;
using Corelibs.Basic.Logging;

namespace Corelibs.MongoDB.Logging
{
    public class MongoDbLogger : ILogger
    {
        private readonly IRepository<Log> _logRepository;

        public MongoDbLogger(IRepository<Log> logRepository)
        {
            _logRepository = logRepository;
            ConventionPackExtensions.AddIDConventionPack();
        }

        public void Log(string message)
        {
            var log = new Log(message);
            _logRepository.Save(log);
        }

        public void Log(object @object)
        {
            var log = new Log(@object);
            _logRepository.Save(log);
        }

        public void Log(object @object, string message)
        {
            var log = new Log(@object, message);
            _logRepository.Save(log);
        }
    }
}
