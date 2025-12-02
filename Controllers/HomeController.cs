using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Web_Recocycle.Models;

namespace Web_Recocycle.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DatabaseHelper _db;
        private readonly DatabaseHelper1 _db1;

        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, DatabaseHelper db, DatabaseHelper1 db1, IWebHostEnvironment env)
        {
            _logger = logger;
            _db = db;
            _db1 = db1;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult RegistroUsuario()
        {
            return View();
        }

        public IActionResult InicioSesionUsuario()
        {
            return View();
        }

        //[HttpPost]
        //public IActionResult Login(Usuario user)
        //{
        //    Usuario usuario = _db.IniciarSesion(user.Correo, user.Contrasenia);

        //    if (usuario != null)
        //    {
        //        //agregado recientemente
        //        HttpContext.Session.SetInt32("IdUsuario", usuario.IdUsuario);

        //        switch (usuario.RolSesion)
        //        {
        //            case 1: // Rol Administrador
        //                return RedirectToAction("Index", "Administrador");
        //            case 2: // Rol Empresa recolectora
        //                return RedirectToAction("Index", "Empresa");
        //            case 3: // Rol Usuario Reciclador
        //                return RedirectToAction("PerfilReciclador", "Usuario");
        //        }
        //        ViewBag.ErrorLogin = true;
        //        return View("InicioSesionUsuario");
        //    }
        //    else
        //    {
        //        ViewBag.ErrorLogin = true;
        //        return View("InicioSesionUsuario");
        //    }
        //}
        [HttpPost]
        public IActionResult Login(Usuario user)
        {
            Usuario usuario = _db.IniciarSesion(user.Correo, user.Contrasenia);

            if (usuario.Sesion == 0)
            {
                ViewBag.ErrorLogin = true;
                return View("InicioSesionUsuario");
            }

            // Guardar datos en sesión
            HttpContext.Session.SetInt32("IdUsuario", usuario.Sesion);
            HttpContext.Session.SetString("Correo", usuario.Correo);   
            HttpContext.Session.SetInt32("Rol", usuario.RolSesion);   

            
            switch (usuario.RolSesion)
            {
                case 1:
                    return RedirectToAction("Index", "Administrador");
                case 2:
                    return RedirectToAction("Index", "Empresa");
                case 3:
                    return RedirectToAction("PerfilReciclador", "Usuario");
            }

            ViewBag.ErrorLogin = true;
            return View("InicioSesionUsuario");
        }



        //LO LLAMA A EMPRESA PARA QUE SEA PREDETERMINADA
        [HttpPost]
        public IActionResult Registrar(Usuario user, Reciclador reciclador, string ConfirmContrasenia)
        {
            if (string.IsNullOrWhiteSpace(user.Contrasenia))
            {
                TempData["RegistroExitoso"] = 4;
                return RedirectToAction("RegistroUsuario");
            }


            byte[] imgPredeterminada = ObtenerImagenPredeterminada();
            reciclador.ImgPerfil = imgPredeterminada;

            string patron = @"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#_-]).{8,}$";
            if (!Regex.IsMatch(user.Contrasenia, patron))
            {
                TempData["RegistroExitoso"] = 4;
                return RedirectToAction("RegistroUsuario");
            }

            if (user.Contrasenia != ConfirmContrasenia)
            {
                TempData["RegistroExitoso"] = 5;
                return RedirectToAction("RegistroUsuario");
            }

            int resultado = _db1.RegistrarUsuario(user, reciclador);

            if (resultado == 1)
            {
                TempData["RegistroExitoso"] = resultado;
                return RedirectToAction("RegistroUsuario");
            }
            else if (resultado == 2)
            {
                TempData["RegistroExitoso"] = resultado;
                return RedirectToAction("RegistroUsuario");
            }
            else
            {
                TempData["RegistroExitoso"] = resultado;
                return RedirectToAction("RegistroUsuario");
            }
        }

        //imagen predeterminada
        public byte[] ObtenerImagenPredeterminada()
        {
            string rutaImagen = Path.Combine(_env.WebRootPath, "imagenes", "PerfilPredeterminado.jpg");
            return System.IO.File.ReadAllBytes(rutaImagen);
        }

        [HttpPost]
        public JsonResult ValidarTelefono(string telefono)
        {
            bool telefonoValido = _db1.TelefonoValido(Convert.ToInt32(telefono));
            return Json(new { telefonoValido });
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
