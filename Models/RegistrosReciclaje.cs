using Microsoft.Data.SqlClient;
using System.Data;

namespace Web_Recocycle.Models
{
    public class RegistrosReciclaje
    {
        public int IdRegistroReciclaje { get; set; }
        public int Categoria { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Latitud { get; set; }
        public string Longitud { get; set; }

        public int IdUsuario { get; set; }
        public string Modo { get; set; }

        public string NombreCategoria { get; set; }
        public string NombreCompletoReciclador { get; set; }

        public List<FechaDisponible> FechasDisponibles { get; set; }
        public List<ImagenesPrueba> Imagenes { get; set; }

    }

    public class FechaDisponible
    {
        public string Fecha { get; set; }
        public string DesdeHora { get; set; }
        public string HastaHora { get; set; }
    }

    public class ImagenesPrueba
    {
        public string Imagenes { get; set; }
    }

    public class DatabaseHelper2
    {
        private readonly string _connectionString;

        public DatabaseHelper2(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public int GuardarRegistroCompleto(RegistrosReciclaje request)
        {

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    string sqlRegistro = @"INSERT INTO RegistroReciclaje (idCategoria, descripcion, latitud, longitud, idUsuario)
                                                            VALUES (@idCategoria, @descripcion, @latitud, @longitud, @idUsuario);
                                                            SELECT SCOPE_IDENTITY();";

                    SqlCommand cmdRR = new SqlCommand(sqlRegistro, con, transaction);
                    cmdRR.Parameters.AddWithValue("@idCategoria", request.Categoria);
                    cmdRR.Parameters.AddWithValue("@descripcion", request.Descripcion ?? "");
                    cmdRR.Parameters.AddWithValue("@latitud", request.Latitud);
                    cmdRR.Parameters.AddWithValue("@longitud", request.Longitud);
                    cmdRR.Parameters.AddWithValue("@idUsuario", SesionGlobal.IdUsuario);

                    int idRegistro = Convert.ToInt32((decimal)cmdRR.ExecuteScalar());
                    //Console.WriteLine($"Resultado ExecuteScalar: {idRegistro}");

                    if (idRegistro == null)
                    {
                        throw new Exception("No se pudo obtener el ID del registro");
                    }
                    //Console.WriteLine($"ID Registro generado: {idRegistro}");

                    if (request.FechasDisponibles != null && request.FechasDisponibles.Any())
                    {
                        string sqlFecha = @"INSERT INTO FechaDisponible (fecha, desdeHora, hastaHora, idRegistroReciclaje)
                                                        VALUES (@fecha, @desdeHora, @hastaHora, @idRegistroReciclaje);";

                        int fechaCount = 0;
                        foreach (var fecha in request.FechasDisponibles)
                        {
                            //Console.WriteLine($"Insertando fecha {fechaCount + 1}: {fecha.Fecha} {fecha.DesdeHora}-{fecha.HastaHora}");

                            SqlCommand cmdFD = new SqlCommand(sqlFecha, con, transaction);
                            cmdFD.Parameters.AddWithValue("@fecha", DateTime.Parse(fecha.Fecha));
                            cmdFD.Parameters.AddWithValue("@desdeHora", TimeSpan.Parse(fecha.DesdeHora));
                            cmdFD.Parameters.AddWithValue("@hastaHora", TimeSpan.Parse(fecha.HastaHora));
                            cmdFD.Parameters.AddWithValue("@idRegistroReciclaje", idRegistro);

                            int filas = cmdFD.ExecuteNonQuery();
                            //Console.WriteLine($"Fecha {fechaCount + 1} insertada. Filas: {filas}");
                            fechaCount++;
                        }
                    }

                    if (request.Imagenes != null && request.Imagenes.Any())
                    {
                        string sqlImagen = @"INSERT INTO ImagenesPrueba (imagenes, idRegistroReciclaje) 
                                                        VALUES (@imagenes, @idRegistroReciclaje);";

                        int imgCount = 0;
                        foreach (var img in request.Imagenes)
                        {
                            if (string.IsNullOrEmpty(img.Imagenes))
                                continue;

                            //Console.WriteLine($"Insertando imagen {imgCount + 1}");

                            SqlCommand cmdIP = new SqlCommand(sqlImagen, con, transaction);
                            byte[] imageBytes = Convert.FromBase64String(img.Imagenes);
                            cmdIP.Parameters.AddWithValue("@imagenes", imageBytes);
                            cmdIP.Parameters.AddWithValue("@idRegistroReciclaje", idRegistro);

                            int filas = cmdIP.ExecuteNonQuery();
                            //Console.WriteLine($"Imagen {imgCount + 1} insertada. Tamaño: {imageBytes.Length} bytes, Filas: {filas}");
                            imgCount++;
                        }
                    }

                    transaction.Commit();
                    //Console.WriteLine("🎉 TRANSACCIÓN COMPLETADA EXITOSAMENTE");
                    return 1;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    //Console.WriteLine($"💥 ERROR EN TRANSACCIÓN: {ex.Message}");
                    //Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                    //if (ex.InnerException != null)
                    //{
                    //    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    //}

                    return 0;
                }
            }
        }

