using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Extensions;

public static class DbUpdateExceptionExtensions
{
    // SQL Server error 2601 / 2627: a row would duplicate a unique index or constraint.
    // Happens when two requests pass the "already exists?" check at the same moment and both try to save.
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
