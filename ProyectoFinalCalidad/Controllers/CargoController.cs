using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoFinalCalidad.Controllers
{
    public class CargoController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CargoController(ApplicationDbContext context) => _context = context;

    
        public async Task<IActionResult> MostrarCargos()
        {
            var cargos = await _context.Cargos.ToListAsync();
            return View(cargos);
        }

     
        public IActionResult Agregar()
        {
            return View(new Cargo());
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(Cargo model)
        {
            if (await _context.Cargos.AnyAsync(c => c.titulo_cargo == model.titulo_cargo))
                ModelState.AddModelError(nameof(model.titulo_cargo), "El título ya existe.");
            if (!ModelState.IsValid)
                return View(model);

            _context.Cargos.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MostrarCargos));
        }

      
        public async Task<IActionResult> Editar(int id)
        {
            var cargo = await _context.Cargos.FindAsync(id);
            if (cargo == null) return NotFound();
            return View(cargo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Cargo model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cargo = await _context.Cargos.FindAsync(model.cargo_id);
            if (cargo == null) return NotFound();

            cargo.titulo_cargo = model.titulo_cargo;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MostrarCargos));
        }
    }
}
