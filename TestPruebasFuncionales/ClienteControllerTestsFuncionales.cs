using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using ProyectoFinalCalidad.Controllers;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TestPruebasFuncionales
{
    [TestFixture]
    public class ClienteControllerTestsFuncionales
    {
        private ApplicationDbContext _context;
        private ClienteController _controller;
        private Mock<UserManager<IdentityUser>> _mockUserManager;

        [SetUp]
        public void Setup()
        {
            // Crear InMemory DbContext
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var serviceProvider = services.BuildServiceProvider();
            _context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Mock UserManager
            _mockUserManager = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);

            // Crear controlador
            _controller = new ClienteController(_context, _mockUserManager.Object);

            // Inicializar TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            // Simular usuario logueado
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
        new Claim(ClaimTypes.Name, "testuser"),
        new Claim(ClaimTypes.Role, "Usuario")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            // Sembrar datos de prueba
            SeedTestData();
        }

        private void SeedTestData()
        {
            var clientes = new List<Cliente>
            {
                new Cliente
                {
                    ClienteId = 1,
                    Nombres = "Juan Pérez",
                    Dni = "12345678",
                    Telefono = "987654321",
                    Correo = "juan@ejemplo.com",
                    Direccion = "Av. Principal 123",
                    Estado = "activo",
                    Password = "password123",
                    FechaRegistro = DateTime.Now
                },
                new Cliente
                {
                    ClienteId = 2,
                    Nombres = "María García",
                    Dni = "87654321",
                    Telefono = "912345678",
                    Correo = "maria@ejemplo.com",
                    Direccion = "Calle Secundaria 456",
                    Estado = "activo",
                    Password = "password456",
                    FechaRegistro = DateTime.Now
                },
                new Cliente
                {
                    ClienteId = 3,
                    Nombres = "Carlos López",
                    Dni = "11223344",
                    Telefono = "998877665",
                    Correo = "carlos@ejemplo.com",
                    Direccion = "Jr. Tercero 789",
                    Estado = "inactivo",
                    Password = "password789",
                    FechaRegistro = DateTime.Now
                }
            };

            _context.Clientes.AddRange(clientes);
            _context.SaveChanges();
        }

        [Test]
        public async Task MostrarClientes_DebeRetornarVistaConListaDeClientesActivos()
        {
            // Act
            var result = await _controller.MostrarClientes();

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as List<Cliente>;

            Assert.IsNotNull(model);
            Assert.AreEqual(3, model.Count);
        }

        [Test]
        public void AgregarCliente_Get_DebeRetornarVista()
        {
            var result = _controller.AgregarCliente();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task AgregarCliente_Post_Valido_DebeAgregarClienteYRedirigir()
        {
            // Arrange
            var nuevoCliente = new Cliente
            {
                Nombres = "Ana Martínez",
                Dni = "55443322",
                Telefono = "933221144",
                Correo = "ana@ejemplo.com",
                Direccion = "Av. Nueva 321",
                Password = "password321",
                Estado = "activo"
            };

            // Act
            var result = await _controller.AgregarCliente(nuevoCliente);

            // Assert
            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("MostrarClientes", redirectResult.ActionName);

            var clienteGuardado = await _context.Clientes.FirstOrDefaultAsync(c => c.Dni == "55443322");
            Assert.IsNotNull(clienteGuardado);
            Assert.AreEqual("Ana Martínez", clienteGuardado.Nombres);
        }


        [Test]
        public async Task AgregarCliente_Post_DniDuplicado_DebeRetornarVistaConError()
        {
            var clienteDuplicado = new Cliente
            {
                Nombres = "Cliente Duplicado",
                Dni = "12345678", // DNI existente
                Telefono = "911223344",
                Correo = "nuevo@ejemplo.com",
                Direccion = "Av. Duplicada 111",
                Password = "password111",
                Estado = "activo"
            };

            var result = await _controller.AgregarCliente(clienteDuplicado);

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsTrue(_controller.TempData.ContainsKey("ErrorMessage"));
            StringAssert.Contains("DNI", _controller.TempData["ErrorMessage"].ToString());
        }

        [Test]
        public async Task AgregarCliente_Post_CorreoDuplicado_DebeRetornarVistaConError()
        {
            var clienteDuplicado = new Cliente
            {
                Nombres = "Cliente Correo Duplicado",
                Dni = "99887766",
                Telefono = "911223344",
                Correo = "juan@ejemplo.com", // Correo existente
                Direccion = "Av. Correo 111",
                Password = "password111",
                Estado = "activo"
            };

            var result = await _controller.AgregarCliente(clienteDuplicado);

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsTrue(_controller.TempData.ContainsKey("ErrorMessage"));
            StringAssert.Contains("correo", _controller.TempData["ErrorMessage"].ToString().ToLower());
        }

        [Test]
        public async Task AgregarCliente_Post_ModeloInvalido_DebeRetornarVistaConError()
        {
            var clienteInvalido = new Cliente
            {
                Nombres = "", // Requerido
                Dni = "",     // Requerido
                Telefono = "933221144",
                Correo = "invalido@ejemplo.com",
                Direccion = "Av. Invalida 321",
                Password = "password321",
                Estado = "activo"
            };

            _controller.ModelState.AddModelError("Nombres", "El nombre es requerido");
            _controller.ModelState.AddModelError("Dni", "El DNI es requerido");

            var result = await _controller.AgregarCliente(clienteInvalido);

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsTrue(_controller.TempData.ContainsKey("ErrorMessage"));
        }

        [Test]
        public async Task EditarCliente_Get_ClienteExistente_DebeRetornarVistaConCliente()
        {
            var result = await _controller.EditarCliente(1);

            Assert.IsInstanceOf<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as Cliente;

            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.ClienteId);
            Assert.AreEqual("Juan Pérez", model.Nombres);
        }

        [Test]
        public async Task EditarCliente_Get_ClienteNoExistente_DebeRetornarNotFound()
        {
            var result = await _controller.EditarCliente(999);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task EditarCliente_Post_Valido_DebeActualizarClienteYRedirigir()
        {
            var clienteActualizado = new Cliente
            {
                ClienteId = 1,
                Nombres = "Juan Actualizado",
                Dni = "12345678",
                Telefono = "999888777",
                Correo = "juanactualizado@ejemplo.com",
                Direccion = "Av. Actualizada 999",
                Password = "password123",
                Estado = "activo"
            };

            var result = await _controller.EditarCliente(1, clienteActualizado, null);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("MostrarClientes", redirectResult.ActionName);

            var clienteEnDb = await _context.Clientes.FindAsync(1);
            Assert.AreEqual("Juan Actualizado", clienteEnDb.Nombres);
            Assert.AreEqual("999888777", clienteEnDb.Telefono);
            Assert.AreEqual("juanactualizado@ejemplo.com", clienteEnDb.Correo);
        }

        [Test]
        public async Task InhabilitarCliente_ClienteExistente_DebeCambiarEstadoYRedirigir()
        {
            var result = await _controller.InhabilitarCliente(1, null);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var clienteEnDb = await _context.Clientes.FindAsync(1);
            Assert.AreEqual("inactivo", clienteEnDb.Estado);
        }

        [Test]
        public async Task InhabilitarCliente_ClienteNoExistente_DebeRetornarNotFound()
        {
            var result = await _controller.InhabilitarCliente(999, null);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _controller.Dispose();
        }
    }
}