using System.Globalization;
using JardinNoemi.Components;
using JardinNoemi.Components.Account;
using JardinNoemi.Data;
using JardinNoemi.Endpoints;
using JardinNoemi.Models;
using JardinNoemi.Services;
using JardinNoemi.Services.Admin;
using JardinNoemi.Services.Payments;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ---------- Blazor ----------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment());

// ---------- Configuración tipada ----------
builder.Services.Configure<ShopOptions>(builder.Configuration.GetSection(ShopOptions.SectionName));
builder.Services.Configure<ShippingOptions>(builder.Configuration.GetSection(ShippingOptions.SectionName));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));

// ---------- Datos ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection.");

// Blazor Server: un DbContext por operación mediante la fábrica (los circuitos viven mucho tiempo
// y DbContext no es thread-safe). Identity usa el DbContext Scoped por request HTTP.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------- Autenticación y autorización ----------
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "jn.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/cuenta/ingresar";
    options.LogoutPath = "/cuenta/salir";
    options.AccessDeniedPath = "/cuenta/acceso-denegado";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // Sin proveedor de correo configurado aún: no se exige confirmar la cuenta (ver README → mejoras).
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Admin));

// ---------- Servicios de la tienda ----------
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<CartState>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddSingleton<IShippingCalculator, ShippingCalculator>();
builder.Services.AddSingleton<IPaymentGateway, MockPaymentGateway>(); // ← reemplazar por Stripe / Mercado Pago / Webpay
builder.Services.AddSingleton<IOrderNotifier, LoggingOrderNotifier>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAdminCatalogService, AdminCatalogService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddSingleton<LocalImageStorage>();
builder.Services.AddSingleton<IImageStorage>(sp => sp.GetRequiredService<LocalImageStorage>());

var app = builder.Build();

// ---------- Cultura (moneda y fechas) ----------
var store = app.Services.GetRequiredService<IOptions<ShopOptions>>().Value;
CultureInfo.DefaultThreadCurrentCulture = store.CultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = store.CultureInfo;
Money.Configure(store.CultureInfo);

// ---------- Pipeline HTTP ----------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "DENY";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    headers["Content-Security-Policy"] = "base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'";
    await next();
});

// Imágenes subidas desde el panel (fuera de wwwroot, nunca ejecutables).
var storage = app.Services.GetRequiredService<LocalImageStorage>();
var storageOptions = app.Services.GetRequiredService<IOptions<StorageOptions>>().Value;
Directory.CreateDirectory(storage.PhysicalRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storage.PhysicalRoot),
    RequestPath = storageOptions.RequestPath,
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public,max-age=604800"
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAccountEndpoints();
app.MapSeoEndpoints();

await app.InitializeDatabaseAsync();

app.Run();
