using Newtonsoft.Json;

namespace Web_Recocycle.Models
{
    public class EmpresaInteresadaViewModel
    {
        public int IdAsignacion { get; set; }

        [JsonProperty("IdEmpresa")]
        public int IdEmpresa { get; set; } // este es el idUsuario que se usará en GuardarReseña
        public string NombreEmpresa { get; set; }
        public string Correo { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }

        public double? PromedioEstrellas { get; set; }

        // --- Nuevos campos ---
        public string Fecha { get; set; }
        public string DesdeHora { get; set; }
        public string HastaHora { get; set; }
    }
}
