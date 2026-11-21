using Portifolio.Models.Domain;

namespace Portifolio.Services.DeleteAccount
{
    /// <summary>
    /// Interface responsável pelo fluxo de exclusão de conta.
    /// </summary>
    public interface IDeleteAccount
    {
        /// <summary>
        /// Encontra o usuário no banco de dados pelo Id. 
        /// </summary>
        /// <param name="idUser">Id do usuário.</param>
        /// <returns>Retorna a entidade User em caso de sucesso, senão, retorna nulo.</returns>
        Task<User?> FindUserAsync(long idUser);

        /// <summary>
        /// Verifica a senha informada com a salva em rash no banco de dados. Exclui o registro de usuário no banco de dados.
        /// </summary>
        /// <param name="password">Senha informada pelo usuário.</param>
        /// <param name="user">Entidade User para verificação do rash e exclusão da conta.</param>
        /// <returns>Retorna true em caso de sucesso na exclusão da conta. Senão, retorna false.</returns>
        Task<bool> VerifyPasswordAndDeleteAccountAsync(string password, User user);
    }
}