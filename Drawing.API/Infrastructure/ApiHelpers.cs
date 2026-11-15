using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Drawing.API.Infrastructure;
public static class ApiHelpers
{
    public static Guid UserId(this ControllerBase controller)
    {
        var value = controller.User.FindFirstValue("sub") ?? controller.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var id))
            throw new UnauthorizedAccessException("İstifadəçi identifikasiyası yoxdur.");

        return id;
    }
}
