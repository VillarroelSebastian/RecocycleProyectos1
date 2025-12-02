using Microsoft.Data.SqlClient;
using Web_Recocycle.Models;

namespace Web_Recocycle.Models
{
    public class HistorialAsignacionViewModel
    {
        public int IdAsignacion { get; set; }
        public int IdReciclador { get; set; } // ← ESTA ES LA PROPIEDAD QUE FALTA
        public string NombreReciclador { get; set; }
        public string Categoria { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
        public string DesdeHora { get; set; }
        public string HastaHora { get; set; }
        public byte[] ImgPerfil { get; set; }
        public string Estado { get; set; }

    }


    public class DetalleAsignacionViewModel
    {
        public string Categoria { get; set; }
        public string Descripcion { get; set; }
        public string NombreReciclador { get; set; }
        public DateTime FechaRegistro { get; set; }
        

        public List<FechaDisponibleViewModel> FechasDisponibles { get; set; }
    }

    public class FechaDisponibleViewModel
    {
        public string Fecha { get; set; }
        public string DesdeHora { get; set; }
        public string HastaHora { get; set; }
    }

}
