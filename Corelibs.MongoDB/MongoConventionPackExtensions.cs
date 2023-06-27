using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization;
using System.Reflection;
using Corelibs.Basic.Repository;

namespace Corelibs.MongoDB
{
    public static class MongoConventionPackExtensions
    {
        #region Id Convention

        public static void AddIDConventionPack()
        {
            var pack = new ConventionPack
            {
                new StringObjectIDConvention()
            };
            ConventionRegistry.Register(nameof(StringObjectIDConvention), pack, _ => true);
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

        #endregion

        #region Ignore Convention

        public static void AddIgnoreConventionPack()
        {
            var pack = new ConventionPack
            {
                new IgnoreConvention()
            };
            ConventionRegistry.Register(nameof(IgnoreConvention), pack, _ => true);
        }

        private class IgnoreConvention : ConventionBase, IMemberMapConvention
        {
            public void Apply(BsonMemberMap memberMap)
            {
                var ignoreAttribute = memberMap.MemberInfo.GetCustomAttribute<IgnoreAttribute>();
                if (ignoreAttribute is null)
                {
                    memberMap.SetShouldSerializeMethod(obj => true);
                    return;
                }

                memberMap.SetIgnoreIfDefault(true);
                memberMap.SetShouldSerializeMethod(obj => false);
            }
        }

        #endregion
    }
}
