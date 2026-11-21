using Microsoft.EntityFrameworkCore;
using Portifolio.Models.Data;
using Portifolio.Models.Domain;

namespace Portifolio.Services.DeleteAccount
{
    public class DeleteAccount : IDeleteAccount
    {
        private readonly AppDbContext _context;

        public DeleteAccount(AppDbContext appDbContext)
        {
            _context = appDbContext;
        }

        public async Task<User?> FindUserAsync(long idUser)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == idUser);
        }

        public async Task<bool> VerifyPasswordAndDeleteAccountAsync(string password, User user)
        {
            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return false;
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return true;
            }

            return true;
        }
    }
}