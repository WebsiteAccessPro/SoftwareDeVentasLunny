using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    [Table("PedidoInstalacion")]
    public class PedidoInstalacion
    {
        [Key]
        [Column("pedido_id")]
        public int PedidoId { get; set; }

        [Required(ErrorMessage = "Seleccione un contrato")]
        [Column("contrato_id")]
        public int ContratoId { get; set; }

        [ForeignKey("ContratoId")]
        public Contrato? Contrato { get; set; }

        [Required(ErrorMessage = "Seleccione un empleado")]
        [Column("empleado_id")]
        public int EmpleadoId { get; set; }

        [ForeignKey("EmpleadoId")]
        public Empleado? Empleado { get; set; }

        [Column("fecha_instalacion")]
        public DateTime? FechaInstalacion { get; set; }

        [Column("estado_instalacion")]
        [StringLength(20)]
        public string EstadoInstalacion { get; set; } = "pendiente";

        [Column("observaciones")]
        [StringLength(255)]
        public string Observaciones { get; set; }

        //extra - no en BD
        [NotMapped]
        [Column("jefe_a_cargo")]
        [StringLength(100)]
        public string? JefeACargo { get; set; }
    }
}
