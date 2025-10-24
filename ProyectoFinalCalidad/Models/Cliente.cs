using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    [Table("Cliente")]
    public class Cliente
    {
        [Key]
        [Column("cliente_id")]
        public int ClienteId { get; set; }

        [Required, StringLength(100)]
        [Column("nombres")]
        public string Nombres { get; set; }

        [Required, StringLength(15)]
        [Column("dni")]
        public string Dni { get; set; }

        [StringLength(255)]
        [Column("direccion")]
        public string Direccion { get; set; }

        [Required, StringLength(255)]
        [Column("correo")]
        public string Correo { get; set; }

        [StringLength(20)]
        [Column("telefono")]
        public string Telefono { get; set; }

        [Required, StringLength(16)]
        [Column("password")]
        public string Password { get; set; }

        [StringLength(20)]
        [Column("estado")]
        public string Estado { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
    }
}
