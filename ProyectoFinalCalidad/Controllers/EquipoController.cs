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
    public class EquipoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EquipoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PARA ADMIN: MOSTRAR EQUIPOS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> MostrarEquiposAdmin()
        {
            var equipos = await _context.Equipos.AsNoTracking().ToListAsync();
            return View("Admin/MostrarEquiposAdmin", equipos);
        }

        // =========================================================
        // PARA EMPLEADO: MOSTRAR EQUIPOS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> MostrarEquipos()
        {
            var equipos = await _context.Equipos.AsNoTracking().ToListAsync();
            return View("MostrarEquipos", equipos);
        }

        // =========================================================
        // AGREGAR EQUIPO
        // =========================================================
        [HttpGet]
        public IActionResult Agregar()
        {
            return View(new Equipo());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(Equipo model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Verifica los datos ingresados.";
                return View(model);
            }

            model.Estado = "disponible";
            model.FechaRegistro = DateTime.Now;
            model.FechaModificacion = DateTime.Now;

            _context.Equipos.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Equipo agregado correctamente.";

            // Redirigir según rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) &&
                rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarEquiposAdmin));
            }

            return RedirectToAction(nameof(MostrarEquipos));
        }

        // =========================================================
        // EDITAR EQUIPO
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null) return NotFound();
            return View(equipo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Equipo model)
        {
            if (!ModelState.IsValid) return View(model);

            var equipoExistente = await _context.Equipos.FindAsync(id);
            if (equipoExistente == null)
            {
                // Redirigir según rol
                var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(rolUsuario) &&
                    rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(MostrarEquiposAdmin));
                }
                return RedirectToAction(nameof(MostrarEquipos));
            }

            equipoExistente.NombreEquipo = model.NombreEquipo;
            equipoExistente.Descripcion = model.Descripcion;
            equipoExistente.Estado = model.Estado;
            
            if (model.Estado == "agotado")
            {
                equipoExistente.CantidadStock = 0;
            }
            
            equipoExistente.FechaModificacion = DateTime.Now;

            _context.Equipos.Update(equipoExistente);
            await _context.SaveChangesAsync();

            // Redirigir según rol
            var rol = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rol) && rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(MostrarEquiposAdmin));

            return RedirectToAction(nameof(MostrarEquipos));
        }

        // =========================================================
        // AGOTAR EQUIPO (solo empleado)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Agotar(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null) return NotFound();

            equipo.Estado = "agotado";
            equipo.CantidadStock = 0;
            equipo.FechaModificacion = DateTime.Now;

            _context.Equipos.Update(equipo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MostrarEquipos));
        }

        // =========================================================
        // ELIMINAR EQUIPO (solo admin)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int equipoId)
        {
            var equipo = await _context.Equipos.FindAsync(equipoId);
            if (equipo != null)
            {
                _context.Equipos.Remove(equipo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Equipo eliminado correctamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Equipo no encontrado.";
            }

            return RedirectToAction(nameof(MostrarEquiposAdmin));
        }

        // =========================================================
        // AUMENTAR STOCK
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> AumentarStock()
        {
            // Mostrar todos los equipos (disponibles, agotados o pendientes)
            var equipos = await _context.Equipos
                .Where(e => e.Estado == "disponible"
                         || e.Estado == "agotado"
                         || e.Estado == "pendiente")
                .OrderBy(e => e.NombreEquipo)
                .ToListAsync();

            return View(equipos);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AumentarStock(int equipoId, int cantidad)
        {
            var equipo = await _context.Equipos.FindAsync(equipoId);
            if (equipo != null)
            {
                equipo.CantidadStock += cantidad;

                // Si estaba agotado o pendiente, vuelve a disponible
                if ((equipo.Estado == "agotado" || equipo.Estado == "pendiente") && equipo.CantidadStock > 0)
                {
                    equipo.Estado = "disponible";
                }

                equipo.FechaModificacion = DateTime.Now;
                _context.Equipos.Update(equipo);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Stock actualizado correctamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Equipo no encontrado.";
            }

            // Redirigir según rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) &&
                rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarEquiposAdmin));
            }

            return RedirectToAction(nameof(MostrarEquipos));
        }

        // =========================================================
        // REDUCIR STOCK
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> BajarStock()
        {
            // Mostrar todos los equipos disponibles o con stock
            var equipos = await _context.Equipos
                .Where(e => e.Estado == "disponible" || e.Estado == "pendiente")
                .OrderBy(e => e.NombreEquipo)
                .ToListAsync();

            return View(equipos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BajarStock(int equipoId, int cantidad)
        {
            var equipo = await _context.Equipos.FindAsync(equipoId);
            if (equipo != null)
            {
                // Reducir la cantidad del stock
                equipo.CantidadStock -= cantidad;

                // Si el stock llega a cero, marcar como agotado
                if (equipo.CantidadStock <= 0)
                {
                    equipo.Estado = "agotado";
                }

                equipo.FechaModificacion = DateTime.Now;
                _context.Equipos.Update(equipo);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Stock reducido correctamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Equipo no encontrado.";
            }

            // Redirigir según rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarEquiposAdmin));
            }

            return RedirectToAction(nameof(MostrarEquipos));
        }

    }
}
