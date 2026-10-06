using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using SwaApimPoc.Api.Authentication;
using SwaApimPoc.Api.Errors;
using SwaApimPoc.Api.Security;
using SwaApimPoc.Api.Services;
using SwaApimPoc.Application;
using SwaApimPoc.Application.Abstractions;
using SwaApimPoc.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection(GatewayOptions.SectionName));

// Identity comes from the x-ms-client-principal header that SWA adds; no bearer token validation
// and no access_as_user scope.
builder.Services
    .AddAuthentication(SwaAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SwaAuthenticationHandler>(SwaAuthenticationHandler.SchemeName, null);

// Every endpoint requires a signed-in user unless it opts out with [AllowAnonymous].
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IGraphTokenAccessor, HeaderGraphTokenAccessor>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

WebApplication app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Must run before authentication: only requests that came through APIM may carry an identity.
app.UseMiddleware<GatewaySecretMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
