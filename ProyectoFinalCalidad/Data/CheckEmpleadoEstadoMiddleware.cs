using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;

public class CheckEstadoUsuarioMiddleware
{
    private readonly RequestDelegate _next;

    public CheckEstadoUsuarioMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, UserManager<IdentityUser> userManager, ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            string userName = context.User.Identity.Name?.Trim();
            string email = string.Empty;
            string dni = userName ?? string.Empty;
            string redirectTipo = null;

            // Buscar empleado en Identity
            var identityUser = await userManager.FindByNameAsync(userName);
            if (identityUser != null)
            {
                email = identityUser.Email?.Trim().ToLower() ?? string.Empty;
            }
            else
            {
                var emailClaim = context.User.Claims.FirstOrDefault(c => c.Type.Contains("email"));
                if (emailClaim != null)
                    email = emailClaim.Value.Trim().ToLower();
            }

            // === EMPLEADO ===
            if (!string.IsNullOrEmpty(email))
            {
                var empleado = await db.Empleados
                    .FirstOrDefaultAsync(e => e.correo.ToLower() == email);

                if (empleado != null && empleado.estado?.ToLower() == "inhabilitado")
                {
                    redirectTipo = "empleado";
                }
            }

            // === CLIENTE ===
            if (!string.IsNullOrEmpty(dni) || !string.IsNullOrEmpty(email))
            {
                var cliente = await db.Clientes
                    .FirstOrDefaultAsync(c => c.Dni == dni || c.Correo.ToLower() == email);

                if (cliente != null && cliente.Estado?.ToLower() == "inactivo")
                {
                    redirectTipo = "cliente";
                }
            }

            // Si hay tipo para redirigir
            if (redirectTipo != null)
            {
                // Logout completo
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                await context.SignOutAsync();

                // Redirigir pasando tipo como parámetro
                context.Response.Redirect($"/Cuenta/Inhabilitado?tipo={redirectTipo}");
                return;
            }
        }

        await _next(context);
    }
}
