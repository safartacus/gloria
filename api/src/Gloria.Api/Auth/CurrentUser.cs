using System.Security.Claims;
using Gloria.Application.Abstractions;

namespace Gloria.Api.Auth;

/// <summary>Exposes the authenticated JWT principal to the application layer (audit, row-level checks).</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public string Username => Principal?.FindFirstValue(ClaimTypes.Name) ?? "anonymous";
    public string FullName => Principal?.FindFirstValue("fullName") ?? Username;
    public string RoleCode => Principal?.FindFirstValue(ClaimTypes.Role) ?? "NONE";
    public int? EmployeeId =>
        int.TryParse(Principal?.FindFirstValue("employeeId"), out var id) ? id : null;
}
