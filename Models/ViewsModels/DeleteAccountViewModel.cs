using System.ComponentModel.DataAnnotations;

namespace Portifolio.Models.ViewModels
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Informe sua senha atual para confirmar a exclusão.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha atual")]
        public string Password { get; set; } = string.Empty;
    }
}
