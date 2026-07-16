using MySqlConnector;

namespace OptimaJet.Workflow.MySQL;

internal static class ExceptionExtensions
{
    public static bool IsDuplicateKeyException(this MySqlException exception)
    {
        return exception.ErrorCode == MySqlErrorCode.DuplicateKeyEntry;
    }
}
