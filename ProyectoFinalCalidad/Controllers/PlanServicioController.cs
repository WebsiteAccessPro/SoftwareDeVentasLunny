using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoFinalCalidad.Controllers
{
    public class PlanServicioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PlanServicioController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // MOSTRAR PLANES DE SERVICIO (SOLO ADMIN)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> MostrarPlanServicio()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!await _userManager.IsInRoleAsync(user, "Administrador")) return Forbid();

            var planesServicio = await _context.PlanServicios
                .Include(p => p.ZonaCobertura)
                .AsNoTracking()
                .ToListAsync();

            return View("Admin/MostrarPlanServicio", planesServicio);
        }

        // =========================================================
        // GET: AGREGAR PLAN DE SERVICIO
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> AgregarPlanServicio()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (!await _userManager.IsInRoleAsync(user, "Administrador")) return Forbid();

            ViewBag.ZonasCobertura = await _context.ZonaCoberturas
                .ToListAsync();

            return View("Admin/AgregarPlanServicio");
        }

        // =========================================================
        // POST: AGREGAR PLAN DE SERVICIO
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarPlanServicio(PlanServicio planServicio)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (!await _userManager.IsInRoleAsync(user, "Administrador")) return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Verifica los datos ingresados antes de continuar.";
                ViewBag.ZonasCobertura = await _context.ZonaCoberturas.ToListAsync();
                return View("Admin/AgregarPlanServicio", planServicio);
            }

            if (await _context.PlanServicios.AnyAsync(p => p.NombrePlan == planServicio.NombrePlan))
            {
                TempData["ErrorMessage"] = "Ya existe un plan de servicio con este nombre.";
                ViewBag.ZonasCobertura = await _context.ZonaCoberturas.ToListAsync();
                return View("Admin/AgregarPlanServicio", planServicio);
            }

            planServicio.Estado = "activo";
            _context.PlanServicios.Add(planServicio);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Plan de servicio agregado correctamente.";
            return RedirectToAction(nameof(MostrarPlanServicio));
        }

        // =========================================================
        // GET: EDITAR PLAN DE SERVICIO
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EditarPlanServicio(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (!await _userManager.IsInRoleAsync(user, "Administrador")) return Forbid();

            if (id == null) return NotFound();

            var planServicio = await _context.PlanServicios.FindAsync(id);
            if (planServicio == null) return NotFound();

            ViewBag.ZonasCobertura = await _context.ZonaCoberturas.ToListAsync();

            return View("Admin/EditarPlanServicio", planServicio);
        }

        // =========================================================
        // POST: EDITAR PLAN DE SERVICIO
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPlanServicio(int id, PlanServicio model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (!await _userManager.IsInRoleAsync(user, "Administrador")) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.ZonasCobertura = await _context.ZonaCoberturas.ToListAsync();
                return View("Admin/EditarPlanServicio", model);
            }

            var planExistente = await _context.PlanServicios.FindAsync(id);
            if (planExistente == null)
            {
                TempData["ErrorMessage"] = "Plan de servicio no encontrado.";
                return RedirectToAction(nameof(MostrarPlanServicio));
            }

            if (await _context.PlanServicios.AnyAsync(p => p.NombrePlan == model.NombrePlan && p.PlanId != id))
            {
                TempData["ErrorMessage"] = "Ya existe otro plan de servicio con este nombre.";
                ViewBag.ZonasCobertura = await _context.ZonaCoberturas.ToListAsync();
                return View("Admin/EditarPlanServicio", model);
            }

            planExistente.NombrePlan = model.NombrePlan;
            planExistente.TipoServicio = model.TipoServicio;
            planExistente.Velocidad = model.Velocidad;
            planExistente.Descripcion = model.Descripcion;
            planExistente.PrecioMensual = model.PrecioMensual;
            planExistente.Estado = model.Estado;
            planExistente.ZonaId = model.ZonaId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Plan de servicio actualizado correctamente.";
            return RedirectToAction(nameof(MostrarPlanServicio));
        }

        // =========================================================
        // DESHABILITAR PLAN DE SERVICIO
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeshabilitarPlanServicio(int planServicioId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (!await _userManager.IsInRoleAsync(user, "Administrador")) return Forbid();

            var plan = await _context.PlanServicios.FindAsync(planServicioId);
            if (plan == null)
            {
                TempData["ErrorMessage"] = "Plan de servicio no encontrado.";
                return RedirectToAction(nameof(MostrarPlanServicio));
            }

            plan.Estado = "inactivo";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Plan de servicio deshabilitado correctamente.";
            return RedirectToAction(nameof(MostrarPlanServicio));
        }

        // =========================================================
        // HABILITAR PLAN DE SERVICIO
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HabilitarPlanServicio(int planServicioId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (!await _userManager.IsInRoleAsync(user, "Administrador")) return Forbid();

            var plan = await _context.PlanServicios.FindAsync(planServicioId);
            if (plan == null)
            {
                TempData["ErrorMessage"] = "Plan de servicio no encontrado.";
                return RedirectToAction(nameof(MostrarPlanServicio));
            }

            plan.Estado = "activo";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Plan de servicio habilitado correctamente.";
            return RedirectToAction(nameof(MostrarPlanServicio));
        }

        // =========================================================
        // VERIFICAR SI EXISTE PLAN CON NOMBRE (AJAX)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> VerificarNombrePlan(string nombrePlan, int? planId)
        {
            var existe = await _context.PlanServicios
                .AnyAsync(p => p.NombrePlan == nombrePlan && (planId == null || p.PlanId != planId));

            return Json(new { existe });
        }
    }
}
