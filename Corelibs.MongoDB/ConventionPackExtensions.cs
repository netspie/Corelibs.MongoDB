using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization;

namespace Corelibs.MongoDB
{
    public static class ConventionPackExtensions
    {
        public static void AddIDConventionPack()
        {
            var pack = new ConventionPack
            {
                new StringObjectIDConvention()
            };
            ConventionRegistry.Register("MyConventions", pack, _ => true);
        }

        private class StringObjectIDConvention : ConventionBase, IPostProcessingConvention
        {
            public void PostProcess(BsonClassMap classMap)
            {
                var idMap = classMap.IdMemberMap;
                if (idMap != null && idMap.MemberName == "Id" && idMap.MemberType == typeof(string))
                {
                    idMap.SetIdGenerator(new StringObjectIdGenerator());
                }
            }
        }
    }
}
