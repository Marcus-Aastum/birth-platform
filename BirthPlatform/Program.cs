using BirthPlatform;
using BirthPlatform.Data;
using BirthPlatform.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
const string HELSEIDSCHEME = "helseid";

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContextFactory<BirthContext>(options =>
    options.UseSqlServer(builder.Configuration.GetValue<string>("ConnectionString")));

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.RequireInteraction = false;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

builder.Services.AddTransient<OidcEvents>();

builder.Services.AddAuthentication(HELSEIDSCHEME).AddOpenIdConnect(HELSEIDSCHEME, oidcOptions =>
{
    oidcOptions.Authority = builder.Configuration.GetValue<string>("Authority");
    oidcOptions.ClientId = builder.Configuration.GetValue<string>("ClientId");

    oidcOptions.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Require;
    oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
    oidcOptions.ResponseMode = OpenIdConnectResponseMode.Query;

    oidcOptions.Scope.Add("helseid://scopes/identity/pid");
    oidcOptions.Scope.Add("helseid://scopes/hpr/hpr_number");

    oidcOptions.EventsType = typeof(OidcEvents);

    oidcOptions.SaveTokens = true;
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Healthcare", p =>
    {
        p.RequireClaim("helseid://claims/hpr/hpr_number");
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseAuthentication();
app.UseAuthorization();


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGroup("/authentication").MapLoginAndLogout();

app.Run();