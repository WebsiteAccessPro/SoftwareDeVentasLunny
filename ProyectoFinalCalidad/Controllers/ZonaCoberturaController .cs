using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;

public class ZonaCoberturaController : Controller
{

    private readonly ApplicationDbContext _context;

    public ZonaCoberturaController(ApplicationDbContext context)
    {
        _context = context;
    }


    // GET: ZonaCobertura/Agregar
    [HttpGet]
    public IActionResult Agregar()
    {
        return View();
    }

    // POST: ZonaCobertura/Agregar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Agregar(ZonaCobertura zona)
    {
        if (ModelState.IsValid)
        {
            _context.ZonaCoberturas.Add(zona);
            _context.SaveChanges();
            return RedirectToAction("Index"); // O el nombre de tu acción de listado
        }
        return View(zona);
    }

    // GET: ZonaCobertura/Editar/5
    [HttpGet]
    public IActionResult Editar(int id)
    {
        var zona = _context.ZonaCoberturas.FirstOrDefault(z => z.Id == id);

        if (zona == null)
        {
            return NotFound();
        }

        return View(zona);
    }

    // POST: ZonaCobertura/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, ZonaCobertura zonaActualizada)
    {
        if (id != zonaActualizada.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(zonaActualizada);
        }

        var zonaExistente = _context.ZonaCoberturas.FirstOrDefault(z => z.Id == id);
        if (zonaExistente == null)
        {
            return NotFound();
        }

        // Actualizar campos
        zonaExistente.NombreZona = zonaActualizada.NombreZona;
        zonaExistente.Distrito = zonaActualizada.Distrito;
        zonaExistente.Descripcion = zonaActualizada.Descripcion;

        try
        {
            _context.Update(zonaExistente);
            _context.SaveChanges();
            return RedirectToAction("Index"); // O la acción/listado que uses
        }
        catch (Exception ex)
        {
            // Opcional: registrar error
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar los cambios.");
            return View(zonaActualizada);
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        var zona = _context.ZonaCoberturas.FirstOrDefault(z => z.Id == id);
        if (zona == null)
        {
            return NotFound();
        }

        _context.ZonaCoberturas.Remove(zona);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }


    public async Task<IActionResult> Index()
    {
        var zonas = await _context.ZonaCoberturas.ToListAsync();
        return View(zonas);
    }
}
