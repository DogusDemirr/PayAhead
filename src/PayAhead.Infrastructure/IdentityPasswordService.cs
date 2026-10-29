using Microsoft.AspNetCore.Identity;
using PayAhead.Application.Auth;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class IdentityPasswordService : IPasswordService
{
    private readonly PasswordHasher<User> hasher = new(Microsoft.Extensions.Options.Options.Create(
        new PasswordHasherOptions { IterationCount = 210_000 }));
    public string Hash(User user, string password) => hasher.HashPassword(user, password);
    public bool Verify(User user, string password, out bool needsRehash)
    {
        PasswordVerificationResult result;
        try { result = hasher.VerifyHashedPassword(user, user.PasswordHash, password); }
        catch (FormatException) { result = PasswordVerificationResult.Failed; }
        needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
        return result != PasswordVerificationResult.Failed;
    }
    public bool VerifyStored(User user, string hash, string password)
    {
        try { return hasher.VerifyHashedPassword(user, hash, password) != PasswordVerificationResult.Failed; }
        catch (FormatException) { return false; }
    }
}
