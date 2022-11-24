using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Kiota.Web;
using System.Globalization;
using Microsoft.JSInterop;
using Microsoft.Fast.Components.FluentUI;
using BlazorApplicationInsights;
using Kiota.Builder.Configuration;
using Kiota.Web.Authentication.GitHub;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddLocalization();
builder.Services.AddFluentUIComponents();
builder.Services.AddBlazorApplicationInsights();
var configObject = new KiotaConfiguration();
builder.Configuration.Bind(configObject);
builder.Services.AddSingleton(configObject);
builder.Services.AddPatAuthentication();

var host = builder.Build();

CultureInfo culture;
var js = host.Services.GetRequiredService<IJSRuntime>();
var result = await js.InvokeAsync<string>("blazorCulture.get");

if (result != null)
{
    culture = new CultureInfo(result);
}
else
{
    culture = new CultureInfo("en-US");
    await js.InvokeVoidAsync("blazorCulture.set", "en-US");
}

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
