using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Services;

namespace ProyectoFinalCalidad.Controllers
{
    public class ContratoController : Controller
    {
        private readonly IContratoService _contratoService;
        private readonly ApplicationDbContext _context;

        public ContratoController(IContratoService contratoService, ApplicationDbContext context)
        {
            _contratoService = contratoService;
            _context = context;
        }

        private void CargarCombos()
        {
            ViewBag.Clientes = new SelectList(
                _context.Clientes
                    .Where(c => c.Estado.ToLower() == "activo")
                    .ToList(),
                "ClienteId",
                "Nombres"
            );

            ViewBag.Planes = new SelectList(
                _context.PlanServicios.ToList(),
                "PlanId",
                "NombrePlan"
            );

            ViewBag.Empleados = new SelectList(
                _context.Empleados
                    .Where(e => e.estado.ToLower() == "activo")
                    .ToList(),
                "empleado_id",
                "nombres"
            );

            // Equipos para seleccionar el equipo principal del contrato
            ViewBag.Equipos = new SelectList(
                _context.Equipos
                    .OrderBy(eq => eq.NombreEquipo)
                    .ToList(),
                "EquipoId",
                "NombreEquipo"
            );
        }


        public async Task<IActionResult> Index()
        {
            // Si el usuario es Administrador, redirigir a la vista de Admin
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarContratosAdmin));
            }

            var contratos = await _context.Contratos
                .Include(c => c.Cliente)
                .Include(c => c.PlanServicio)
                .Include(c => c.Empleado)
                .ToListAsync();

            return View(contratos);
        }

        [HttpGet]
        public IActionResult AgregarContrato()
        {
            CargarCombos();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarContrato(Contrato contrato, int mesesDuracion, int? equipoId)
        {
            if (!ModelState.IsValid)
            {
                // Usa el mismo método para recargar combos correctamente
                CargarCombos();
                ViewBag.Debug = "Datos inválidos. Verifica los campos.";
                return View(contrato);
            }

            // Calcular fecha de fin
            contrato.FechaFin = contrato.FechaInicio.AddMonths(mesesDuracion);

            // Guardar
            _context.Contratos.Add(contrato);
            await _context.SaveChangesAsync();

            // Registrar equipo principal del contrato (sin seleccionar unidad física aún)
            if (equipoId.HasValue)
            {
                var contratoEquipo = new ContratoEquipo
                {
                    ContratoId = contrato.Id,
                    EquipoId = equipoId.Value,
                    Estado = "activo"
                };
                _context.ContratoEquipos.Add(contratoEquipo);
                await _context.SaveChangesAsync();
            }

            // Redirigir según rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarContratosAdmin));
            }
            return RedirectToAction(nameof(MostrarContratos));
        }

        public async Task<IActionResult> MostrarContratos()
        {
            // Si el usuario es Administrador, redirigir a la vista de Admin
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarContratosAdmin));
            }

            var contratos = await _context.Contratos
                .Include(c => c.Cliente)
                .Include(c => c.PlanServicio)
                .Include(c => c.Empleado)
                .ToListAsync();

            return View(contratos);
        }

        [HttpGet]
        public async Task<IActionResult> EditarContrato(int id)
        {
            var contrato = await _context.Contratos
                .Include(c => c.Cliente)
                .Include(c => c.PlanServicio)
                .Include(c => c.Empleado)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contrato == null) return NotFound();

            CargarCombos();

            // Precalcular la duración en meses basada en las fechas actuales
            var mesesDuracion = Math.Max(0, ((contrato.FechaFin.Year - contrato.FechaInicio.Year) * 12)
                                               + (contrato.FechaFin.Month - contrato.FechaInicio.Month));
            ViewBag.MesesDuracion = mesesDuracion;

            // Seleccionar el equipo principal actual para precargar en el combo
            var equipoPrincipalId = await _context.ContratoEquipos
                .Where(ce => ce.ContratoId == id && ce.EquipoUnidadId == null)
                .OrderBy(ce => ce.FechaAsignacion)
                .Select(ce => ce.EquipoId)
                .FirstOrDefaultAsync();

            if (equipoPrincipalId == 0)
            {
                // Si no hay asignación sin unidad, tomar la primera asignación
                equipoPrincipalId = await _context.ContratoEquipos
                    .Where(ce => ce.ContratoId == id)
                    .OrderBy(ce => ce.FechaAsignacion)
                    .Select(ce => ce.EquipoId)
                    .FirstOrDefaultAsync();
            }

            // Reemplazar ViewBag.Equipos para tener seleccionado el equipo actual
            ViewBag.Equipos = new SelectList(
                _context.Equipos
                    .OrderBy(eq => eq.NombreEquipo)
                    .ToList(),
                "EquipoId",
                "NombreEquipo",
                equipoPrincipalId
            );

            return View(contrato);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarContrato(int id, Contrato contrato, int mesesDuracion, int? equipoId)
        {
            if (id != contrato.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                CargarCombos();
                ViewBag.MesesDuracion = mesesDuracion;
                return View(contrato);
            }

            var contratoDb = await _context.Contratos.FindAsync(id);
            if (contratoDb == null) return NotFound();

            // Actualizar campos editables
            contratoDb.ClienteId = contrato.ClienteId;
            contratoDb.PlanId = contrato.PlanId;
            contratoDb.EmpleadoId = contrato.EmpleadoId;
            contratoDb.FechaInicio = contrato.FechaInicio;
            contratoDb.FechaFin = contrato.FechaInicio.AddMonths(mesesDuracion);

            _context.Update(contratoDb);
            await _context.SaveChangesAsync();

            // Actualizar el equipo principal del contrato si se proporcionó
            if (equipoId.HasValue)
            {
                var principal = await _context.ContratoEquipos
                    .Where(ce => ce.ContratoId == contratoDb.Id && ce.EquipoUnidadId == null)
                    .OrderBy(ce => ce.FechaAsignacion)
                    .FirstOrDefaultAsync();

                if (principal != null)
                {
                    principal.EquipoId = equipoId.Value;
                    principal.Estado = string.IsNullOrWhiteSpace(principal.Estado) ? "activo" : principal.Estado;
                    _context.ContratoEquipos.Update(principal);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var nuevo = new ContratoEquipo
                    {
                        ContratoId = contratoDb.Id,
                        EquipoId = equipoId.Value,
                        Estado = "activo"
                    };
                    _context.ContratoEquipos.Add(nuevo);
                    await _context.SaveChangesAsync();
                }
            }

            // Redirigir según rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarContratosAdmin));
            }
            return RedirectToAction(nameof(MostrarContratos));
        }

        // Vista de administración: Mostrar Contratos
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> MostrarContratosAdmin()
        {
            var contratos = await _context.Contratos
                .Include(c => c.Cliente)
                .Include(c => c.PlanServicio)
                .Include(c => c.Empleado)
                .ToListAsync();

            // Forzar la vista dentro de la carpeta Admin
            return View("Admin/MostrarContratosAdmin", contratos);
        }

        [HttpPost]
        public async Task<IActionResult> Deshabilitar(int id)
        {
            var contrato = await _context.Contratos.FindAsync(id);
            if (contrato == null) return NotFound();

            contrato.Estado = "inactivo";
            _context.Update(contrato);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Contrato deshabilitado correctamente.";
            // Redirigir según rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarContratosAdmin));
            }
            return RedirectToAction(nameof(MostrarContratos));
        }

        [HttpPost]
        public async Task<IActionResult> Habilitar(int id)
        {
            var contrato = await _context.Contratos.FindAsync(id);
            if (contrato == null) return NotFound();

            contrato.Estado = "activo";
            _context.Update(contrato);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Contrato habilitado correctamente.";
            // Redirigir según rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarContratosAdmin));
            }
            return RedirectToAction(nameof(MostrarContratos));
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var contrato = await _context.Contratos
                .Include(c => c.ContratoEquipos)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contrato == null) return NotFound();

            try
            {
                // Bloquear eliminación si tiene pagos asociados
                var tienePagos = await _context.Pagos.AnyAsync(p => p.ContratoId == id);
                if (tienePagos)
                {
                    TempData["MensajeError"] = "No se puede eliminar el contrato porque tiene pagos asociados.";
                    // Redirigir según rol (Admin)
                    var rolUsuarioBloqueo = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                    if (!string.IsNullOrEmpty(rolUsuarioBloqueo) && rolUsuarioBloqueo.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction(nameof(MostrarContratosAdmin));
                    }
                    return RedirectToAction(nameof(MostrarContratos));
                }
                // Liberar unidades y eliminar asignaciones asociadas
                if (contrato.ContratoEquipos != null && contrato.ContratoEquipos.Any())
                {
                    foreach (var ce in contrato.ContratoEquipos)
                    {
                        // Si hay unidad física asignada, devolverla a disponible y recuperar stock
                        if (ce.EquipoUnidadId.HasValue)
                        {
                            var unidad = await _context.EquiposUnidades.FindAsync(ce.EquipoUnidadId.Value);
                            if (unidad != null)
                            {
                                unidad.EstadoUnidad = "disponible";
                                unidad.FechaModificacion = DateTime.Now;
                                _context.EquiposUnidades.Update(unidad);
                            }

                            var equipo = await _context.Equipos.FindAsync(ce.EquipoId);
                            if (equipo != null)
                            {
                                equipo.CantidadStock += 1;
                                equipo.FechaModificacion = DateTime.Now;
                                _context.Equipos.Update(equipo);
                            }
                        }
                    }

                    // Eliminar asignaciones del contrato
                    _context.ContratoEquipos.RemoveRange(contrato.ContratoEquipos);
                }

                // Eliminar el contrato
                _context.Contratos.Remove(contrato);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Contrato eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al eliminar el contrato: {ex.Message}";
            }

            // Redirigir según rol (Admin)
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarContratosAdmin));
            }
            return RedirectToAction(nameof(MostrarContratos));
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> TienePagos(int id)
        {
            var count = await _context.Pagos.CountAsync(p => p.ContratoId == id);
            return Json(new { hasPayments = count > 0, count });
        }
    }
}
