using Oracle.ManagedDataAccess.Client;

namespace OptimaJet.Workflow.Oracle;

internal static class ExceptionExtensions
{
    public static bool IsDuplicateKeyException(this OracleException exception)
    {
        return exception.Number == 1;
    }
}
