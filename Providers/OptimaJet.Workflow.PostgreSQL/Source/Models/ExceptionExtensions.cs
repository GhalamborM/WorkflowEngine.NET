using Npgsql;

namespace OptimaJet.Workflow.PostgreSQL;

internal static class ExceptionExtensions
{
    public static bool IsDuplicateKeyException(this PostgresException exception)
    {
        return exception.SqlState == "23505";
    }
}
