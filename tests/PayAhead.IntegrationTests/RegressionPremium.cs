using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Application.Premium;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Infrastructure;

namespace PayAhead.IntegrationTests;

// Phase 1–8 functional fixtures run as Premium to retain their original multi-rule/bulk scenarios.
// Phase 9 tests use the shipping SQL policy, not this fixture.
internal static class RegressionPremium
{
    public static IWebHostBuilder UseRegressionPremium(this IWebHostBuilder builder)=>builder.ConfigureTestServices(s=>s.AddScoped<IEntitlementService,RegressionEntitlements>());
}
internal sealed class RegressionEntitlements(PayAheadDbContext db) : IEntitlementService
{
    public Task<EntitlementResponse> GetAsync(Guid user,CancellationToken ct)=>Task.FromResult(new EntitlementResponse("Premium",MembershipState.Active,true,new(1000,10,1000),null,true,user,SqlEntitlementService.AccountId(user),AdvancedSubscriptionTracking:true));
    public async Task LockUserAsync(Guid user,CancellationToken ct)
    {if(await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH(UPDLOCK,HOLDLOCK) WHERE Id={user} AND IsActive=1").SingleOrDefaultAsync(ct) is null)throw new AuthenticationRequiredException();}
    public Task EnsureIncreaseAsync(Guid user,LimitResource resource,Guid? plan,int increase,CancellationToken ct)=>LockUserAsync(user,ct);
}
