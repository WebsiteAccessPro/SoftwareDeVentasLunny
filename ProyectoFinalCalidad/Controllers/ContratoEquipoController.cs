using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoFinalCalidad.Controllers
{
    public class ContratoEquipoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IContratoEquipoService _service;

        public ContratoEquipoController(ApplicationDbContext context, IContratoEquipoService service)
        {
            _context = context;
            _service = service;
        }

       
        public async Task<IActionResult> Asignar(int? contratoId)
        {
            var contratos = await _context.Contratos.OrderBy(c => c.Id).ToListAsync();
            var equiposActivos = await _context.Equipos
                .Where(e => e.Estado == "activo")
                .ToListAsync();

            ViewBag.Contratos = new SelectList(contratos, "Id", "Id", contratoId);
            ViewBag.Equipos = new SelectList(equiposActivos, "EquipoId", "NombreEquipo");
            ViewBag.Estados = new SelectList(new[] { "asignado", "entregado", "mantenimiento" });

            return View();
        }

   
        [HttpPost]
        public async Task<IActionResult> Asignar(int contratoId, int equipoId, string estado)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Asignar));

            // Validar que exista al menos una unidad disponible
            var disponible = await _context.EquiposUnidades
                .AnyAsync(u => u.EquipoId == equipoId && u.EstadoUnidad == "disponible");

            if (!disponible)
            {
                TempData["ErrorMessage"] = "No hay unidades disponibles para el equipo seleccionado.";
                return RedirectToAction(nameof(Asignar));
            }

            try
            {
                await _service.AsignarContratoEquipoAsync(contratoId, equipoId, estado);
                TempData["SuccessMessage"] = "Equipo asignado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Asignar));
            }

            return RedirectToAction("Administrar", "Equipo");
        }


        public async Task<IActionResult> Index()
        {
            var asignaciones = await _service.ListarAsignacionesAsync();
            return View(asignaciones);
        }

        // Equipo principal asociado a un contrato (primer asignación registrada)
        [HttpGet]
        public async Task<IActionResult> EquipoPorContrato(int contratoId)
        {
            var equipoId = await _context.ContratoEquipos
                .Where(ce => ce.ContratoId == contratoId)
                .OrderBy(ce => ce.FechaAsignacion)
                .Select(ce => ce.EquipoId)
                .FirstOrDefaultAsync();

            if (equipoId == 0)
                return NotFound(new { message = "Contrato sin equipo asociado" });

            return Json(new { equipoId });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            try
            {
                await _service.CambiarEstadoAsignacionAsync(id, estado);
                TempData["SuccessMessage"] = "Estado actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Devolver(int id)
        {
            try
            {
                await _service.CambiarEstadoAsignacionAsync(id, "devuelto");
                TempData["SuccessMessage"] = "Equipo devuelto y stock actualizado.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }


    }
}
