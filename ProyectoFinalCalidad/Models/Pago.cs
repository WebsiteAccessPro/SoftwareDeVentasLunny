using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    [Table("Pago")]
    public class Pago
    {
        [Key]
        [Column("pago_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PagoId { get; set; }

        [Required]
        [Column("contrato_id")]
        public int ContratoId { get; set; }

        [Required]
        [Column("monto")]
        [Range(0.01, double.MaxValue)]
        public decimal Monto { get; set; }

        [Column("estado_pago")]
        [StringLength(20)]
        public string EstadoPago { get; set; } = "pendiente";

        [Required]
        [Column("fecha_de_vencimiento")]
        public DateTime FechaDeVencimiento { get; set; }

        [Column("fecha_pago")]
        public DateTime? FechaPago { get; set; }

        [Column("metodo_pago")]
        [StringLength(50)]
        public string MetodoPago { get; set; }

        [ForeignKey(nameof(ContratoId))]
        public virtual Contrato Contrato { get; set; }
    }
}
