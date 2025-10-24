// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file under the MIT license.
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

            // Cierra sesión externa si existe
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
            // 1.Intentar login por DNI (Clientes)
            // ======================================
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Dni == Input.UserOrDni);

            if (cliente != null)
            {
                // Si el cliente está inactivo → redirigir a vista Inhabilitado
                if (!string.Equals(cliente.Estado, "activo", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Intento de inicio de sesión de cliente inhabilitado: {cliente.Nombres} ({cliente.Dni})");
                    return RedirectToAction("Inhabilitado", "Cuenta");
                }

                if (cliente.Password != Input.Password)
                {
                    ModelState.AddModelError(string.Empty, "Contraseña incorrecta.");
                    return Page();
                }

                var claimsCliente = new List<Claim>
        {
            new Claim(ClaimTypes.Name, cliente.Nombres ?? "Cliente"),
            new Claim("ClienteId", cliente.ClienteId.ToString()),
            new Claim("ClienteDni", cliente.Dni),
            new Claim(ClaimTypes.Role, "Usuario")
        };

                await IniciarSesionAsync(claimsCliente, Input.RememberMe);
                HttpContext.Session.SetString("ClienteId", cliente.ClienteId.ToString());
                HttpContext.Session.SetString("ClienteNombre", cliente.Nombres ?? "");
                HttpContext.Session.SetString("ClienteDni", cliente.Dni ?? "");

                _logger.LogInformation($"Cliente {cliente.Nombres} inició sesión correctamente.");
                return LocalRedirect(returnUrl);
            }

            // ======================================
            // 2. Intentar login como Empleado
            // ======================================
            var empleado = await _context.Empleados
                .Include(e => e.Cargo)
                .FirstOrDefaultAsync(e =>
                    e.correo == Input.UserOrDni || e.dni == Input.UserOrDni);

            if (empleado != null)
            {
                // Si el empleado está inactivo → redirigir a vista Inhabilitado
                if (!string.Equals(empleado.estado, "activo", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Intento de inicio de sesión de empleado inhabilitado: {empleado.nombres} ({empleado.correo})");
                    return RedirectToAction("Inhabilitado", "Cuenta");
                }

                if (empleado.password != Input.Password)
                {
                    ModelState.AddModelError(string.Empty, "Contraseña incorrecta.");
                    return Page();
                }

                // Rol predeterminado: Empleado
                string rolEmpleado = "Empleado";

                // Si el cargo tiene un título que denote administrador, se cambia el rol
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
            // 3. Intentar login como usuario Identity
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

            // Si no coincide con ninguno
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
    