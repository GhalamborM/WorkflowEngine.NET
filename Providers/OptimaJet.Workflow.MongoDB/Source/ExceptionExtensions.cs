using MongoDB.Driver;

namespace OptimaJet.Workflow.MongoDB;

internal static class ExceptionExtensions
{
    public static bool IsDuplicateKeyException(this MongoWriteException exception)
    {
        return exception.WriteError?.Category == ServerErrorCategory.DuplicateKey;
    }
}
