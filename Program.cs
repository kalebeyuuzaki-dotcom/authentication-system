using Microsoft.EntityFrameworkCore;
using Portifolio.Models.Data;
using Portifolio.Services.Authentication;
using DotNetEnv;
using Portifolio.Configurations;
using Portifolio.Services.Email;
using Portifolio.Services.Register;
using Microsoft.AspNetCore.Authentication.Cookies;
using Portifolio.Services.PasswordRecovery;
using Portifolio.Services.PasswordReset;
using Hangfire;
using Portifolio.Services.Jobs;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Cookies 
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    });

builder.Services.AddControllersWithViews();

// Connection Db
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("A string de conexão 'DefaultConnection' não foi encontrada.");
}

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Services
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddScoped<IConfirmationCodeService, ConfirmationCodeService>();
builder.Services.AddScoped<IRecoverPassword, RecoverPassword>();
builder.Services.AddScoped<IConfirmCodePasswordRecoverService, ConfirmCodePasswordRecoverService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IPasswordRecoveryJob, PasswordRecoveryJob>();
builder.Services.AddScoped<IRegisterAccountJob, RegisterAccountJob>();

// Configure Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

// Server Hangfire
builder.Services.AddHangfireServer();

// E-mail service
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Rate limiting
builder.Services.AddRateLimiter(options =>
{

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path;

        if (path.StartsWithSegments("/Home/TooManyRequests") ||
        path.StartsWithSegments("/Home/Error") ||
        path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/css") ||
        path.StartsWithSegments("/js") ||
        path.Value?.EndsWith(".css") == true ||
        path.Value?.EndsWith(".js") == true)
        {
            return RateLimitPartition.GetNoLimiter("BypassStaticAndErrorRoutes");
        }
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueLimit = 0
            });
    });

    options.AddTokenBucketLimiter(policyName: "BucketTokenPolicy", opt =>
    {
        opt.TokenLimit = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(12);
        opt.TokensPerPeriod = 1;
        opt.QueueLimit = 0;
        opt.AutoReplenishment = true;
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Migrations auto
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch
    {
        throw new InvalidOperationException("Não foi possível realizar as Migrations para o SQL Server.");
    }
}
app.UseHttpsRedirection();
app.UseRouting();
// Agora ele vai para uma página genérica de erro ou uma página 404 específica
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
