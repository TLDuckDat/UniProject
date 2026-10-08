namespace UniProject.API.Modules.Identity
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
        bool NeedsRehash(string passwordHash);
    }
}
