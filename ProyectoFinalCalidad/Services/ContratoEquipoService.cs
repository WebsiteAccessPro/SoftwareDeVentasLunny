using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProyectoFinalCalidad.Models;
using ProyectoFinalCalidad.Repositories.Interfaces;
using ProyectoFinalCalidad.Services.Interfaces;

namespace ProyectoFinalCalidad.Services
{
    public class ContratoEquipoService : IContratoEquipoService
    {
        private readonly IContratoEquipoRepository _repo;
        private readonly Data.ApplicationDbContext _context;

        public ContratoEquipoService(IContratoEquipoRepository repo, Data.ApplicationDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task AsignarContratoEquipoAsync(int contratoId, int equipoId, string estado)
        {
            // Buscar primera unidad disponible del equipo
            var unidad = _context.EquiposUnidades
                .Where(u => u.EquipoId == equipoId && u.EstadoUnidad == "disponible")
                .OrderBy(u => u.EquipoUnidadId)
                .FirstOrDefault();

            if (unidad == null)
            {
                throw new InvalidOperationException("No hay unidades disponibles para el equipo seleccionado.");
            }

            // Asignar y actualizar estados
            unidad.EstadoUnidad = "asignado";
            unidad.FechaModificacion = DateTime.Now;
            _context.EquiposUnidades.Update(unidad);

            var equipo = await _context.Equipos.FindAsync(equipoId);
            if (equipo != null && equipo.CantidadStock > 0)
            {
                equipo.CantidadStock -= 1;
                equipo.FechaModificacion = DateTime.Now;
                _context.Equipos.Update(equipo);
            }

            var asignacion = new ContratoEquipo
            {
                ContratoId = contratoId,
                EquipoId = equipoId,
                EquipoUnidadId = unidad.EquipoUnidadId,
                FechaAsignacion = DateTime.Now,
                Estado = estado
            };

            await _repo.CrearAsync(asignacion);
            await _context.SaveChangesAsync();
        }

        public async Task AsignarUnidadEspecificaAsync(int contratoId, int equipoId, int equipoUnidadId, string estado)
        {
            // Validar que la unidad pertenece al equipo y está disponible
            var unidad = await _context.EquiposUnidades.FindAsync(equipoUnidadId);
            if (unidad == null || unidad.EquipoId != equipoId)
            {
                throw new InvalidOperationException("La unidad seleccionada no pertenece al equipo indicado.");
            }
            if (!string.Equals(unidad.EstadoUnidad, "disponible", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La unidad seleccionada no está disponible.");
            }

            // Actualizar estado de la unidad
            unidad.EstadoUnidad = "asignado";
            unidad.FechaModificacion = DateTime.Now;
            _context.EquiposUnidades.Update(unidad);

            // Reducir stock del equipo
            var equipo = await _context.Equipos.FindAsync(equipoId);
            if (equipo != null && equipo.CantidadStock > 0)
            {
                equipo.CantidadStock -= 1;
                equipo.FechaModificacion = DateTime.Now;
                _context.Equipos.Update(equipo);
            }

            // Reutilizar asignación existente del contrato para este equipo si aún no tiene unidad
            var asignacionExistente = _context.ContratoEquipos
                .Where(ce => ce.ContratoId == contratoId && ce.EquipoId == equipoId && ce.EquipoUnidadId == null)
                .OrderBy(ce => ce.FechaAsignacion)
                .FirstOrDefault();

            if (asignacionExistente != null)
            {
                asignacionExistente.EquipoUnidadId = equipoUnidadId;
                asignacionExistente.FechaAsignacion = DateTime.Now;
                asignacionExistente.Estado = estado;
                _context.ContratoEquipos.Update(asignacionExistente);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Crear asignación específica
                var asignacion = new ContratoEquipo
                {
                    ContratoId = contratoId,
                    EquipoId = equipoId,
                    EquipoUnidadId = equipoUnidadId,
                    FechaAsignacion = DateTime.Now,
                    Estado = estado
                };

                await _repo.CrearAsync(asignacion);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ContratoEquipo>> ListarAsignacionesAsync()
        {
            return await _repo.ListarAsync();
        }

        public async Task CambiarEstadoAsignacionAsync(int contratoEquipoId, string nuevoEstado)
        {
            var asignacion = await _repo.BuscarPorIdAsync(contratoEquipoId);
            if (asignacion == null)
                throw new InvalidOperationException("Asignación no encontrada.");

            // Cargar unidad y equipo relacionados
            var unidad = asignacion.EquipoUnidadId.HasValue
                ? await _context.EquiposUnidades.FindAsync(asignacion.EquipoUnidadId.Value)
                : null;
            var equipo = await _context.Equipos.FindAsync(asignacion.EquipoId);

            nuevoEstado = (nuevoEstado ?? string.Empty).ToLower();

            switch (nuevoEstado)
            {
                case "devuelto":
                    if (unidad != null)
                    {
                        unidad.EstadoUnidad = "disponible";
                        unidad.FechaModificacion = DateTime.Now;
                        _context.EquiposUnidades.Update(unidad);
                    }
                    if (equipo != null)
                    {
                        equipo.CantidadStock += 1;
                        equipo.FechaModificacion = DateTime.Now;
                        _context.Equipos.Update(equipo);
                    }
                    asignacion.Estado = "devuelto";
                    break;
                case "mantenimiento":
                    if (unidad != null)
                    {
                        unidad.EstadoUnidad = "mantenimiento";
                        unidad.FechaModificacion = DateTime.Now;
                        _context.EquiposUnidades.Update(unidad);
                    }
                    asignacion.Estado = "mantenimiento";
                    break;
                case "asignado":
                case "entregado":
                    if (unidad != null)
                    {
                        unidad.EstadoUnidad = "asignado";
                        unidad.FechaModificacion = DateTime.Now;
                        _context.EquiposUnidades.Update(unidad);
                    }
                    asignacion.Estado = nuevoEstado;
                    break;
                default:
                    throw new InvalidOperationException("Estado no soportado.");
            }

            // Persistir cambios
            _context.ContratoEquipos.Update(asignacion);
            await _context.SaveChangesAsync();
        }
    }
}
