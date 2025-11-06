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
        public async Task<IActionResult> AgregarContrato(Contrato contrato, int mesesDuracion)
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

            return View(contrato);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarContrato(int id, Contrato contrato, int mesesDuracion)
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

                if (contrato.ContratoEquipos != null && contrato.ContratoEquipos.Any())
                {
                    TempData["MensajeError"] = "No se puede eliminar el contrato porque tiene equipos asignados.";
                }
                else
                {
                    _context.Contratos.Remove(contrato);
                    await _context.SaveChangesAsync();
                    TempData["Mensaje"] = "Contrato eliminado correctamente.";
                }
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
