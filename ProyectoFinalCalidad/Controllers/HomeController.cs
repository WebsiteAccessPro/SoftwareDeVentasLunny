using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProyectoFinalCalidad.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoFinalCalidad.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Privacy()
        {
            // Nombre del usuario o "Invitado" si no está autenticado
            string nombreUsuario = User.Identity.IsAuthenticated ? User.Identity.Name : "Invitado";
            ViewData["Usuario"] = nombreUsuario;

            // Valor de la cookie de autenticación
            var cookieValor = Request.Cookies[".AspNetCore.Identity.Application"];
            ViewData["CookieValor"] = cookieValor;

            // Claims del usuario
            var claims = User.Claims
                .Select(c => new KeyValuePair<string, string>(c.Type, c.Value))
                .ToList();
            ViewData["Claims"] = claims;

            // Roles del usuario (solo si hay usuario logueado)
            var user = await _userManager.GetUserAsync(User);
            IList<string> roles = new List<string>();
            if (user != null)
            {
                roles = await _userManager.GetRolesAsync(user);
            }
            ViewData["Roles"] = roles;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
