using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Repositories.Interfaces;
using ProyectoFinalCalidad.Services.Interfaces;

namespace ProyectoFinalCalidad.Services
{
    public class ContratoEquipoService : IContratoEquipoService
    {
        private readonly IContratoEquipoRepository _repo;

        public ContratoEquipoService(IContratoEquipoRepository repo)
        {
            _repo = repo;
        }

        public async Task AsignarContratoEquipoAsync(int contratoId, int equipoId, string estado)
        {
            var asignacion = new ContratoEquipo
            {
                ContratoId = contratoId,
                EquipoId = equipoId,
                FechaAsignacion = DateTime.Now,
                Estado = estado
            };
            await _repo.CrearAsync(asignacion);
        }

        public async Task<List<ContratoEquipo>> ListarAsignacionesAsync()
        {
            return await _repo.ListarAsync();
        }
    }
}
