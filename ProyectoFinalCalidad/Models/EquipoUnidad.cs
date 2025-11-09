using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    [Table("EquipoUnidad")]
    public class EquipoUnidad
    {
        [Key]
        [Column("equipo_unidad_id")]
        public int EquipoUnidadId { get; set; }

        [Required]
        [Column("equipo_id")]
        public int EquipoId { get; set; }
        [ForeignKey(nameof(EquipoId))]
        public Equipo Equipo { get; set; }

        [Required]
        [Column("codigo_unidad")]
        [StringLength(60)]
        public string CodigoUnidad { get; set; }

        [Column("estado_unidad")]
        [StringLength(20)]
        public string EstadoUnidad { get; set; } = "disponible"; // disponible, asignado, entregado, mantenimiento, devuelto

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; } = DateTime.Now;
    }
}