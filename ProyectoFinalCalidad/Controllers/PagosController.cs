using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProyectoFinalCalidad.Controllers
{
    public class PagosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PagosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================
        // GET: /Pagos/BuscarPorDni
        // ==============================
        [HttpGet]
        public async Task<IActionResult> BuscarPorDni(string dni, bool? asAdmin)
        {
            if (User.IsInRole("Administrador") && (!asAdmin.HasValue || asAdmin != true) && string.IsNullOrEmpty(dni))
            {
                return RedirectToAction(nameof(MostrarDatosPagoAdmin));
            }

            if (string.IsNullOrEmpty(dni))
            {
                return View();
            }

            // Buscar cliente por DNI
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Dni == dni);
            if (cliente == null)
            {
                ViewBag.Mensaje = "No se encontró ningún cliente con el DNI ingresado.";
                return View();
            }

            // Validar estado del cliente
            if (cliente.Estado?.ToLower() != "activo")
            {
                ViewBag.Mensaje = "El cliente está inhabilitado y no puede realizar pagos.";
                return View();
            }

            // Buscar contrato activo
            var contrato = await _context.Contratos
                .Include(c => c.PlanServicio)
                    .ThenInclude(ps => ps.ZonaCobertura)
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.ClienteId == cliente.ClienteId && c.Estado == "activo");

            if (contrato == null)
            {
                ViewBag.Mensaje = "No se encontró un contrato activo para este cliente.";
                return View();
            }

            // Generar pago pendiente automáticamente si no existe
            await GenerarPagoAutomaticoInterno(contrato.Id);

            // Mostrar la vista con los datos del cliente (consulta de pagos)
            return RedirectToAction(nameof(MostrarDatosPago), new { dni = cliente.Dni });
        }

        // ==============================
        // POST: /Pagos/BuscarPorDni
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BuscarPorDni(string dni)
        {
            if (string.IsNullOrEmpty(dni))
            {
                ViewBag.Mensaje = "Por favor, ingrese un DNI válido.";
                return View();
            }

            // Usar PRG (Post/Redirect/Get) para evitar reenvíos
            return RedirectToAction(nameof(BuscarPorDni), new { dni });
        }

        // ======================================
        // GENERAR PAGO AUTOMÁTICO (Interno)
        // ======================================
        private async Task GenerarPagoAutomaticoInterno(int contratoId)
        {
            var contrato = await _context.Contratos
                .Include(c => c.PlanServicio)
                .FirstOrDefaultAsync(c => c.Id == contratoId);

            if (contrato == null) return;

            // Si ya hay un pago pendiente, no generar otro
            bool existePagoPendiente = await _context.Pagos
                .AnyAsync(p => p.ContratoId == contratoId && p.EstadoPago == "pendiente");
            if (existePagoPendiente) return;

            var ultimoPago = await _context.Pagos
                .Where(p => p.ContratoId == contratoId)
                .OrderByDescending(p => p.FechaDeVencimiento)
                .FirstOrDefaultAsync();

            DateTime fechaVencimiento = ultimoPago == null
                ? contrato.FechaInicio.AddMonths(1)
                : ultimoPago.FechaDeVencimiento.AddMonths(1);

            if (fechaVencimiento > contrato.FechaFin)
                return;

            var nuevoPago = new Pago
            {
                ContratoId = contratoId,
                Monto = contrato.PlanServicio.PrecioMensual,
                EstadoPago = "pendiente",
                FechaDeVencimiento = fechaVencimiento,
                FechaPago = null,
                MetodoPago = "Por especificar"
            };

            _context.Pagos.Add(nuevoPago);
            await _context.SaveChangesAsync();
        }

        // ======================================
        // MOSTRAR DATOS DE PAGO (Cliente)
        // ======================================
        [HttpGet]
        public async Task<IActionResult> MostrarDatosPago(string dni)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Dni == dni);
            if (cliente == null)
                return NotFound();

            var contrato = await _context.Contratos
                .Include(c => c.PlanServicio)
                    .ThenInclude(ps => ps.ZonaCobertura)
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.ClienteId == cliente.ClienteId && c.Estado == "activo");

            if (contrato == null)
                return NotFound();

            var pagos = await _context.Pagos
                .Where(p => p.ContratoId == contrato.Id)
                .OrderBy(p => p.FechaDeVencimiento)
                .ToListAsync();

            var viewModel = new PagoViewModel
            {
                Cliente = cliente,
                Contrato = contrato,
                Pagos = pagos
            };

            return View(viewModel);
        }

        // ======================================
        // ADMINISTRADOR - LISTA COMPLETA DE PAGOS
        // ======================================
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> MostrarDatosPagoAdmin()
        {
            var pagos = await _context.Pagos
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.Cliente)
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.PlanServicio)
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.Empleado)
                .OrderByDescending(p => p.FechaDeVencimiento)
                .ToListAsync();

            return View("Admin/MostrarDatosPagoAdmin", pagos);
        }

        // ======================================
        // CHECKOUT DE PAGO
        // ======================================
        [HttpGet]
        public IActionResult Checkout(int pagoId)
        {
            var pago = _context.Pagos
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.PlanServicio)
                .FirstOrDefault(p => p.PagoId == pagoId);

            if (pago == null)
                return NotFound();

            return View(pago);
        }

        // ======================================
        // PROCESAR PAGO
        // ======================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(int pagoId, string metodoPago)
        {
            var pago = await _context.Pagos
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.Cliente)
                .FirstOrDefaultAsync(p => p.PagoId == pagoId);

            if (pago == null || pago.Contrato?.Cliente == null)
                return NotFound();

            pago.EstadoPago = "pagado";
            pago.MetodoPago = metodoPago;
            pago.FechaPago = DateTime.Now;

            await _context.SaveChangesAsync();
            await GenerarPagoAutomaticoInterno(pago.ContratoId);

            TempData["MensajeExito"] = "¡Pago exitoso!";
            TempData["ContratoId"] = pago.ContratoId;
            TempData["DniCliente"] = pago.Contrato.Cliente.Dni;

            return RedirectToAction(nameof(ConfirmacionPago));
        }

        // ======================================
        // CONFIRMACIÓN DE PAGO
        // ======================================
        [HttpGet]
        public async Task<IActionResult> ConfirmacionPago()
        {
            if (TempData["MensajeExito"] == null)
                return RedirectToAction(nameof(BuscarPorDni));

            var contratoId = (int)TempData["ContratoId"];
            var contrato = await _context.Contratos
                .Include(c => c.PlanServicio)
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.Id == contratoId);

            ViewBag.DniCliente = contrato.Cliente.Dni;
            return View(contrato);
        }

        // ======================================
        // ELIMINAR PAGO (solo admin)
        // ======================================
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int pagoId)
        {
            try
            {
                var pago = await _context.Pagos.FindAsync(pagoId);
                if (pago == null)
                {
                    TempData["ErrorMessage"] = "Pago no encontrado.";
                    return RedirectToAction(nameof(MostrarDatosPagoAdmin));
                }

                // Los administradores pueden eliminar cualquier pago, incluso los procesados
                _context.Pagos.Remove(pago);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Pago eliminado correctamente.";
                return RedirectToAction(nameof(MostrarDatosPagoAdmin));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar el pago: " + ex.Message;
                return RedirectToAction(nameof(MostrarDatosPagoAdmin));
            }
        }
    }
}
