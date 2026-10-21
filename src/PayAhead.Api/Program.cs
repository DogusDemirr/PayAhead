using PayAhead.Api;
if (args is ["--health-probe"]) return await ContainerProbe.RunAsync();
var app = PayAheadApi.Build(args);
app.Run();
return 0;
public partial class Program;
