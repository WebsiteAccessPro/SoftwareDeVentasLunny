using Microsoft.AspNetCore.Mvc;

namespace ProyectoFinalCalidad.Controllers
{
    public class CuentaController : Controller
    {
        public IActionResult Inhabilitado(string tipo)
        {
            // Pasar tipo directamente a la vista
            ViewData["Tipo"] = tipo?.ToLower() ?? "";
            return View();
        }
    }
}
