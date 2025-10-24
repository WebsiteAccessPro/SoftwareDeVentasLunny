using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
                // 👇 Usa el mismo método para recargar combos correctamente
                CargarCombos();
                ViewBag.Debug = "Datos inválidos. Verifica los campos.";
                return View(contrato);
            }

            // Calcular fecha de fin
            contrato.FechaFin = contrato.FechaInicio.AddMonths(mesesDuracion);

            // Guardar
            _context.Contratos.Add(contrato);
            await _context.SaveChangesAsync();

            return RedirectToAction("MostrarContratos");
        }

        public async Task<IActionResult> MostrarContratos()
        {
            var contratos = await _context.Contratos
                .Include(c => c.Cliente)
                .Include(c => c.PlanServicio)
                .Include(c => c.Empleado)
                .ToListAsync();

            return View(contratos);
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
            return RedirectToAction(nameof(MostrarContratos));
        }
    }
}
