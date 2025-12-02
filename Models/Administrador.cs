using Microsoft.Data.SqlClient;
namespace Web_Recocycle.Models

{
    public class Administrador  : Usuario
    {
        public int Id { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public char Genero { get; set; }
    }

    public class RecicladoresHelper
    {
        private readonly string _connectionString;

        public RecicladoresHelper(string connectionString)
        {
            _connectionString = connectionString;
        }
        public List<Reciclador> ObtenerRankingRecicladores()
        {
            var lista = new List<Reciclador>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"
            SELECT r.idUsuario, r.nombres, r.apellidos, r.imgPerfil, r.puntos,
                   u.telefono, u.correo
            FROM Reciclador r
            JOIN Usuario u ON r.idUsuario = u.idUsuario
            ORDER BY r.puntos DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Reciclador
                        {
                            Id = reader.GetInt32(0),
                            Nombres = reader.GetString(1),
                            Apellidos = reader.GetString(2),
                            ImgPerfil = !reader.IsDBNull(3) ? (byte[])reader.GetValue(3) : new byte[0],
                            Puntos = reader.GetInt32(4),
                            Telefono = reader.GetInt32(5),
                            Correo = reader.GetString(6)
                        });
                    }
                }
            }

            return lista;
        }


        public void ResetearPuntosPorPorcentaje(int porcentaje)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"
                UPDATE Reciclador
                SET Puntos = CAST(Puntos * (1 - @Porcentaje / 100.0) AS INT)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Porcentaje", porcentaje);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public List<RecicladorReporteDTO> ObtenerDatosRecicladoresReporte(string mes)
        {
            var recicladores = new List<RecicladorReporteDTO>();
            var query = @"
        SELECT 
            u.idUsuario,
            ISNULL(SUM(CAST(re.numEstrella AS INT)), 0) AS Puntos,
            ISNULL(AVG(CAST(re.numEstrella AS FLOAT)), 0) AS PromedioEstrellas
        FROM Usuario u
        LEFT JOIN Reseña re 
            ON u.idUsuario = re.idUsuario
            AND MONTH(re.fechaReseña) = @Mes
        LEFT JOIN Asignacion a 
            ON u.idUsuario = a.idRecolector
            AND MONTH(a.fecha) = @Mes
        WHERE u.idRol = 2
        GROUP BY u.idUsuario;
    ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@Mes", Convert.ToInt32(mes));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        recicladores.Add(new RecicladorReporteDTO
                        {
                            Nombres = "Usuario " + reader["idUsuario"].ToString(), // nombre genérico
                            Apellidos = "", // vacío si no tienes apellidos
                            Puntos = Convert.ToInt32(reader["Puntos"]),
                            PromedioEstrellas = Convert.ToDouble(reader["PromedioEstrellas"])
                        });
                    }
                }
            }
            return recicladores;
        }

    }
}
