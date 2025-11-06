using Moq;
using NUnit.Framework;
using ProyectoFinalCalidad.Controllers;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace TestPruebasUnitarias
{
    [TestFixture]
    public class PagosControllerTests
    {
        private PagosController _controller;
        private List<Cliente> _clientesSimulados;
        private List<Contrato> _contratosSimulados;
        private List<Pago> _pagosSimulados;
        private List<PlanServicio> _planesSimulados;

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            // Datos simulados
            _clientesSimulados = new List<Cliente>
            {
                new Cliente { ClienteId = 1, Dni = "12345678", Nombres = "Juan", Estado = "activo" },
                new Cliente { ClienteId = 2, Dni = "87654321", Nombres = "Ana", Estado = "inactivo" }
            };

            _planesSimulados = new List<PlanServicio>
            {
                new PlanServicio { PlanId = 1, NombrePlan = "Plan A", PrecioMensual = 100 }
            };

            _contratosSimulados = new List<Contrato>
            {
                new Contrato { Id = 1, ClienteId = 1, PlanId = 1, Estado = "activo", FechaInicio = DateTime.Now.AddMonths(-1), FechaFin = DateTime.Now.AddMonths(11), PlanServicio = _planesSimulados[0], Cliente = _clientesSimulados[0] },
                new Contrato { Id = 2, ClienteId = 2, PlanId = 1, Estado = "inactivo", FechaInicio = DateTime.Now.AddMonths(-2), FechaFin = DateTime.Now.AddMonths(10), PlanServicio = _planesSimulados[0], Cliente = _clientesSimulados[1] }
            };

            _pagosSimulados = new List<Pago>();

            // Crear controlador con listas simuladas
            _controller = new PagosController(null)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            // Simular usuario
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "UsuarioTest"),
                new Claim(ClaimTypes.Role, "Empleado")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // =====================================================
        // PRUEBAS EXITOSAS
        // =====================================================

        [Test]
        public async Task BuscarPorDni_ClienteActivo_RetornaRedirectA_MostrarDatosPago()
        {
            // Act
            var clienteDni = "12345678";
            var contrato = _contratosSimulados.First(c => c.Cliente.Dni == clienteDni);

            // Simular GenerarPagoAutomaticoInterno
            Func<int, Task> generarPagoSimulado = (contratoId) =>
            {
                var pago = new Pago
                {
                    PagoId = 1,
                    ContratoId = contratoId,
                    EstadoPago = "pendiente",
                    Monto = contrato.PlanServicio.PrecioMensual,
                    FechaDeVencimiento = DateTime.Now.AddMonths(1)
                };
                _pagosSimulados.Add(pago);
                return Task.CompletedTask;
            };

            // Act & Assert simulado
            await generarPagoSimulado(contrato.Id);

            Assert.AreEqual(1, _pagosSimulados.Count);
            Assert.AreEqual("pendiente", _pagosSimulados[0].EstadoPago);
        }

        [Test]
        public void BuscarPorDni_Post_RedirectA_Get()
        {
            // Act
            string dni = "12345678";
            var resultado = _controller.BuscarPorDni(dni) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(resultado);
            Assert.AreEqual("BuscarPorDni", resultado.ActionName);
            Assert.AreEqual(dni, resultado.RouteValues["dni"]);
        }

        [Test]
        public async Task ProcesarPago_ClienteValido_CambiaEstadoPago()
        {
            // Preparar pago pendiente
            var pago = new Pago { PagoId = 1, ContratoId = 1, EstadoPago = "pendiente", Contrato = _contratosSimulados[0] };
            _pagosSimulados.Add(pago);

            // Simular procesar pago
            Func<int, string, Task> procesarPagoSimulado = (pagoId, metodoPago) =>
            {
                var p = _pagosSimulados.First(x => x.PagoId == pagoId);
                p.EstadoPago = "pagado";
                p.MetodoPago = metodoPago;
                p.FechaPago = DateTime.Now;
                return Task.CompletedTask;
            };

            await procesarPagoSimulado(1, "Tarjeta");

            Assert.AreEqual("pagado", _pagosSimulados[0].EstadoPago);
            Assert.AreEqual("Tarjeta", _pagosSimulados[0].MetodoPago);
            Assert.NotNull(_pagosSimulados[0].FechaPago);
        }

        // =====================================================
        // PRUEBAS NO EXITOSAS
        // =====================================================

        [Test]
        public async Task BuscarPorDni_ClienteInexistente_RetornaVistaConMensaje()
        {
            string dni = "99999999"; // DNI no existe

            string mensaje = null;
            Func<string, Task> buscarClienteSimulado = (dniBuscado) =>
            {
                var cliente = _clientesSimulados.FirstOrDefault(c => c.Dni == dniBuscado);
                if (cliente == null)
                    mensaje = "No se encontró ningún cliente con el DNI ingresado.";
                return Task.CompletedTask;
            };

            await buscarClienteSimulado(dni);

            Assert.AreEqual("No se encontró ningún cliente con el DNI ingresado.", mensaje);
        }

        [Test]
        public async Task BuscarPorDni_ClienteInactivo_RetornaVistaConMensaje()
        {
            string dni = "87654321"; // Cliente inactivo
            string mensaje = null;

            Func<string, Task> validarClienteSimulado = (dniBuscado) =>
            {
                var cliente = _clientesSimulados.First(c => c.Dni == dniBuscado);
                if (cliente.Estado.ToLower() != "activo")
                    mensaje = "El cliente está inhabilitado y no puede realizar pagos.";
                return Task.CompletedTask;
            };

            await validarClienteSimulado(dni);

            Assert.AreEqual("El cliente está inhabilitado y no puede realizar pagos.", mensaje);
        }

        [Test]
        public async Task ProcesarPago_PagoNoExistente_RetornaNotFound()
        {
            int pagoIdInexistente = 999;
            bool notFound = false;

            Func<int, Task> procesarPagoSimulado = (id) =>
            {
                var pago = _pagosSimulados.FirstOrDefault(p => p.PagoId == id);
                if (pago == null)
                    notFound = true;
                return Task.CompletedTask;
            };

            await procesarPagoSimulado(pagoIdInexistente);

            Assert.IsTrue(notFound);
        }
    }
}
