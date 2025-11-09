using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoFinalCalidad.Controllers
{
    public class PedidoInstalacionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IContratoEquipoService _contratoEquipoService;

        public PedidoInstalacionController(ApplicationDbContext context, IContratoEquipoService contratoEquipoService)
        {
            _context = context;
            _contratoEquipoService = contratoEquipoService;
        }

        // GET: /PedidoInstalacion/Index
        public async Task<IActionResult> Index(string estadoFiltro)
        {
            ViewBag.EstadoFiltro = estadoFiltro ?? "Todos";

            // Si el usuario es Administrador, redirigir a la vista de Admin
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarPedidoInstalacionAdmin), new { estadoFiltro });
            }

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

        // GET: /PedidoInstalacion/MostrarPedidoInstalacionAdmin
        public async Task<IActionResult> MostrarPedidoInstalacionAdmin(string estadoFiltro)
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

            // Forzar la vista dentro de la carpeta Admin
            return View("Admin/MostrarPedidoInstalacionAdmin", pedidos);
        }

        // GET: /PedidoInstalacion/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _context.PedidosInstalacion
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.Cliente)
                .Include(p => p.Empleado)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null)
                return NotFound();

            // Cargar el equipo secundario actual del contrato (última asignación con unidad específica)
            var equipoSecundario = await _context.ContratoEquipos
                .Include(ce => ce.Equipo)
                .Include(ce => ce.EquipoUnidad)
                .Where(ce => ce.ContratoId == pedido.ContratoId && ce.EquipoUnidadId != null)
                .OrderByDescending(ce => ce.FechaAsignacion)
                .FirstOrDefaultAsync();

            if (equipoSecundario != null)
            {
                ViewBag.EquipoSecundarioNombre = equipoSecundario.Equipo?.NombreEquipo;
                ViewBag.EquipoSecundarioCodigoUnidad = equipoSecundario.EquipoUnidad?.CodigoUnidad;
            }
            else
            {
                ViewBag.EquipoSecundarioNombre = null;
                ViewBag.EquipoSecundarioCodigoUnidad = null;
            }

            return View(pedido);
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
        public async Task<IActionResult> Create(PedidoInstalacion model, int? equipoUnidadId)
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

                    // Asignar unidad física si se seleccionó: inferir equipo por contrato
                    if (equipoUnidadId.HasValue)
                    {
                        try
                        {
                            var equipoId = await _context.ContratoEquipos
                                .Where(ce => ce.ContratoId == model.ContratoId)
                                .OrderBy(ce => ce.FechaAsignacion)
                                .Select(ce => ce.EquipoId)
                                .FirstOrDefaultAsync();

                            if (equipoId == 0)
                            {
                                TempData["ErrorMessage"] = "El contrato seleccionado no tiene equipo asociado.";
                            }
                            else
                            {
                                await _contratoEquipoService.AsignarUnidadEspecificaAsync(model.ContratoId, equipoId, equipoUnidadId.Value, "asignado");
                                TempData["SuccessMessage"] = "Unidad asignada al contrato exitosamente.";
                            }
                        }
                        catch (Exception exAsignacion)
                        {
                            TempData["ErrorMessage"] = $"No se pudo asignar la unidad: {exAsignacion.Message}";
                        }
                    }

                    // Redirigir según rol
                    var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                    if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction(nameof(MostrarPedidoInstalacionAdmin));
                    }
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

            // Determinar y mostrar el Jefe a Cargo según el rol actual
            string jefeACargo = "Sin cargo";
            if (User.IsInRole("Administrador"))
            {
                jefeACargo = "Administrador general";
            }
            else
            {
                var emailUsuario = User.Identity?.Name;
                if (!string.IsNullOrEmpty(emailUsuario))
                {
                    var empleadoActual = await _context.Empleados
                        .Include(e => e.Cargo)
                        .FirstOrDefaultAsync(e => e.correo.ToLower() == emailUsuario.ToLower());

                    jefeACargo = empleadoActual?.Cargo?.titulo_cargo ?? pedido.JefeACargo ?? "Sin cargo";
                }
            }

            // Asegurar que el modelo tenga el valor para mostrar en la vista
            pedido.JefeACargo = jefeACargo;

            CargarCombos(pedido.ContratoId, pedido.EmpleadoId);
            return View("Editar", pedido);
        }

        // POST: /PedidoInstalacion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PedidoInstalacion model, int? equipoUnidadId)
        {
            if (!ModelState.IsValid)
            {
                CargarCombos(model.ContratoId, model.EmpleadoId);
                return View("Editar", model);
            }

            var pedido = await _context.PedidosInstalacion.FindAsync(model.PedidoId);
            if (pedido == null) return NotFound();

            pedido.ContratoId = model.ContratoId;
            pedido.EmpleadoId = model.EmpleadoId;
            pedido.FechaInstalacion = model.FechaInstalacion;
            pedido.EstadoInstalacion = model.EstadoInstalacion;
            pedido.Observaciones = model.Observaciones;

            // Persistir el Jefe a Cargo de acuerdo al rol del usuario que edita
            string jefeACargo = "Sin cargo";
            if (User.IsInRole("Administrador"))
            {
                jefeACargo = "Administrador general";
            }
            else
            {
                var emailUsuario = User.Identity?.Name;
                if (!string.IsNullOrEmpty(emailUsuario))
                {
                    var empleadoActual = await _context.Empleados
                        .Include(e => e.Cargo)
                        .FirstOrDefaultAsync(e => e.correo.ToLower() == emailUsuario.ToLower());

                    jefeACargo = empleadoActual?.Cargo?.titulo_cargo ?? pedido.JefeACargo ?? "Sin cargo";
                }
            }
            pedido.JefeACargo = jefeACargo;

            await _context.SaveChangesAsync();

            // Si se proporcionó una unidad física, asignarla al contrato
            if (equipoUnidadId.HasValue && model.ContratoId > 0)
            {
                try
                {
                    // Inferir el equipo asociado al contrato (sin filtrar por estado)
                    var equipoId = await _context.ContratoEquipos
                        .Where(ce => ce.ContratoId == model.ContratoId)
                        .OrderBy(ce => ce.FechaAsignacion)
                        .Select(ce => ce.EquipoId)
                        .FirstOrDefaultAsync();

                    if (equipoId != 0)
                    {
                        await _contratoEquipoService.AsignarUnidadEspecificaAsync(model.ContratoId, equipoId, equipoUnidadId.Value, "asignado");
                        TempData["SuccessMessage"] = "Unidad física asignada al contrato.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "El contrato seleccionado no tiene equipo asociado.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }

            // Si la instalación se marcó como completada, redirigir a asignación de equipos
            if (!string.IsNullOrEmpty(pedido.EstadoInstalacion) && pedido.EstadoInstalacion.ToLower() == "completado")
            {
                return RedirectToAction("Asignar", "ContratoEquipo", new { contratoId = pedido.ContratoId });
            }

            // Redirigir según rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarPedidoInstalacionAdmin));
            }
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
            // Redirigir según rol
            var rolUsuario = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(rolUsuario) && rolUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MostrarPedidoInstalacionAdmin));
            }
            return RedirectToAction(nameof(Index));
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

        // Cargar unidades físicas disponibles de un equipo (para dropdown dependiente)
        [HttpGet]
        public async Task<IActionResult> UnidadesDisponibles(int equipoId)
        {
            // Asegurar que existen filas de unidades físicas para equipos antiguos
            var equipo = await _context.Equipos.FindAsync(equipoId);
            if (equipo == null)
                return Json(Array.Empty<object>());

            var totalUnidades = await _context.EquiposUnidades.CountAsync(u => u.EquipoId == equipoId);
            if (totalUnidades == 0 && equipo.CantidadStock > 0)
            {
                // Backfill: crear unidades físicas "disponible" según el stock actual
                var fecha = DateTime.Now;
                for (int i = 1; i <= equipo.CantidadStock; i++)
                {
                    var codigoUnidad = Helpers.CodigoEquipoHelper.GenerarCodigoUnidad(equipo.NombreEquipo, fecha, i);
                    _context.EquiposUnidades.Add(new Models.EquipoUnidad
                    {
                        EquipoId = equipo.EquipoId,
                        CodigoUnidad = codigoUnidad,
                        EstadoUnidad = "disponible",
                        FechaRegistro = DateTime.Now,
                        FechaModificacion = DateTime.Now
                    });
                }
                await _context.SaveChangesAsync();
            }

            var unidades = await _context.EquiposUnidades
                .Where(u => u.EquipoId == equipoId && u.EstadoUnidad != null && u.EstadoUnidad.Trim().ToLower() == "disponible")
                .OrderBy(u => u.CodigoUnidad)
                .Select(u => new { id = u.EquipoUnidadId, codigo = u.CodigoUnidad })
                .ToListAsync();

            return Json(unidades);
        }

    }
}
