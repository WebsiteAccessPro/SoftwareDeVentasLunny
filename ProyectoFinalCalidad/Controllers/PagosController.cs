using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

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
    public IActionResult BuscarPorDni()
    {
        return View();
    }

    // ==============================
    // POST: /Pagos/BuscarPorDni
    // ==============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuscarPorDni(string dni)
    {
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Dni == dni);
        if (cliente == null)
        {
            ViewBag.Mensaje = "DNI no registrado.";
            return View();
        }

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

        // Generar pago automático antes de mostrar los datos
        await GenerarPagoAutomaticoInterno(contrato.Id);

        // Redirige a la acción GET limpia (evita reenvío de formulario)
        return RedirectToAction("MostrarDatosPago", new { dni = cliente.Dni });
    }

    // ==============================
    // Método interno (sin redirect)
    // ==============================
    private async Task GenerarPagoAutomaticoInterno(int contratoId)
    {
        var contrato = await _context.Contratos
            .Include(c => c.PlanServicio)
            .FirstOrDefaultAsync(c => c.Id == contratoId);

        if (contrato == null)
            return;

        bool existePagoPendiente = await _context.Pagos
            .AnyAsync(p => p.ContratoId == contratoId && p.EstadoPago == "pendiente");

        if (existePagoPendiente)
            return;

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

    // ==============================
    // GET: /Pagos/Checkout?pagoId=###
    // ==============================
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

    // ==============================
    // POST: /Pagos/ProcesarPago
    // ==============================
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

        TempData["MensajeExito"] = "¡Pago exitoso!";
        TempData["ContratoId"] = pago.ContratoId;
        TempData["DniCliente"] = pago.Contrato.Cliente.Dni;

        return RedirectToAction("ConfirmacionPago");
    }

    // ==============================
    // GET: /Pagos/ConfirmacionPago
    // ==============================
    [HttpGet]
    public async Task<IActionResult> ConfirmacionPago()
    {
        if (TempData["MensajeExito"] == null)
            return RedirectToAction("BuscarPorDni");

        var contratoId = (int)TempData["ContratoId"];
        var contrato = await _context.Contratos
            .Include(c => c.PlanServicio)
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == contratoId);

        ViewBag.DniCliente = contrato.Cliente.Dni;
        return View(contrato);
    }

    // ==============================
    // GET: /Pagos/MostrarDatosPago?dni=#######
    // ==============================
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
}
