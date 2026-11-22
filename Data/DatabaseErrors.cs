using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Recruitment.Api.Data;

public static class DatabaseErrors
{
    // SQL Server báo một trong hai mã này khi vi phạm ràng buộc duy nhất.
    public static bool IsDuplicate(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
