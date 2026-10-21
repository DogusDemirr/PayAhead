using PayAhead.Application.Premium;
using PayAhead.Infrastructure;

namespace PayAhead.Api;

public sealed class PremiumWorker(IServiceScopeFactory scopes,StoreOptions options,ILogger<PremiumWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if(!options.WorkerEnabled)return;
        while(!ct.IsCancellationRequested)
        {
            try { using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<IPremiumService>().ProcessAsync(ct); }
            catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError("Store reconciliation failed. ExceptionType={ExceptionType}",ex.GetType().Name);}
            try {await Task.Delay(TimeSpan.FromSeconds(5),ct);}catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}
        }
    }
}
