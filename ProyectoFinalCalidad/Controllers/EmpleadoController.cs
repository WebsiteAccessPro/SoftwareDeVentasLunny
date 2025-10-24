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

        public async Task<IActionResult> MostrarEmpleados()
        {
            var empleados = await _context.Empleados
                .Include(e => e.Cargo)
                .AsNoTracking()
                .ToListAsync();

            return View(empleados);
        }

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

                _context.Empleados.Add(empleado);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Empleado agregado correctamente";
                return RedirectToAction(nameof(MostrarEmpleados));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al guardar: {ex.Message}";
                CargarCargosViewBag();
                return View("Agregar", empleado);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }

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
                {
                    empleadoExistente.password = NewPassword;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Empleado actualizado correctamente.";
                return RedirectToAction(nameof(MostrarEmpleados));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar: {ex.Message}";
                CargarCargosViewBag();
                return View("Editar", empleado);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarEditar(Equipo equipo)
        {
            if (!ModelState.IsValid)
                return View(equipo);

            if (equipo.EquipoId == 0)
            {
                // 🔹 Asigna la fecha actual automáticamente (hora Perú UTC-5)
                equipo.FechaRegistro = DateTime.UtcNow.AddHours(-5);
                _context.Equipos.Add(equipo);
            }
            else
            {
                // 🔹 Edición: mantenemos la fecha original
                var equipoExistente = await _context.Equipos.FindAsync(equipo.EquipoId);
                if (equipoExistente != null)
                {
                    equipoExistente.NombreEquipo = equipo.NombreEquipo;
                    equipoExistente.Descripcion = equipo.Descripcion;
                    equipoExistente.CantidadStock = equipo.CantidadStock;
                    equipoExistente.Estado = equipo.Estado;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Administrar");
        }


        [HttpGet]
        public async Task<IActionResult> InhabilitarEmpleado(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }

            empleado.estado = "inhabilitado";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Empleado inhabilitado correctamente";
            return RedirectToAction(nameof(MostrarEmpleados));
        }

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
