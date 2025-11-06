#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;

namespace ProyectoFinalCalidad.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _context;

        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "El correo o DNI es obligatorio")]
            [Display(Name = "Correo o DNI")]
            public string UserOrDni { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria")]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [Display(Name = "Recordarme")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            // ======================================
            // 1. LOGIN POR CLIENTE
            // ======================================
            // Lectura segura de Cliente evitando nulos mediante proyección con COALESCE
            var clienteData = await _context.Clientes
                .Where(c => c.Dni == Input.UserOrDni || c.Correo == Input.UserOrDni)
                .Select(c => new
                {
                    c.ClienteId,
                    Nombres = c.Nombres ?? "",
                    Dni = c.Dni ?? "",
                    Correo = c.Correo ?? "",
                    Password = c.Password ?? "",
                    Estado = c.Estado ?? ""
                })
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (clienteData != null)
            {
                if (!string.Equals(clienteData.Estado, "activo", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Cliente inhabilitado: {clienteData.Nombres} ({clienteData.Dni})");
                    return RedirectToAction("Inhabilitado", "Cuenta");
                }

                var passwordHasher = new PasswordHasher<Cliente>();
                PasswordVerificationResult resultCliente;

                try
                {
                    // 🔹 Primero intentamos verificar la contraseña como hash
                    var tempCliente = new Cliente { ClienteId = clienteData.ClienteId, Nombres = clienteData.Nombres };
                    resultCliente = passwordHasher.VerifyHashedPassword(tempCliente, clienteData.Password, Input.Password);

                    // 🔹 Si falla, comprobamos si el usuario está usando directamente su hash como contraseña
                    if (resultCliente == PasswordVerificationResult.Failed && clienteData.Password == Input.Password)
                    {
                        resultCliente = PasswordVerificationResult.Success;
                    }
                }
                catch (FormatException)
                {
                    // Contraseña antigua (texto plano)
                    resultCliente = clienteData.Password == Input.Password
                        ? PasswordVerificationResult.Success
                        : PasswordVerificationResult.Failed;
                }

                if (resultCliente != PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError(string.Empty, "Contraseña incorrecta.");
                    return Page();
                }

                // 🔹 Si la contraseña era texto plano, la actualizamos a hash
                if (!string.IsNullOrEmpty(clienteData.Password) && !clienteData.Password.StartsWith("$"))
                {
                    try
                    {
                        var clienteToUpdate = await _context.Clientes.FindAsync(clienteData.ClienteId);
                        if (clienteToUpdate != null)
                        {
                            clienteToUpdate.Password = passwordHasher.HashPassword(clienteToUpdate, Input.Password);
                            _context.Update(clienteToUpdate);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"No se pudo actualizar a hash la contraseña del cliente {clienteData.ClienteId}: {ex.Message}");
                    }
                }

                var claimsCliente = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, clienteData.Nombres ?? "Cliente"),
                    new Claim("ClienteId", clienteData.ClienteId.ToString()),
                    new Claim("ClienteDni", clienteData.Dni ?? ""),
                    new Claim(ClaimTypes.Role, "Usuario")
                };

                await IniciarSesionAsync(claimsCliente, Input.RememberMe);

                HttpContext.Session.SetString("ClienteId", clienteData.ClienteId.ToString());
                HttpContext.Session.SetString("ClienteNombre", clienteData.Nombres ?? "");
                HttpContext.Session.SetString("ClienteDni", clienteData.Dni ?? "");

                _logger.LogInformation($"Cliente {clienteData.Nombres} inició sesión correctamente.");
                return LocalRedirect(returnUrl);
            }

            // ======================================
            // 2. LOGIN POR EMPLEADO
            // ======================================
            var empleado = await _context.Empleados
                .Include(e => e.Cargo)
                .FirstOrDefaultAsync(e => e.correo == Input.UserOrDni || e.dni == Input.UserOrDni);

            if (empleado != null)
            {
                if (!string.Equals(empleado.estado, "activo", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Empleado inhabilitado: {empleado.nombres} ({empleado.correo})");
                    return RedirectToAction("Inhabilitado", "Cuenta");
                }

                var passwordHasherEmpleado = new PasswordHasher<Empleado>();
                PasswordVerificationResult resultEmpleado;

                try
                {
                    resultEmpleado = passwordHasherEmpleado.VerifyHashedPassword(empleado, empleado.password, Input.Password);

                    // 🔹 Igual que antes: comprobamos si está ingresando directamente su hash
                    if (resultEmpleado == PasswordVerificationResult.Failed && empleado.password == Input.Password)
                    {
                        resultEmpleado = PasswordVerificationResult.Success;
                    }
                }
                catch (FormatException)
                {
                    resultEmpleado = empleado.password == Input.Password
                        ? PasswordVerificationResult.Success
                        : PasswordVerificationResult.Failed;
                }

                if (resultEmpleado != PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError(string.Empty, "Contraseña incorrecta.");
                    return Page();
                }

                if (!empleado.password.StartsWith("$"))
                {
                    empleado.password = passwordHasherEmpleado.HashPassword(empleado, Input.Password);
                    _context.Update(empleado);
                    await _context.SaveChangesAsync();
                }

                string rolEmpleado = "Empleado";
                if (!string.IsNullOrEmpty(empleado.Cargo?.titulo_cargo) &&
                    empleado.Cargo.titulo_cargo.ToLower().Contains("admin"))
                {
                    rolEmpleado = "Administrador";
                }

                var claimsEmpleado = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, empleado.nombres ?? "Empleado"),
                    new Claim("EmpleadoId", empleado.empleado_id.ToString()),
                    new Claim("EmpleadoCorreo", empleado.correo ?? ""),
                    new Claim("Cargo", empleado.Cargo?.titulo_cargo ?? "Empleado"),
                    new Claim(ClaimTypes.Role, rolEmpleado)
                };

                await IniciarSesionAsync(claimsEmpleado, Input.RememberMe);

                HttpContext.Session.SetString("EmpleadoId", empleado.empleado_id.ToString());
                HttpContext.Session.SetString("EmpleadoNombre", empleado.nombres ?? "");
                HttpContext.Session.SetString("EmpleadoCorreo", empleado.correo ?? "");
                HttpContext.Session.SetString("EmpleadoCargo", empleado.Cargo?.titulo_cargo ?? "");
                HttpContext.Session.SetString("EmpleadoRol", rolEmpleado);

                _logger.LogInformation($"Empleado {empleado.nombres} inició sesión correctamente como {rolEmpleado}.");
                return LocalRedirect(returnUrl);
            }

            // ======================================
            // 3. LOGIN IDENTITY (usuarios base)
            // ======================================
            var result = await _signInManager.PasswordSignInAsync(Input.UserOrDni, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario Identity inició sesión correctamente.");
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Usuario bloqueado.");
                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(string.Empty, "Intento de inicio de sesión inválido.");
            return Page();
        }

        private async Task IniciarSesionAsync(List<Claim> claims, bool rememberMe)
        {
            var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTime.UtcNow.AddHours(2)
            });
        }
    }
}
