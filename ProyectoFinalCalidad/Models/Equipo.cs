using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalCalidad.Models
{
    [Table("Equipo")]
    public class Equipo
    {
        [Key]
        [Column("equipo_id")]
        public int EquipoId { get; set; }

        [Required]
        [Column("codigo_equipo")]
        [StringLength(50)]
        [Display(Name = "Código de Equipo")]
        public string CodigoEquipo { get; set; }

        [Required]
        [Column("nombre_equipo")]
        [StringLength(100)]
        public string NombreEquipo { get; set; }

        [Column("descripcion")]
        [StringLength(255)]
        public string Descripcion { get; set; }

        [Column("cantidad_stock")]
        public int CantidadStock { get; set; }

        [Column("estado")]
        [StringLength(20)]
        public string Estado { get; set; } = "disponible";

        [Column("fecha_registro")]
        [Display(Name = "Fecha de Registro")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Column("fecha_modificacion")]
        [Display(Name = "Última Modificación")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime FechaModificacion { get; set; } = DateTime.Now;

        public ICollection<ContratoEquipo> ContratoEquipos { get; set; }
            = new List<ContratoEquipo>();

    }
}