using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoFinalCalidad.Controllers
{
    public class EmpleadoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpleadoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // 🔹 MOSTRAR EMPLEADOS (Vista empleado)
        // =========================================================
        public async Task<IActionResult> MostrarEmpleados()
        {
            var empleados = await _context.Empleados
                .Include(e => e.Cargo)
                .AsNoTracking()
                .ToListAsync();

            return View(empleados);
        }

        // =========================================================
        // 🔹 MOSTRAR EMPLEADOS (Vista del ADMIN)
        // =========================================================
        public async Task<IActionResult> MostrarEmpleadosAdmin()
        {
            var empleados = await _context.Empleados
                .Include(e => e.Cargo)
                .AsNoTracking()
                .ToListAsync();

            // Forzamos la vista dentro de la carpeta Admin
            return View("Admin/MostrarEmpleadosAdmin", empleados);
        }

        // =========================================================
        // 🔹 AGREGAR EMPLEADO
        // =========================================================
        [HttpGet]
        public IActionResult Agregar()
        {
            CargarCargosViewBag();
            var model = new Empleado
            {
                estado = "activo",
                fecha_inicio = DateTime.Today
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarEmpleado(Empleado empleado, int mesesDuracion)
        {
            try
            {
                if (await _context.Empleados.AnyAsync(e => e.dni == empleado.dni))
                {
                    TempData["ErrorMessage"] = "Este DNI ya está registrado";
                    CargarCargosViewBag();
                    return View("Agregar", empleado);
                }

                empleado.estado = "activo";
                empleado.fecha_fin = empleado.fecha_inicio?.AddMonths(mesesDuracion);

                var passwordHasher = new PasswordHasher<Empleado>();
                empleado.password = passwordHasher.HashPassword(empleado, empleado.password);

                _context.Empleados.Add(empleado);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Empleado agregado correctamente";
                // Detectar el rol del usuario logueado
                var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

                // Si es administrador → redirige a MostrarEmpleadosAdmin
                if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(MostrarEmpleadosAdmin));
                }

                // Si no, redirige a la vista normal
                return RedirectToAction(nameof(MostrarEmpleados));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al guardar: {ex.Message}";
                CargarCargosViewBag();
                return View("Agregar", empleado);
            }
        }

        // =========================================================
        // 🔹 EDITAR EMPLEADO
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
                return NotFound();

            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
                return NotFound();

            CargarCargosViewBag();
            return View(empleado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEmpleado(int id, Empleado empleado, string? NewPassword)
        {
            try
            {
                var empleadoExistente = await _context.Empleados.FindAsync(id);
                if (empleadoExistente == null)
                {
                    TempData["ErrorMessage"] = "Empleado no encontrado.";
                    return RedirectToAction(nameof(MostrarEmpleados));
                }

                empleadoExistente.cargo_id = empleado.cargo_id;
                empleadoExistente.nombres = empleado.nombres;
                empleadoExistente.dni = empleado.dni;
                empleadoExistente.telefono = empleado.telefono;
                empleadoExistente.correo = empleado.correo;
                empleadoExistente.estado = empleado.estado;
                empleadoExistente.fecha_inicio = empleado.fecha_inicio;
                empleadoExistente.fecha_fin = empleado.fecha_fin;

                if (!string.IsNullOrWhiteSpace(NewPassword))
                    empleadoExistente.password = NewPassword;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Empleado actualizado correctamente.";

                // Detectar el rol del usuario logueado
                var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

                // Si es administrador → redirige a MostrarEmpleadosAdmin
                if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(MostrarEmpleadosAdmin));
                }

                // Si no, redirige a la vista normal
                return RedirectToAction(nameof(MostrarEmpleados));

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar: {ex.Message}";
                CargarCargosViewBag();
                return View("Editar", empleado);
            }
        }

        // =========================================================
        // 🔹 INHABILITAR EMPLEADO (funciona en vista normal y admin)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InhabilitarEmpleado(int id, string? origen)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
                return NotFound();

            empleado.estado = "inhabilitado";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Empleado inhabilitado correctamente";

            // 🔹 1️⃣ Si se indica manualmente el origen (por formulario)
            if (!string.IsNullOrEmpty(origen) && origen.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(MostrarEmpleadosAdmin));

            // 🔹 2️⃣ Si no se pasa el parámetro, detectar por rol del usuario logueado
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(MostrarEmpleadosAdmin));

            // 🔹 3️⃣ Redirección por defecto (vista de empleados normal)
            return RedirectToAction(nameof(MostrarEmpleados));
        }


        // =========================================================
        // 🔹 ELIMINAR EMPLEADO -- SOLO ADMIN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEmpleado(int id)
        {
            try
            {
                var empleado = await _context.Empleados.FindAsync(id);
                if (empleado == null)
                    return NotFound();

                _context.Empleados.Remove(empleado);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (DbUpdateException ex) when (ex.InnerException != null &&
                                               ex.InnerException.Message.Contains("REFERENCE"))
            {
                return BadRequest(new { mensaje = "No se pudo eliminar el empleado porque tiene un contrato asociado." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al eliminar: {ex.Message}" });
            }
        }

        // =========================================================
        // 🔹 MÉTODOS AUXILIARES
        // =========================================================
        private bool EmpleadoExists(int id)
        {
            return _context.Empleados.Any(e => e.empleado_id == id);
        }

        private void CargarCargosViewBag()
        {
            ViewBag.Cargos = new SelectList(_context.Cargos.AsNoTracking().ToList(), "cargo_id", "titulo_cargo");
        }
    }
}
