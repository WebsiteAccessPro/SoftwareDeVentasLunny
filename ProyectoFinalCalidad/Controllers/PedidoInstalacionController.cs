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
    public class PedidoInstalacionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidoInstalacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /PedidoInstalacion/Index
        public async Task<IActionResult> Index(string estadoFiltro)
        {
            ViewBag.EstadoFiltro = estadoFiltro ?? "Todos";

            var query = _context.PedidosInstalacion
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.Cliente)
                .Include(p => p.Empleado)
                .AsQueryable();

            if (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro.ToLower() != "todos")
            {
                query = query.Where(p => p.EstadoInstalacion.ToLower() == estadoFiltro.ToLower());
            }

            var pedidos = await query.ToListAsync();

            return View(pedidos);
        }

        // GET: /PedidoInstalacion/Create
        public IActionResult Create()
        {
            CargarCombos(null, null);
            return View();
        }

        // POST: /PedidoInstalacion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PedidoInstalacion model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string jefeACargo = "Sin cargo";

                    if (User.IsInRole("Administrador"))
                    {
                        // Si el usuario es administrador, asignar como Administrador general
                        jefeACargo = "Administrador general";
                    }
                    else
                    {
                        // Si no es administrador, buscar el cargo en la tabla Empleados
                        var emailUsuario = User.Identity?.Name;
                        if (!string.IsNullOrEmpty(emailUsuario))
                        {
                            var empleadoActual = await _context.Empleados
                                .Include(e => e.Cargo)
                                .FirstOrDefaultAsync(e => e.correo.ToLower() == emailUsuario.ToLower());

                            if (empleadoActual != null)
                            {
                                jefeACargo = empleadoActual.Cargo?.titulo_cargo ?? "Sin cargo";
                            }
                            else
                            {
                                jefeACargo = "Empleado no encontrado";
                            }
                        }
                        else
                        {
                            jefeACargo = "Usuario no autenticado";
                        }
                    }

                    // Asignar valores al modelo
                    model.JefeACargo = jefeACargo;
                    model.EstadoInstalacion ??= "Pendiente";
                    model.FechaInstalacion ??= DateTime.Now;

                    _context.Add(model);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al crear pedido: {ex.Message}");
                    ModelState.AddModelError("", "Ocurrió un error al guardar el pedido.");
                }
            }

            // Si hay error, recargar combos
            CargarCombos(model.ContratoId, model.EmpleadoId);

            // Mostrar el cargo del jefe en la vista (solo si no es administrador)
            if (!User.IsInRole("Administrador"))
            {
                var correoUsuario = User.Identity?.Name ?? "";
                var empleadoActual2 = await _context.Empleados
                    .Include(e => e.Cargo)
                    .FirstOrDefaultAsync(e => e.correo.ToLower() == correoUsuario.ToLower());
                ViewBag.JefeACargo = empleadoActual2?.Cargo?.titulo_cargo ?? "";
            }
            else
            {
                ViewBag.JefeACargo = "Administrador general";
            }

            return View(model);
        }


        // GET: /PedidoInstalacion/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var pedido = await _context.PedidosInstalacion.FindAsync(id);
            if (pedido == null) return NotFound();

            CargarCombos(pedido.ContratoId, pedido.EmpleadoId);
            return View(pedido);
        }

        // POST: /PedidoInstalacion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PedidoInstalacion model)
        {
            if (!ModelState.IsValid)
            {
                CargarCombos(model.ContratoId, model.EmpleadoId);
                return View(model);
            }

            var pedido = await _context.PedidosInstalacion.FindAsync(model.PedidoId);
            if (pedido == null) return NotFound();

            pedido.ContratoId = model.ContratoId;
            pedido.EmpleadoId = model.EmpleadoId;
            pedido.FechaInstalacion = model.FechaInstalacion;
            pedido.EstadoInstalacion = model.EstadoInstalacion;
            pedido.Observaciones = model.Observaciones;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /PedidoInstalacion/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var pedido = await _context.PedidosInstalacion.FindAsync(id);
            if (pedido == null) return NotFound();

            pedido.EstadoInstalacion = "Cancelado";
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "El pedido fue cancelado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /PedidoInstalacion/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _context.PedidosInstalacion
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.Cliente)
                .Include(p => p.Empleado)
                    .ThenInclude(e => e.Cargo)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        // Método privado para combos (solo contratos y técnicos)
        private void CargarCombos(int? contratoId, int? tecnicoId)
        {
            // Contratos con nombre visible
            var contratos = _context.Contratos
                .Include(c => c.Cliente)
                .Select(c => new
                {
                    Id = c.Id,
                    NombreVisible = $"Contrato #{c.Id} - Cliente: {c.Cliente.Nombres}"
                }).ToList();

            // Técnicos activos
            var tecnicos = _context.Empleados
                .Include(e => e.Cargo)
                .Where(e => e.estado.ToLower() == "activo" &&
                            e.Cargo.titulo_cargo.ToLower() == "técnico de instalación")
                .Select(e => new
                {
                    empleado_id = e.empleado_id,
                    nombre = e.nombres
                }).ToList();

            ViewBag.Contratos = new SelectList(contratos, "Id", "NombreVisible", contratoId);
            ViewBag.Tecnicos = new SelectList(tecnicos, "empleado_id", "nombre", tecnicoId);
        }

    }
}
