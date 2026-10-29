using PayAhead.Application.Auth;
namespace PayAhead.UnitTests;
internal static class TestLegal
{
    internal static RegistrationLegal Confirmed => new(LegalDocuments.Version, true, LegalDocuments.Version, true, LegalDocuments.Version, true);
}
