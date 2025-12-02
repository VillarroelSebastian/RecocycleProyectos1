using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Web_Recocycle.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public int Telefono { get; set; }
        public string Correo { get; set; }
        public string Contrasenia { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaModificacion { get; set; }
        public int Estado { get; set; }
        public int Rol { get; set; }

        public int Sesion { get; set; }
        public int RolSesion { get; set; }
    }

    public static class SesionGlobal
    {
        public static int IdUsuario { get; set; }

        /////
        ///
        //public static int RolSesion { get; set; }
    }

    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password == null)
            {
                return "";
            }
            else {

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    StringBuilder builder = new StringBuilder();
                    foreach (byte b in bytes)
                    {
                        builder.Append(b.ToString("x2"));
                    }
                    return builder.ToString();
                } 
            }
        }

        public Usuario IniciarSesion(string correo, string contraseña)
        {
            Usuario usuarioFallido = new Usuario
            {
                Sesion = 0,
                RolSesion = 0,
                Correo = null
            };

            try
            {
                string contraseñaCifrada = HashPassword(contraseña);

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    string query = @"SELECT idUsuario, idRol, correo
                             FROM Usuario 
                             WHERE correo = @correo AND contrasenia = @contrasenia AND estado = 1";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@correo", correo);
                        cmd.Parameters.AddWithValue("@contrasenia", contraseñaCifrada);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                SesionGlobal.IdUsuario = reader.GetInt32(0);

                                Console.WriteLine($"[DEBUG SESIÓN]ID de Usuario asignado: {SesionGlobal.IdUsuario}");
                                return new Usuario
                                {
                                    Sesion = reader.GetInt32(0),
                                    RolSesion = reader.GetInt32(1),
                                    Correo = reader.GetString(2) 
                                };
                            }
                        }
                    }
                }
                return usuarioFallido;
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de SQL al iniciar sesión: {ex.Message}");
                return usuarioFallido;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error general al iniciar sesión: {ex.Message}");
                return usuarioFallido;
            }
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
                    cmd.Parameters.AddWithValue("@contrasenia", HashPassword(admin.Contrasenia));
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


        public Usuario IniciarSesionAdministrador(string correo, string contraseña)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                string contraseñaCifrada = HashPassword(contraseña);
                Console.WriteLine("Hash generado: " + contraseñaCifrada);

                string query = @"SELECT idUsuario, idRol, correo
                 FROM Usuario
                 WHERE correo = @correo AND contrasenia = @contrasenia AND estado = 1";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@correo", correo);
                    cmd.Parameters.AddWithValue("@contrasenia", contraseñaCifrada);
                    //cmd.Parameters.AddWithValue("@contrasenia", HashPassword(contraseña));
                    // cmd.Parameters.AddWithValue("@contrasenia", hashed);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int rol = reader.GetInt32(1);
                            if (rol == 1) // solo si es administrador
                            {
                                return new Usuario
                                {
                                    Sesion = reader.GetInt32(0),
                                    RolSesion = reader.GetInt32(1),
                                    IdUsuario = reader.GetInt32(0),
                                    Correo = reader.GetString(2) 
                                };
                            }
                        }
                    
                    }
                }
            }

            return new Usuario { Sesion = 0, RolSesion = 0 };
        }


    }
}
