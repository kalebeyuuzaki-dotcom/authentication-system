using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portifolio.Models.ViewModels;
using Portifolio.Services.Authentication;

namespace Portifolio.Controllers
{
    /// <summary>
    /// Controla o fluxo de login e sessão do usuário.
    /// </summary>
    [Authorize]
    public class LoginController : Controller
    {
        private const string ErrorMessageKey = "ErrorMessage";
        private const string LoginModelKey = "LoginModel";
        private const string InvalidDataMessage = "Dados informados em formato inválido. Por favor, verifique as informações.";

        private readonly ILoginService _authService;

        public LoginController(ILoginService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Rota GET que renderiza usuário já logado para página '/Login/Panel'.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Panel));
            }
            return View("~/Views/Home/Index.cshtml");
        }

        /// <summary>
        /// Rota POST para autenticação do usuário no login.
        /// </summary>
        /// <param name="model">Credenciais informadas pelo usuário (email e senha).</param>
        [AllowAnonymous]
        [HttpPost]
        [EnableRateLimiting("BucketTokenPolicy")]
        public async Task<IActionResult> Authenticate([FromForm] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Password = string.Empty;
                ViewBag.ErrorMessage = InvalidDataMessage;
                return View(nameof(Index), model);
            }

            UserSessionViewModel? user = await _authService.AuthenticateAsync(model);

            if (user == null)
            {
                model.Password = string.Empty;
                TempData[LoginModelKey] = JsonSerializer.Serialize(model);
                TempData[ErrorMessageKey] = "E-mail e/ou senha incorretos.";
                return RedirectToAction("Index", "Home");
            }

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("Id", user.Id.ToString()),
                new System.Security.Claims.Claim("Name", user.Name),
                new System.Security.Claims.Claim("Email", user.Email)
            };

            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20)
            });

            return RedirectToAction(nameof(Panel));
        }

        /// <summary>
        /// Rota GET protegida que exibe o painel administrativo após o login bem-sucedido.
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult Panel()
        {
            return View();
        }

        /// <summary>
        /// Realiza o encerramento da sessão atual do usuário limpando os cookies.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Index));
        }
    }
}
