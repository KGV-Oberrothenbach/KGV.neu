namespace KGV.Core.Models;

public class PreparedContractRepositoryException : Exception
{
    public PreparedContractRepositoryException(string message) : base(message) { }
}

public sealed class PreparedContractNotFoundException : PreparedContractRepositoryException
{
    public PreparedContractNotFoundException(string message) : base(message) { }
}

public sealed class PreparedContractConflictException : PreparedContractRepositoryException
{
    public PreparedContractConflictException(string message) : base(message) { }
}
