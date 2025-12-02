using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Web_Recocycle.Models
{
    public class Reciclador : Usuario
    {
        public int Id { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public char Genero { get; set; }
        public int Puntos { get; set; }
        public byte[] ImgPerfil { get; set; }

        public double PromedioEstrellas { get; set; }


        public string ImgPerfilBase64 { get; set; }

    }

    public class UsuarioRecicladorViewModel
    {
        public Usuario? Usuario { get; set; }
        public Reciclador? Reciclador { get; set; }
        public List<Reciclador>  TopRecicladores { get; set; } = new List<Reciclador>();
        public Premio Premio { get; set; }
    }

    public class DatosUsuarioReciclador
    {
        public Usuario? Usuario { get; set; }
        public Reciclador? Reciclador { get; set; }
    }

    public class DatabaseHelper1
    {
        private readonly string _connectionString;

        public DatabaseHelper1(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //Lectura de datos
        public UsuarioRecicladorViewModel DatosUsuario(int id)
        {
            var vm = new UsuarioRecicladorViewModel();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"SELECT u.correo, r.nombres, r.apellidos, r.puntos, r.imgPerfil
                         FROM Usuario u
                         INNER JOIN Reciclador r ON r.idUsuario = u.idUsuario
                         WHERE u.idUsuario = @idUsuario AND u.estado = 1";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idUsuario", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vm.Usuario = new Usuario
                            {
                                Correo = reader.GetString(0)
                            };
                            vm.Reciclador = new Reciclador
                            {
                                Nombres = reader.GetString(1),
                                Apellidos = reader.GetString(2),
                                Puntos = reader.GetInt32(3),
                                ImgPerfil = reader.IsDBNull(4) ? null : (byte[])reader[4]
                            };
                        }
                        else
                        {
                            vm.Usuario = new Usuario { Correo = "" };
                            vm.Reciclador = new Reciclador { Nombres = "No se encontraron datos", Apellidos = "", Puntos = 0 };
                        }
                    }
                }
            }

            vm.TopRecicladores = Top10Recicladores();
            vm.Premio = ObtenerPremio();

            return vm;
        }


        public int RegistrarAdministrador(Administrador admin)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string queryUsuario = @"INSERT INTO Usuario (telefono, correo, contrasenia, fechaRegistro, fechaModificacion, estado, idRol)
                                OUTPUT INSERTED.idUsuario
                                VALUES (@telefono, @correo, @contrasenia, @fechaRegistro, @fechaModificacion, @estado, @idRol)";

                using (SqlCommand cmd = new SqlCommand(queryUsuario, con))
                {
                    cmd.Parameters.AddWithValue("@telefono", admin.Telefono);
                    cmd.Parameters.AddWithValue("@correo", admin.Correo);
                    cmd.Parameters.AddWithValue("@contrasenia", DatabaseHelper.HashPassword(admin.Contrasenia)); // reutiliza hash
                    cmd.Parameters.AddWithValue("@fechaRegistro", admin.FechaRegistro);
                    cmd.Parameters.AddWithValue("@fechaModificacion", admin.FechaModificacion);
                    cmd.Parameters.AddWithValue("@estado", admin.Estado);
                    cmd.Parameters.AddWithValue("@idRol", 1);

                    int idUsuario = (int)cmd.ExecuteScalar();

                    string queryAdmin = @"INSERT INTO Administrador (idUsuario, nombres, apellidos, genero)
                                  VALUES (@idUsuario, @nombres, @apellidos, @genero)";

                    using (SqlCommand cmdAdmin = new SqlCommand(queryAdmin, con))
                    {
                        cmdAdmin.Parameters.AddWithValue("@idUsuario", idUsuario);
                        cmdAdmin.Parameters.AddWithValue("@nombres", admin.Nombres);
                        cmdAdmin.Parameters.AddWithValue("@apellidos", admin.Apellidos);
                        cmdAdmin.Parameters.AddWithValue("@genero", admin.Genero);

                        cmdAdmin.ExecuteNonQuery();
                    }

                    return idUsuario;
                }
            }
        }


        //Notificacion:

        public bool ActualizarEstadoAsignacion(int idAsignacion, string nuevoEstado)
        {
            string query = "UPDATE Asignacion SET estado = @estado WHERE idAsignacion = @id";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@estado", nuevoEstado);
                    command.Parameters.AddWithValue("@id", idAsignacion);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<Reciclador> ObtenerRecicladoresConDatos()
        {
            var recicladores = new List<Reciclador>();

            string query = @"
        SELECT r.idUsuario, r.nombres, r.apellidos, r.puntos,
               ISNULL(AVG(CAST(re.numEstrella AS FLOAT)), 0) AS PromedioEstrellas
        FROM Reciclador r
        LEFT JOIN Reseña re ON r.idUsuario = re.idUsuario
        GROUP BY r.idUsuario, r.nombres, r.apellidos, r.puntos";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            recicladores.Add(new Reciclador
                            {
                                IdUsuario = Convert.ToInt32(reader["idUsuario"]),
                                Nombres = reader["nombres"].ToString(),
                                Apellidos = reader["apellidos"].ToString(),
                                Puntos = Convert.ToInt32(reader["puntos"]),
                                PromedioEstrellas = Convert.ToDouble(reader["PromedioEstrellas"])
                            });
                        }
                    }
                }
            }

            return recicladores;
        }

        public List<EmpresaInteresadaViewModel> ObtenerEmpresasInteresadas(int idReciclador)
        {
            var empresas = new List<EmpresaInteresadaViewModel>();

            string query = @"
        SELECT a.idAsignacion, e.nombreEmpresa, e.correo, rr.descripcion, c.nombre AS Categoria
        FROM Asignacion a
        INNER JOIN EmpresaRecolector e ON a.idRecolector = e.idUsuario
        INNER JOIN RegistroReciclaje rr ON a.idRegistroReciclaje = rr.idRegistroReciclaje
        INNER JOIN Categoria c ON rr.idCategoria = c.idCategoria
        WHERE a.estado = 'Pendiente' AND a.idReciclador = @idReciclador";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@idReciclador", idReciclador);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            empresas.Add(new EmpresaInteresadaViewModel
                            {
                                IdAsignacion = reader.GetInt32(0),
                                NombreEmpresa = reader.GetString(1),
                                Correo = reader.GetString(2),
                                Descripcion = reader.GetString(3),
                                Categoria = reader.GetString(4)
                            });
                        }
                    }
                }
            }

            return empresas;
        }

        public List<EmpresaInteresadaViewModel> ObtenerEmpresasPorEstado(int idReciclador, string estado)
        {
            var empresas = new List<EmpresaInteresadaViewModel>();

            string query = @"
SELECT 
    a.idAsignacion,
    e.idUsuario AS IdEmpresa,
    e.nombreEmpresa,
    u.correo,
    rr.descripcion,
    c.nombre AS Categoria,
    a.fecha,
    a.desdeHora,
    a.hastaHora,
    (SELECT CAST(AVG(CAST(numEstrella AS DECIMAL(10,2))) AS DECIMAL(10,2))
 FROM Reseña r
 WHERE r.idUsuario = e.idUsuario) AS PromedioEstrellas

FROM Asignacion a
INNER JOIN EmpresaRecolector e ON a.idRecolector = e.idUsuario
INNER JOIN Usuario u ON e.idUsuario = u.idUsuario
INNER JOIN RegistroReciclaje rr ON a.idRegistroReciclaje = rr.idRegistroReciclaje
INNER JOIN Categoria c ON rr.idCategoria = c.idCategoria
WHERE a.estado = @estado AND a.idReciclador = @idReciclador
";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@estado", estado);
                    command.Parameters.AddWithValue("@idReciclador", idReciclador);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            empresas.Add(new EmpresaInteresadaViewModel
                            {
                                IdAsignacion = reader.GetInt32(0),
                                IdEmpresa = reader.GetInt32(1),
                                NombreEmpresa = reader.GetString(2),
                                Correo = reader.GetString(3),
                                Descripcion = reader.GetString(4),
                                Categoria = reader.GetString(5),
                                Fecha = reader.IsDBNull(6) ? null : reader.GetDateTime(6).ToString("yyyy-MM-dd"),
                                DesdeHora = reader.IsDBNull(7) ? null : reader.GetTimeSpan(7).ToString(@"hh\:mm"),
                                HastaHora = reader.IsDBNull(8) ? null : reader.GetTimeSpan(8).ToString(@"hh\:mm"),
                                PromedioEstrellas = reader.IsDBNull(9)
    ? null
    : Convert.ToDouble(reader.GetDecimal(9))

                            });
                        }
                    }
                }
            }

            return empresas;
        }

        public bool GuardarReseña(int idUsuario, int numEstrella)
        {
            string query = @"
        INSERT INTO Reseña (idUsuario, numEstrella, fechaReseña)
        VALUES (@idUsuario, @numEstrella, GETDATE())";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@idUsuario", idUsuario);
                    command.Parameters.AddWithValue("@numEstrella", numEstrella);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        //Recien puesto
        public bool CambiarEstadoAsignacion(int idAsignacion, string nuevoEstado)
        {
            string query = "UPDATE Asignacion SET estado = @estado WHERE idAsignacion = @idAsignacion";

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@estado", nuevoEstado);
                    cmd.Parameters.AddWithValue("@idAsignacion", idAsignacion);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        public bool MarcarAsignacionComoCalificada(int idAsignacion)
        {
            string query = @"UPDATE Asignacion 
                     SET Estado = 'Calificado'
                     WHERE idAsignacion = @idAsignacion";

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idAsignacion", idAsignacion);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }



        //pUNTOS
        public bool SumarPuntosAlReciclador(int idUsuario, int numEstrella)
        {
            int puntos = numEstrella switch
            {
                5 => 30,
                4 => 25,
                3 => 17,
                2 => 10,
                1 => 4,
                _ => 0
            };

            string query = "UPDATE Reciclador SET puntos = puntos + @puntos WHERE idUsuario = @idUsuario";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@puntos", puntos);
                    command.Parameters.AddWithValue("@idUsuario", idUsuario);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }


        //Lista del Ranking
        public List<Reciclador> Top10Recicladores()
        {
            var lista = new List<Reciclador>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"SELECT TOP 10 r.nombres, r.apellidos, r.puntos, r.imgPerfil
                         FROM Reciclador r
                         ORDER BY r.puntos DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Reciclador
                        {
                            Nombres = reader.GetString(0),
                            Apellidos = reader.GetString(1),
                            Puntos = reader.GetInt32(2),
                            ImgPerfil = reader.IsDBNull(3) ? null : (byte[])reader[3]
                        });
                    }
                }
            }

            return lista;
        }

        //verificar si el correo ya existe en EMPRESA
        public bool CorreoExiste(string correo)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Usuario WHERE correo = @correo";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@correo", correo);
                int count = (int)cmd.ExecuteScalar();
                return count > 0; // true si el correo ya existe
            }
        }

        public bool TelefonoValido(int telefono)
        {
            string telStr = telefono.ToString();

            if (telStr.Length > 8 || telStr.Length < 7)
                return false;

            foreach (char c in telStr)
            {
                if (!char.IsDigit(c))
                    return false;
            }

            return true;
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2")); // Convierte a hexadecimal
                }
                return builder.ToString();
            }
        }

        public int RegistrarUsuario(Usuario user, Reciclador reciclador)
        {

            if (CorreoExiste(user.Correo))
            {
                return 3;
            }

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    string insertUsuario = @"
                            INSERT INTO Usuario (telefono, correo, contrasenia, idRol)
                            VALUES (@telefono, @correo, @contrasenia, @idRol);
                            SELECT SCOPE_IDENTITY();";

                    SqlCommand cmdUsuario = new SqlCommand(insertUsuario, con, transaction);
                    cmdUsuario.Parameters.AddWithValue("@telefono", user.Telefono);
                    cmdUsuario.Parameters.AddWithValue("@correo", user.Correo);

                    string contraseñaCifrada = HashPassword(user.Contrasenia);

                    cmdUsuario.Parameters.AddWithValue("@contrasenia", contraseñaCifrada);
                    cmdUsuario.Parameters.AddWithValue("@idRol", 3);

                    int idUsuario = Convert.ToInt32((decimal)cmdUsuario.ExecuteScalar());

                    string insertReciclador = @"
                            INSERT INTO Reciclador (idUsuario, nombres, apellidos, genero, imgPerfil)
                            VALUES (@idUsuario, @nombres, @apellidos, @genero, @imgPerfil);";

                    SqlCommand cmdReciclador = new SqlCommand(insertReciclador, con, transaction);
                    cmdReciclador.Parameters.AddWithValue("@idUsuario", idUsuario);
                    cmdReciclador.Parameters.AddWithValue("@nombres", reciclador.Nombres);
                    cmdReciclador.Parameters.AddWithValue("@apellidos", reciclador.Apellidos);
                    cmdReciclador.Parameters.AddWithValue("@genero", reciclador.Genero);
                    cmdReciclador.Parameters.AddWithValue("@imgPerfil", reciclador.ImgPerfil);

                    cmdReciclador.ExecuteNonQuery();

                    transaction.Commit();
                    return 1;
                }
                catch
                {
                    transaction.Rollback();
                    return 2;
                }
            }
        }

        public Premio ObtenerPremio()
        {
            var vm = new Premio();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"SELECT TOP 1 nombre, imgPremio, fechaLimite
                         FROM Premio
                         ORDER BY fechaLimite DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        vm = new Premio
                        {
                            Nombre = reader.GetString(0),
                            ImgPremio = reader.IsDBNull(1) ? null : (byte[])reader[1],
                            FechaLimite = reader.GetDateTime(2)
                        };
                    }
                }
            }

            return vm;
        }

        public DatosUsuarioReciclador ObtenerDatos(int id)
        {
            var vm = new DatosUsuarioReciclador();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string query = @"SELECT u.correo, r.nombres, r.apellidos, r.imgPerfil, u.telefono
                         FROM Usuario u
                         INNER JOIN Reciclador r ON r.idUsuario = u.idUsuario
                         WHERE u.idUsuario = @idUsuario AND u.estado = 1";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@idUsuario", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vm.Usuario = new Usuario
                            {
                                Correo = reader.GetString(0),
                                Telefono = reader.GetInt32(4)
                            };
                            vm.Reciclador = new Reciclador
                            {
                                Nombres = reader.GetString(1),
                                Apellidos = reader.GetString(2),
                                ImgPerfil = reader.IsDBNull(3) ? null : (byte[])reader[3]
                            };
                        }
                        else
                        {
                            vm.Usuario = new Usuario { Correo = "" };
                            vm.Reciclador = new Reciclador { Nombres = "No se encontraron datos", Apellidos = ""};
                        }
                    }
                }
            }

            return vm;
        }

        public bool CambiarContrasenia(int idUsuario, string nuevaContraseniaHash)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "UPDATE Usuario SET contrasenia = @Contrasenia WHERE idUsuario = @IdUsuario";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Contrasenia", nuevaContraseniaHash);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    return filasAfectadas > 0;
                }
            }
        }

        public string ObtenerHashContrasenia(int idUsuario)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT contrasenia FROM Usuario WHERE idUsuario = @IdUsuario";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    object result = cmd.ExecuteScalar();
                    return result != null ? result.ToString() : "";
                }
            }
        }



        public bool ModificarDatosUsuario(int idUsuario, Usuario usuario, Reciclador reciclador)
        {
            byte[] imagenBytes = null;
            bool imagenActualizada = false;

            if (!string.IsNullOrEmpty(reciclador.ImgPerfilBase64))
            {
                try
                {
                    imagenBytes = Convert.FromBase64String(reciclador.ImgPerfilBase64);
                    imagenActualizada = true;
                }
                catch (FormatException)
                {
                    // Error en el formato Base64. Dejamos imagenActualizada = false.
                }
            }
            
            else if (reciclador.ImgPerfilBase64 != null)
            {
                imagenActualizada = true;
                imagenBytes = null;
            }

            bool exito = false;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    var paramReciclador = new List<SqlParameter>();
                    string setReciclador = "";

                    if (!string.IsNullOrEmpty(reciclador.Nombres))
                    {
                        setReciclador += "Nombres = @Nombres, ";
                        paramReciclador.Add(new SqlParameter("@Nombres", reciclador.Nombres));
                    }
                    if (!string.IsNullOrEmpty(reciclador.Apellidos))
                    {
                        setReciclador += "Apellidos = @Apellidos, ";
                        paramReciclador.Add(new SqlParameter("@Apellidos", reciclador.Apellidos));
                    }
                    if (imagenActualizada)
                    {
                        setReciclador += "ImgPerfil = @ImgPerfil, ";
                        paramReciclador.Add(new SqlParameter("@ImgPerfil", imagenBytes ?? (object)DBNull.Value));
                    }

                    if (!string.IsNullOrEmpty(setReciclador))
                    {
                        setReciclador = setReciclador.TrimEnd(',', ' ');
                        string sqlReciclador = $"UPDATE Reciclador SET {setReciclador} WHERE IdUsuario = @IdUsuario";

                        using (SqlCommand cmd = new SqlCommand(sqlReciclador, conn, transaction))
                        {
                            cmd.Parameters.AddRange(paramReciclador.ToArray());
                            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    var paramUsuario = new List<SqlParameter>();
                    string setUsuario = "";

                    if (!string.IsNullOrEmpty(usuario.Correo))
                    {
                        setUsuario += "Correo = @Correo, ";
                        paramUsuario.Add(new SqlParameter("@Correo", usuario.Correo));
                    }
                    
                    if (usuario.Telefono > 0)
                    {
                        setUsuario += "Telefono = @Telefono, ";
                        paramUsuario.Add(new SqlParameter("@Telefono", usuario.Telefono));
                    }

                    if (!string.IsNullOrEmpty(setUsuario))
                    {
                        setUsuario = setUsuario.TrimEnd(',', ' ');
                        string sqlUsuario = $"UPDATE Usuario SET {setUsuario} WHERE IdUsuario = @IdUsuario";

                        using (SqlCommand cmd = new SqlCommand(sqlUsuario, conn, transaction))
                        {
                            cmd.Parameters.AddRange(paramUsuario.ToArray());
                            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    exito = true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    exito = false;
                }

                return exito;
            }
        }
    }
}
