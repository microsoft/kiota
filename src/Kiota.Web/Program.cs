﻿using System.Globalization;
using BlazorApplicationInsights;
using Kiota.Builder.Configuration;
using Kiota.Web;
using Kiota.Web.Authentication.GitHub;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddLocalization();
builder.Services.AddFluentUIComponents(configuration =>
{
    ArgumentNullException.ThrowIfNull(configuration);
    configuration.IconConfiguration.Sizes = new[]
    {
        IconSize.Size16,
        IconSize.Size20,
        IconSize.Size24,
        IconSize.Size28,
    };
    configuration.IconConfiguration.Variants = new[]
    {
        IconVariant.Regular
    };
    configuration.EmojiConfiguration.Styles = Array.Empty<EmojiStyle>();
    configuration.EmojiConfiguration.Groups = Array.Empty<EmojiGroup>();
});
builder.Services.AddBlazorApplicationInsights();
var configObject = new KiotaConfiguration();
builder.Configuration.Bind(configObject);
builder.Services.AddSingleton(configObject);
builder.Services.AddPatAuthentication();
builder.Services.AddSearchService();

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
