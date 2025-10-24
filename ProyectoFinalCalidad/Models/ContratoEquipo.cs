using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    [Table("ContratoEquipo")]
    public class ContratoEquipo
    {
        [Key]
        [Column("contrato_equipo_id")]
        public int ContratoEquipoId { get; set; }

        [Required]
        [Column("contrato_id")]
        public int ContratoId { get; set; }
        [ForeignKey(nameof(ContratoId))]
        public Contrato Contrato { get; set; }

        [Required]
        [Column("equipo_id")]
        public int EquipoId { get; set; }
        [ForeignKey(nameof(EquipoId))]
        public Equipo Equipo { get; set; }

        [Column("fecha_asignacion")]
        public DateTime FechaAsignacion { get; set; } = DateTime.Now;

        [Column("estado")]
        [StringLength(20)]
        public string Estado { get; set; } = "activo";
    }
}
