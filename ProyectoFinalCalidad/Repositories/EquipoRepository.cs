using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace ProyectoFinalCalidad.Repositories
{
  
    public class EquipoRepository : IEquipoRepository
    {
        private readonly ApplicationDbContext _context;
        public EquipoRepository(ApplicationDbContext context) => _context = context;

        public async Task<List<Equipo>> GetAllAsync() => await _context.Equipos.ToListAsync();

        public async Task<Equipo> GetByIdAsync(int id) => await _context.Equipos.FindAsync(id);

        public async Task AddAsync(Equipo equipo)
        {
            _context.Equipos.Add(equipo);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Equipo equipo)
        {
            _context.Equipos.Update(equipo);
            await _context.SaveChangesAsync();
        }

        public async Task IncreaseStockAsync(int equipoId, int cantidad)
        {
            var equipo = await _context.Equipos.FindAsync(equipoId);
            if (equipo != null)
            {
                equipo.CantidadStock += cantidad;
                await _context.SaveChangesAsync();
            }
        }
    }

}
