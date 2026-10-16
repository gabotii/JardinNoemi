using JardinNoemi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JardinNoemi.Components.Account;

public static class Policies
{
    public const string Admin = "AdminOnly";
}

internal static class AccountEndpoints
{
    /// <summary>Cierre de sesión por POST (con antiforgery automático al enlazar [FromForm]).</summary>
    public static IEndpointConventionBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/cuenta/salir", async (
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string? returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect(SafeLocalUrl(returnUrl));
        });
    }

    /// <summary>Evita redirecciones abiertas: solo rutas locales absolutas ("/algo", nunca "//dominio").</summary>
    public static string SafeLocalUrl(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\")
            ? url
            : "/";
}
