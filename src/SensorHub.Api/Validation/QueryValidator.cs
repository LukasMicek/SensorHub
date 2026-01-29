using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SensorHub.Api.Validation;

public static class QueryValidator
{
    public const int MaxLimit = 500;

    public static ActionResult? ValidateLimit(int limit)
    {
        if (limit <= 0 || limit > MaxLimit)
            return new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid limit",
                Detail = $"Limit must be between 1 and {MaxLimit}"
            });
        return null;
    }

    public static ActionResult? ValidateDateRange(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid date range",
                Detail = "'from' must be less than or equal to 'to'"
            });
        return null;
    }

    public static ActionResult? ValidateQuery(int limit, DateTime? from = null, DateTime? to = null)
    {
        return ValidateLimit(limit) ?? ValidateDateRange(from, to);
    }
}
