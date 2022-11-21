using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PnP.Framework.RER.Common.Model;
using PnP.Framework.RER.Common.Tokens;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder=>
    {
        builder.AddUserSecrets<Program>();
    })
    .ConfigureServices((context, services) =>
    {
        var sharePointCreds = context.Configuration.GetSection(SharePointAppCreds.SectionName).Get<SharePointAppCreds>();
        services.AddHttpClient();
        services.AddSingleton<TokenManagerFactory>();
        services.AddSingleton(sharePointCreds);
    })
    .Build();

host.Run();
