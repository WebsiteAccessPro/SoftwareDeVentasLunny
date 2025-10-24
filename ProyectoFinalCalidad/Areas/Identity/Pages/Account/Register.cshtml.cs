﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ProyectoFinalCalidad.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Correo electrónico")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario Identity creado correctamente.");

                    // Asignar rol predeterminado
                    string rolAsignado = "Empleado";
                    if (!await _roleManager.RoleExistsAsync(rolAsignado))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(rolAsignado));
                        _logger.LogInformation("Rol 'Empleado' creado porque no existía.");
                    }

                    await _userManager.AddToRoleAsync(user, rolAsignado);

                    // Registrar empleado en tabla Empleado
                    try
                    {
                        _logger.LogInformation("Intentando crear registro en tabla Empleado...");

                        var random = new Random();
                        string telefonoAleatorio = random.Next(900000000, 999999999).ToString();

                        var nuevoEmpleado = new Empleado
                        {
                            cargo_id = 6,
                            correo = Input.Email,
                            password = Input.Password,
                            estado = "pendiente",
                            fecha_inicio = DateTime.Now,
                            fecha_fin = null,
                            nombres = "Null",
                            dni = Guid.NewGuid().ToString().Substring(0, 8),
                            telefono = telefonoAleatorio
                        };

                        _context.Empleados.Add(nuevoEmpleado);
                        int filas = await _context.SaveChangesAsync();

                        if (filas > 0)
                            _logger.LogInformation("Registro insertado correctamente en tabla Empleado.");
                        else
                            _logger.LogWarning("No se insertó ningún registro en la tabla Empleado.");
                    }
                    catch (DbUpdateException ex)
                    {
                        string msg = ex.InnerException?.Message ?? ex.Message;
                        _logger.LogError($"Error al guardar en tabla Empleado: {msg}");
                        ModelState.AddModelError(string.Empty, $"Error BD: {msg}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error general al crear empleado: {ex}");
                        ModelState.AddModelError(string.Empty, "No se pudo crear el registro en la tabla Empleado. Revisa los logs.");
                    }

                    // Confirmación de correo electrónico
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirmar cuenta",
                        $"Por favor confirma tu cuenta haciendo clic <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>aquí</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                // Mostrar errores del registro Identity
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"No se puede crear una instancia de '{nameof(IdentityUser)}'. " +
                    $"Asegúrate de que '{nameof(IdentityUser)}' no es abstracto y tiene un constructor sin parámetros.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("El UserStore debe soportar email.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
