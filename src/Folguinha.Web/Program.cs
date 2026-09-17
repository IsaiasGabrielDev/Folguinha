using Folguinha.Application;
using Folguinha.Web;
using Folguinha.Web.Servicos;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<Navegador>();
builder.Services.AddScoped<IArmazenamento>(sp => sp.GetRequiredService<Navegador>());
builder.Services.AddScoped<RepositorioDados>();

await builder.Build().RunAsync();
