using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Infrastructure;

public static class ControllerErrorExtensions
{
    public static ObjectResult DatabaseConflict(this ControllerBase controller, DbUpdateException exception)
    {
        return controller.Conflict(new ProblemDetails
        {
            Title = "Database update conflict",
            Detail = exception.GetBaseException().Message,
            Status = StatusCodes.Status409Conflict
        });
    }
}
