namespace Web_Recocycle.Models
{
    public class ReseñaRequest
    {
        public int idUsuario { get; set; }       // ID del usuario que está siendo calificado (empresa)
        public int numEstrella { get; set; }     // Número de estrellas (1 a 5)
        
    }
}