        public List<RegistrosReciclaje> GetUserRecyclingItems(int userId)
        {
            var items = new List<RegistrosReciclaje>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sqlQuery = @"
                                    SELECT rr.idRegistroReciclaje,
                                           c.nombre,         
                                           rr.descripcion,   
                                           rr.modo,
                                           rr.fechaRegistro
                                    FROM RegistroReciclaje rr
                                    INNER JOIN Categoria c ON c.idCategoria = rr.idCategoria
                                    WHERE rr.estado = 1 AND rr.idUsuario = @idUsuario;
                                ";

                try
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@idUsuario", userId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            int idReciclajeIndex = reader.GetOrdinal("idRegistroReciclaje");
                            int nombreIndex = reader.GetOrdinal("nombre");
                            int descripcionIndex = reader.GetOrdinal("descripcion");
                            int modoIndex = reader.GetOrdinal("modo");

                            while (reader.Read())
                            {
                                items.Add(new RegistrosReciclaje
                                {
                                    IdRegistroReciclaje = reader.GetInt32(idReciclajeIndex),
                                    NombreCategoria = reader.GetString(nombreIndex),
                                    Descripcion = reader.GetString(descripcionIndex),
                                    Modo = reader.GetString(modoIndex),
                                    FechaRegistro = reader.GetDateTime(4),

                                    FechasDisponibles = ObtenerFechasDisponibles(reader.GetInt32(0)),
                                    Imagenes = ObtenerImagenes(reader.GetInt32(0)),
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database Error retrieving recycling items for user {userId}: {ex.Message}");
                }
            }

            return items;
        }

        public List<FechaDisponible> ObtenerFechasDisponibles(int idRegistroReciclaje) 
        {
            var fechas = new List<FechaDisponible>();
            string query = @"SELECT fecha, desdeHora, hastaHora 
                            FROM FechaDisponible
                            WHERE idRegistroReciclaje = @id";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", idRegistroReciclaje);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            DateTime fechaDb = reader.GetDateTime(reader.GetOrdinal("fecha"));
                            string fechaString = fechaDb.ToShortDateString();

                            TimeSpan desdeHoraDb = reader.GetTimeSpan(reader.GetOrdinal("desdeHora"));
                            TimeSpan hastaHoraDb = reader.GetTimeSpan(reader.GetOrdinal("hastaHora"));

                            string desdeHoraString = desdeHoraDb.ToString(@"hh\:mm");
                            string hastaHoraString = hastaHoraDb.ToString(@"hh\:mm");

                            fechas.Add(new FechaDisponible
                            {
                                Fecha = fechaString,
                                DesdeHora = desdeHoraString,
                                HastaHora = hastaHoraString
                            });
                        }
                    }
                }
            }
            return fechas;
        }

        public List<ImagenesPrueba> ObtenerImagenes(int idRegistroReciclaje)
        {
            var imagenes = new List<ImagenesPrueba>();
            string query = @"SELECT imagenes
                            FROM ImagenesPrueba
                            WHERE idRegistroReciclaje = @id";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", idRegistroReciclaje);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] imageBytes = (byte[])reader["imagenes"];

                            string base64String = Convert.ToBase64String(imageBytes);

                            string dataUrl = $"data:image/jpeg;base64,{base64String}";

                            imagenes.Add(new ImagenesPrueba
                            {
                                Imagenes = dataUrl
                            });
                        }
                    }
                }
            }
            return imagenes;
        }

        public int EliminarRegistroReciclaje(int idRegistro)
        {
            string sqlQuery = @"UPDATE RegistroReciclaje 
                                SET estado = 0 
                                WHERE idRegistroReciclaje = @Id AND estado = 1;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", idRegistro);

                    try
                    {
                        conn.Open();
                        return cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al eliminar registro: {ex.Message}");
                        return 0;
                    }
                }
            }
        }
    }
}
 