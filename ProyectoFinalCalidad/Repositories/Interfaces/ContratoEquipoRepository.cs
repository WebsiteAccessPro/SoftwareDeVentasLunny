using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Repositories.Interfaces;

namespace ProyectoFinalCalidad.Repositories
{
    public class ContratoEquipoRepository : IContratoEquipoRepository
    {
        private readonly ApplicationDbContext _context;

        public ContratoEquipoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ContratoEquipo>> ListarAsync()
        {
            return await _context.ContratoEquipos
                .Include(ce => ce.Equipo)
                .Include(ce => ce.Contrato)
                .ToListAsync();
        }

        public async Task<ContratoEquipo> BuscarPorIdAsync(int id)
        {
            return await _context.ContratoEquipos
                .Include(ce => ce.Equipo)
                .Include(ce => ce.Contrato)
                .FirstOrDefaultAsync(ce => ce.ContratoEquipoId == id);
        }

        public async Task CrearAsync(ContratoEquipo contratoEquipo)
        {
            _context.ContratoEquipos.Add(contratoEquipo);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(int id)
        {
            var entity = await _context.ContratoEquipos.FindAsync(id);
            if (entity != null)
            {
                _context.ContratoEquipos.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
