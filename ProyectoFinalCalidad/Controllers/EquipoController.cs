using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;

public class EquipoController : Controller
{
    private readonly ApplicationDbContext _context;

    public EquipoController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var equipos = await _context.Equipos.ToListAsync();
        return View(equipos);
    }
    public async Task<IActionResult> Administrar()
    {
        var equipos = await _context.Equipos.ToListAsync();
        return View("Administrar", equipos); 
    }


    public IActionResult Agregar()
    {
        return View(new Equipo());
    }

    [HttpPost]
    public async Task<IActionResult> Agregar(Equipo model)
    {
        if (!ModelState.IsValid) return View(model);

        model.Estado = "disponible"; 
        _context.Equipos.Add(model);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Administrar));
    }

    public async Task<IActionResult> Editar(int id)
    {
        var equipo = await _context.Equipos.FindAsync(id);
        if (equipo == null) return NotFound();

        return View(equipo);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Equipo model)
    {
        if (!ModelState.IsValid) return View(model);

        var equipoExistente = await _context.Equipos.FindAsync(model.EquipoId);
        if (equipoExistente == null) return NotFound();

        equipoExistente.NombreEquipo = model.NombreEquipo;
        equipoExistente.Descripcion = model.Descripcion;
        equipoExistente.Estado = model.Estado;

        _context.Equipos.Update(equipoExistente);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Administrar));
    }


    public async Task<IActionResult> AumentarStock()
    {
        var activos = await _context.Equipos.Where(e => e.Estado == "disponible").ToListAsync();
        return View(activos);
    }

    [HttpPost]
    public async Task<IActionResult> AumentarStock(int equipoId, int cantidad)
    {
        var equipo = await _context.Equipos.FindAsync(equipoId);
        if (equipo != null && equipo.Estado == "disponible")
        {
            equipo.CantidadStock += cantidad;
            _context.Equipos.Update(equipo);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Administrar));
    }

    [HttpPost]
    public async Task<IActionResult> AgregarEditar(Equipo model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var existe = await _context.Equipos.FindAsync(model.EquipoId);
        if (existe == null)
        {
            model.Estado = "disponible";
            _context.Equipos.Add(model);
        }
        else
        {
            existe.NombreEquipo = model.NombreEquipo;
            existe.Descripcion = model.Descripcion;
            existe.Estado = model.Estado;
            _context.Equipos.Update(existe);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Administrar));
    }

}
