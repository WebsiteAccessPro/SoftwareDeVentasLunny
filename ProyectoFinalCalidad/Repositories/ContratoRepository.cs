using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Repositories.Interfaces;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Services;

namespace pruevas_diars_fabricio_0001.Repositories
{
    public class ContratoRepository : IContratoService
    {
        private readonly ApplicationDbContext _context;

        public ContratoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Contrato>> ObtenerTodosAsync()
        {
            return await _context.Contratos
                .Include(c => c.Cliente)
                .Include(c => c.PlanServicio)
                .Include(c => c.Empleado)
                .ToListAsync();
        }

        public async Task<Contrato?> ObtenerPorIdAsync(int id)
        {
            return await _context.Contratos
                .Include(c => c.Cliente)
                .Include(c => c.PlanServicio)
                .Include(c => c.Empleado)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AgregarAsync(Contrato contrato)
        {
            contrato.FechaContrato = DateTime.Now;
            contrato.Estado = "activo";
            _context.Contratos.Add(contrato);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(Contrato contrato)
        {
            _context.Contratos.Update(contrato);
            await _context.SaveChangesAsync();
        }
    }
}
