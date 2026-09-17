using Folguinha.Application;
using Folguinha.Infrastructure.Feriados;
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
builder.Services.AddScoped<Loja>();
builder.Services.AddScoped<IFeriadoProvider>(_ => new BrasilApiFeriados(new HttpClient()));

await builder.Build().RunAsync();
