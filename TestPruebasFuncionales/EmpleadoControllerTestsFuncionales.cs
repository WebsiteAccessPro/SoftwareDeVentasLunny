using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class EmpleadoControllerTestsFuncionales
    {
        private ApplicationDbContext _context;
        private EmpleadoController _controller;
        private Mock<UserManager<IdentityUser>> _mockUserManager;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseInternalServiceProvider(serviceProvider)
                .Options;

            _context = new ApplicationDbContext(options);

            // Semilla de datos
            SeedTestData();

            // Mock UserManager (si lo usa tu controller)
            var mockUserStore = new Mock<IUserStore<IdentityUser>>();
            _mockUserManager = new Mock<UserManager<IdentityUser>>(mockUserStore.Object, null, null, null, null, null, null, null, null);

            _controller = new EmpleadoController(_context);

            // Simular usuario logueado
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Administrador")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Inicializar TempData
            _controller.TempData = new TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Mock.Of<ITempDataProvider>()
            );
        }

        private void SeedTestData()
        {
            // Cargos
            var cargos = new List<Cargo>
            {
                new Cargo { cargo_id = 1, titulo_cargo = "Administrador" },
                new Cargo { cargo_id = 2, titulo_cargo = "Técnico de instalación" },
                new Cargo { cargo_id = 3, titulo_cargo = "Vendedor" }
            };
            _context.Cargos.AddRange(cargos);

            // Empleados
            var empleados = new List<Empleado>
            {
                new Empleado
                {
                    empleado_id = 1,
                    nombres = "Juan Pérez",
                    dni = "12345678",
                    correo = "juan@test.com",
                    telefono = "987654321",
                    cargo_id = 1,
                    estado = "activo",
                    fecha_inicio = DateTime.Now.AddDays(-10),
                    password = "password123"
                },
                new Empleado
                {
                    empleado_id = 2,
                    nombres = "María García",
                    dni = "87654321",
                    correo = "maria@test.com",
                    telefono = "987654322",
                    cargo_id = 2,
                    estado = "activo",
                    fecha_inicio = DateTime.Now.AddDays(-5),
                    password = "password123"
                },
                new Empleado
                {
                    empleado_id = 3,
                    nombres = "Carlos López",
                    dni = "11223344",
                    correo = "carlos@test.com",
                    telefono = "987654323",
                    cargo_id = 3,
                    estado = "inhabilitado",
                    fecha_inicio = DateTime.Now.AddDays(-20),
                    fecha_fin = DateTime.Now.AddDays(-2),
                    password = "password123"
                }
            };
            _context.Empleados.AddRange(empleados);

            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _controller.Dispose();
        }

        [Test]
        public async Task MostrarEmpleados_DebeRetornarVistaConListaEmpleados()
        {
            var result = await _controller.MostrarEmpleados();

            Assert.IsInstanceOf<ViewResult>(result);
            var model = (result as ViewResult)?.Model as List<Empleado>;
            Assert.IsNotNull(model);
            Assert.AreEqual(3, model.Count);
        }

        [Test]
        public void Agregar_Get_DebeRetornarVistaConCargos()
        {
            var result = _controller.Agregar();
            Assert.IsInstanceOf<ViewResult>(result);

            var viewResult = result as ViewResult;
            var cargos = viewResult?.ViewData["Cargos"] as SelectList;
            Assert.IsNotNull(cargos);
            Assert.AreEqual(3, cargos.Count());
        }

        [Test]
        public async Task AgregarEmpleado_Post_Valido_DebeCrearEmpleadoYRedirigir()
        {
            var nuevoEmpleado = new Empleado
            {
                nombres = "Ana Martínez",
                dni = "55667788",
                correo = "ana@test.com",
                telefono = "987654324",
                cargo_id = 2,
                estado = "activo",
                fecha_inicio = DateTime.Now,
                password = "newpassword123"
            };

            var result = await _controller.AgregarEmpleado(nuevoEmpleado, 6);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var redirect = result as RedirectToActionResult;
            Assert.AreEqual("MostrarEmpleadosAdmin", redirect?.ActionName);

            var empleadoCreado = await _context.Empleados.FirstOrDefaultAsync(e => e.dni == "55667788");
            Assert.IsNotNull(empleadoCreado);
            Assert.AreEqual("Ana Martínez", empleadoCreado.nombres);
        }

        [Test]
        public async Task AgregarEmpleado_Post_DniDuplicado_DebeRetornarVistaConError()
        {
            var empleadoDuplicado = new Empleado
            {
                nombres = "Pedro Sánchez",
                dni = "12345678", // DNI ya existente
                correo = "pedro@test.com",
                telefono = "987654325",
                cargo_id = 2,
                estado = "activo",
                fecha_inicio = DateTime.Now,
                password = "password123"
            };

            var result = await _controller.AgregarEmpleado(empleadoDuplicado, 6);

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsTrue(_controller.TempData.ContainsKey("ErrorMessage"));
            StringAssert.Contains("DNI", _controller.TempData["ErrorMessage"].ToString());
        }

        [Test]
        public async Task EditarEmpleado_Get_ConIdValido_DebeRetornarVistaConEmpleado()
        {
            var result = await _controller.Editar(1);

            Assert.IsInstanceOf<ViewResult>(result);
            var model = (result as ViewResult)?.Model as Empleado;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.empleado_id);
            Assert.AreEqual("Juan Pérez", model.nombres);
        }

        [Test]
        public async Task EditarEmpleado_Post_Valido_DebeActualizarEmpleado()
        {
            var empleado = await _context.Empleados.FindAsync(1);
            empleado.nombres = "Juan Carlos Pérez";
            empleado.telefono = "999999999";
            empleado.cargo_id = 2;

            var result = await _controller.EditarEmpleado(1, empleado, null);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var actualizado = await _context.Empleados.FindAsync(1);
            Assert.AreEqual("Juan Carlos Pérez", actualizado.nombres);
            Assert.AreEqual("999999999", actualizado.telefono);
            Assert.AreEqual(2, actualizado.cargo_id);
        }

        [Test]
        public async Task InhabilitarEmpleado_ConIdValido_DebeCambiarEstadoYFechaFin()
        {
            var result = await _controller.InhabilitarEmpleado(1, null);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var empleado = await _context.Empleados.FindAsync(1);
            Assert.AreEqual("inhabilitado", empleado.estado);
        }

        [Test]
        public async Task InhabilitarEmpleado_ConIdInvalido_DebeRetornarNotFound()
        {
            var result = await _controller.InhabilitarEmpleado(999, null);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }
    }
}
