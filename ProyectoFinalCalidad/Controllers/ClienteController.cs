using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoFinalCalidad.Controllers
{
    public class ClienteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ClienteController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // MOSTRAR CLIENTES (auto detecta si es admin o usuario)
        // =========================================================
        public async Task<IActionResult> MostrarClientes()
        {
            var clientes = await _context.Clientes.AsNoTracking().ToListAsync();

            // Detectar el rol del usuario logueado
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(rolUsuario) &&
                rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                // Si es administrador, usa la vista en carpeta Admin
                return View("Admin/MostrarClientesAdmin", clientes);
            }

            // Si no, vista normal
            return View(clientes);
        }

        // =========================================================
        // GET: AGREGAR CLIENTE
        // =========================================================
        [HttpGet]
        public IActionResult AgregarCliente()
        {
            var model = new Cliente
            {
                Estado = "activo",
                FechaRegistro = DateTime.Now
            };
            return View(model);
        }

        // =========================================================
        // POST: AGREGAR CLIENTE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarCliente(Cliente cliente)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Verifica los datos ingresados antes de continuar.";
                    return View("AgregarCliente", cliente);
                }

                if (await _context.Clientes.AnyAsync(c => c.Dni == cliente.Dni))
                {
                    TempData["ErrorMessage"] = "Este DNI ya está registrado.";
                    return View("AgregarCliente", cliente);
                }

                if (await _context.Clientes.AnyAsync(c => c.Correo == cliente.Correo))
                {
                    TempData["ErrorMessage"] = "Este correo ya está registrado.";
                    return View("AgregarCliente", cliente);
                }

                var passwordHasher = new PasswordHasher<Cliente>();
                cliente.Password = passwordHasher.HashPassword(cliente, cliente.Password);
                cliente.Estado = "activo";
                cliente.FechaRegistro = DateTime.Now;

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cliente agregado correctamente.";

                // Detectar rol y redirigir según el tipo de usuario
                var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(rolUsuario) &&
                    rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(MostrarClientes));
                }

                return RedirectToAction(nameof(MostrarClientes));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al guardar: {ex.Message}";
                return View("AgregarCliente", cliente);
            }
        }

        // =========================================================
        // EDITAR CLIENTE
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EditarCliente(int? id)
        {
            if (id == null)
                return NotFound();

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCliente(int id, Cliente model, string? NewPassword)
        {
            try
            {
                var clienteExistente = await _context.Clientes.FindAsync(id);
                if (clienteExistente == null)
                {
                    TempData["ErrorMessage"] = "Cliente no encontrado.";
                    return RedirectToAction(nameof(MostrarClientes));
                }

                clienteExistente.Nombres = model.Nombres;
                clienteExistente.Dni = model.Dni;
                clienteExistente.Direccion = model.Direccion;
                clienteExistente.Correo = model.Correo;
                clienteExistente.Telefono = model.Telefono;
                clienteExistente.Estado = model.Estado;
                clienteExistente.FechaRegistro = model.FechaRegistro;

                if (!string.IsNullOrWhiteSpace(NewPassword))
                {
                    var passwordHasher = new PasswordHasher<Cliente>();
                    clienteExistente.Password = passwordHasher.HashPassword(clienteExistente, NewPassword);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cliente actualizado correctamente.";

                // Redirección según el rol
                var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(rolUsuario) &&
                    rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(MostrarClientes));
                }

                return RedirectToAction(nameof(MostrarClientes));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar cliente: {ex.Message}";
                return View("EditarCliente", model);
            }
        }

        // =========================================================
        // INHABILITAR CLIENTE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InhabilitarCliente(int id, string? origen)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound();

            cliente.Estado = "inactivo";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cliente inhabilitado correctamente";

            // Si viene del admin
            if (!string.IsNullOrEmpty(origen) && origen.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(MostrarClientes));

            // Detectar por rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) &&
                rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarClientes));
            }

            return RedirectToAction(nameof(MostrarClientes));
        }

        // =========================================================
        // ELIMINAR CLIENTE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCliente(int id)
        {
            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                if (cliente == null)
                    return NotFound();

                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (DbUpdateException ex) when (ex.InnerException != null &&
                                               ex.InnerException.Message.Contains("REFERENCE"))
            {
                return BadRequest(new { mensaje = "No se pudo eliminar el cliente porque tiene un contrato asociado." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al eliminar: {ex.Message}" });
            }
        }
    }
}
