using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Portifolio.Models.ViewModels;
using Portifolio.Services.Register;

namespace Portifolio.Controllers
{
    /// <summary>
    /// Coordena o fluxo de cadastro de novas contas.
    /// </summary>
    public class RegisterAccountController : Controller
    {
        private const string RegisterModelKey = "RegisterModel";
        private const string ErrorMessageKey = "ErrorMessage";
        private const string AllowAccessConfirmationKey = "AllowAccessConfirmation";
        private const string InvalidDataMessage = "Os dados informados estão em um formato inválido. Por favor, verifique as informações.";
        private const string SuccessMessage = "Sua conta foi criada e ativada com sucesso! Faça seu login.";
        private const string SessionExpiredMessage = "Sua sessão expirou. Por favor, reinicie o processo de cadastro.";
        private const string GenericErrorMessage = "Por favor, digite o código de confirmação.";

        private readonly IRegisterService _registerService;
        private readonly IConfirmationCodeService _confirmationCodeService;

        public RegisterAccountController(IRegisterService registerService, IConfirmationCodeService confirmationCodeService)
        {
            _registerService = registerService;
            _confirmationCodeService = confirmationCodeService;
        }

        /// <summary>
        /// Rota GET que carrega a página para criar nova conta.
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Rota POST que valida as informações de pré-registro de novas contas.
        /// </summary>
        /// <param name="model">Modelo de Pré-Registro a ser validado (ex: Nome, Email, Senha).</param>
        /// <returns>Redireciona o usuário para página de confirmação de código se as informações forem válidas.</returns>
        [HttpPost]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Register([FromForm] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Password = string.Empty;
                TempData[ErrorMessageKey] = InvalidDataMessage;
                return RedirectToAction(nameof(Register), model);
            }

            var result = await _registerService.GenerateCodeAndSentEmailAsync(model);

            if (!result.IsSuccess)
            {
                model.Password = string.Empty;
                model.ConfirmPassword = string.Empty;
                TempData[ErrorMessageKey] = result.ErrorMessage;
                return RedirectToAction(nameof(Register), model);
            }

            TempData[RegisterModelKey] = model.Email.ToLower();
            TempData[AllowAccessConfirmationKey] = true;
            return RedirectToAction(nameof(ConfirmCode));
        }

        /// <summary>
        /// Rota get protegida com Token de permissão de acesso.
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ConfirmCode()
        {
            var email = TempData.Peek(RegisterModelKey)?.ToString(); // RECEBEMOS O TEMPDATA E USAMOS O PEEK PARA SOBREVIVER AO PROX POST
            var tokenAcess = TempData.Peek(AllowAccessConfirmationKey)?.ToString();

            if (email is null || tokenAcess is null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Email = email;
            return View();
        }

        /// <summary>
        /// Rota POST que processa a validação do código de confirmação enviado pelo usuário.
        /// </summary>
        /// <param name="code">Código de verificação digitado no formulário.</param>
        /// <returns>Redireciona para a página inicial/login em caso de sucesso, ou recarrega a página de 
        /// confirmação exibindo o erro correspondente.</returns>
        [HttpPost]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> VerifyCode(string code)
        {
            var email = TempData.Peek(RegisterModelKey)?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData[ErrorMessageKey] = SessionExpiredMessage;
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData[AllowAccessConfirmationKey] = true;
                TempData[ErrorMessageKey] = GenericErrorMessage;
                return RedirectToAction(nameof(ConfirmCode));
            }

            var result = await _confirmationCodeService.VerifyCodeAndCreateUserAccountAsync(email, code);

            if (!result.IsSuccess)
            {
                TempData[AllowAccessConfirmationKey] = true;
                TempData[ErrorMessageKey] = result.ErrorMessage;
                return RedirectToAction(nameof(ConfirmCode));
            }
            TempData.Remove(RegisterModelKey);
            TempData.Remove(AllowAccessConfirmationKey);

            TempData["SuccessMessage"] = SuccessMessage;
            return RedirectToAction("Index", "Home");
        }
    }
}
