using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoFinalCalidad.Controllers
{
    public class ClienteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ClienteController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Mostrar lista de clientes
        public IActionResult MostrarClientes()
        {
            var clientes = _context.Clientes.ToList();
            return View(clientes);
        }

        // GET: Mostrar formulario registro
        [HttpGet]
        public IActionResult AgregarCliente()
        {
            return View(new Cliente());
        }

        // POST: Registrar cliente y usuario Identity
        [HttpPost]
        public async Task<IActionResult> AgregarCliente(Cliente model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Validar DNI único
            if (_context.Clientes.Any(c => c.Dni == model.Dni))
            {
                ModelState.AddModelError("Dni", "El DNI ya está registrado.");
                return View(model);
            }

            // Validar correo único en Identity
            if (await _userManager.FindByEmailAsync(model.Correo) != null)
            {
                ModelState.AddModelError("Correo", "El correo ya está en uso.");
                return View(model);
            }

            // Crear usuario en Identity
            var user = new IdentityUser
            {
                UserName = model.Correo,
                Email = model.Correo
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                model.Estado = "activo";
                model.FechaRegistro = DateTime.Now;

                _context.Clientes.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("MostrarClientes");
            }

            // Mostrar errores de Identity
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: Editar cliente
        public IActionResult EditarCliente(int id)
        {
            var cliente = _context.Clientes.Find(id);
            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        // POST: Editar cliente
        [HttpPost]
        public IActionResult EditarCliente(Cliente model)
        {
            var cli = _context.Clientes.Find(model.ClienteId);
            if (cli != null)
            {
                cli.Nombres = model.Nombres;
                cli.Direccion = model.Direccion;
                cli.Correo = model.Correo;
                cli.Telefono = model.Telefono;
                cli.Estado = model.Estado;

                _context.SaveChanges();
            }

            return RedirectToAction("MostrarClientes");
        }
    }
}
