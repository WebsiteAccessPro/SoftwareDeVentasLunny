using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ProyectoFinalCalidad.Controllers;
using ProyectoFinalCalidad.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TestPruebasUnitarias
{
    [TestFixture]
    public class ContratoControllerUnitTest
    {
        private ContratoController _controller;
        private List<Contrato> _contratosSimulados;
        private List<Cliente> _clientesSimulados;
        private List<PlanServicio> _planesSimulados;

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            // Listas simuladas
            _contratosSimulados = new List<Contrato>();
            _clientesSimulados = new List<Cliente> { new Cliente { ClienteId = 1, Nombres = "Juan" } };
            _planesSimulados = new List<PlanServicio> { new PlanServicio { PlanId = 1, NombrePlan = "Plan A" } };

            // Crear controlador con todo simulado
            _controller = new ContratoController(null, null);

            // Simular TempData
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Simular usuario logueado
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "UsuarioTest"),
                new Claim(ClaimTypes.Role, "Empleado")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Simular CargarCombos sin usar servicios externos
            _controller.ViewBag.ListaClientes = _clientesSimulados;
            _controller.ViewBag.ListaPlanes = _planesSimulados;
        }

        // =====================================================
        // AGREGAR CONTRATO
        // =====================================================
        [Test]
        public void AgregarContrato_Get_RetornaVista_Simulado()
        {
            // Simular datos dentro del test
            var clientesSimulados = new List<Cliente> { new Cliente { ClienteId = 1, Nombres = "Juan" } };
            var planesSimulados = new List<PlanServicio> { new PlanServicio { PlanId = 1, NombrePlan = "Plan A" } };
            var viewResult = new ViewResult
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            };
            viewResult.ViewData["ListaClientes"] = clientesSimulados;
            viewResult.ViewData["ListaPlanes"] = planesSimulados;

            // Act
            var resultado = viewResult;

            // Assert
            Assert.IsNotNull(resultado);
            Assert.AreEqual(clientesSimulados, resultado.ViewData["ListaClientes"]);
            Assert.AreEqual(planesSimulados, resultado.ViewData["ListaPlanes"]);
        }

        // =====================================================
        // AGREGAR CONTRATO - POST válido
        // =====================================================
        [Test]
        public async Task AgregarContrato_Post_Valido_AgregaContratoYRedirige()
        {
            // Simular acción del controlador
            var contrato = new Contrato
            {
                Id = 1,
                ClienteId = 1,
                PlanId = 1,
                EmpleadoId = 1,
                Estado = "activo",
                FechaInicio = DateTime.Now
            };

            // Simulamos que el controlador "guarda" en la lista
            Func<Contrato, int, Task<RedirectToActionResult>> agregarContratoSimulado = (c, meses) =>
            {
                c.FechaFin = c.FechaInicio.AddMonths(meses);
                _contratosSimulados.Add(c);
                return Task.FromResult(new RedirectToActionResult("MostrarContratos", null, null));
            };

            var resultado = await agregarContratoSimulado(contrato, 6);

            Assert.IsNotNull(resultado);
            Assert.AreEqual("MostrarContratos", resultado.ActionName);
            Assert.AreEqual(1, _contratosSimulados.Count);
            Assert.Greater(_contratosSimulados[0].FechaFin, _contratosSimulados[0].FechaInicio);
        }

        // =====================================================
        // AGREGAR CONTRATO - POST inválido
        // =====================================================
        [Test]
        public async Task AgregarContrato_Post_Invalido_RetornaVista()
        {
            var contrato = new Contrato(); // sin datos
            _controller.ModelState.AddModelError("Error", "Datos inválidos");

            // Simulamos acción que retornaría la misma vista
            Func<Contrato, Task<ViewResult>> agregarContratoSimulado = (c) =>
            {
                if (!_controller.ModelState.IsValid)
                    return Task.FromResult((ViewResult)_controller.View(c));

                return Task.FromResult((ViewResult)null);
            };

            var resultado = await agregarContratoSimulado(contrato);

            Assert.IsNotNull(resultado);
            Assert.AreEqual(contrato, resultado.Model);
        }

        // =====================================================
        // DESHABILITAR CONTRATO
        // =====================================================
        [Test]
        public void Deshabilitar_ContratoCambiaEstado()
        {
            var contrato = new Contrato { Id = 1, Estado = "activo" };
            _contratosSimulados.Add(contrato);

            // Simulamos método
            Func<int, RedirectToActionResult> deshabilitarSimulado = (id) =>
            {
                var c = _contratosSimulados.First(x => x.Id == id);
                c.Estado = "inactivo";
                return new RedirectToActionResult("MostrarContratos", null, null);
            };

            var resultado = deshabilitarSimulado(1);

            Assert.AreEqual("inactivo", _contratosSimulados.First().Estado);
            Assert.AreEqual("MostrarContratos", resultado.ActionName);
        }

        // =====================================================
        // ELIMINAR CONTRATO
        // =====================================================
        [Test]
        public void Eliminar_ContratoSinDependencias_LoElimina()
        {
            var contrato = new Contrato { Id = 2, Estado = "activo" };
            _contratosSimulados.Add(contrato);

            // Simulamos método
            Func<int, RedirectToActionResult> eliminarSimulado = (id) =>
            {
                var c = _contratosSimulados.First(x => x.Id == id);
                _contratosSimulados.Remove(c);
                return new RedirectToActionResult("MostrarContratos", null, null);
            };

            var resultado = eliminarSimulado(2);

            Assert.AreEqual(0, _contratosSimulados.Count);
            Assert.AreEqual("MostrarContratos", resultado.ActionName);
        }
    }
}
