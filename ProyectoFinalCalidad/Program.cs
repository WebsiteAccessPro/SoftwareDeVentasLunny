using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ProyectoFinalCalidad.Data;
using ProyectoFinalCalidad.Services;

var builder = WebApplication.CreateBuilder(args);

// Agregar DbContext para la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Configurar la cookie de autenticación para el manejo de sesiones
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<IContratoService, ContratoService>();
builder.Services.AddSingleton<IEmailSender, Microsoft.AspNetCore.Identity.UI.Services.NoOpEmailSender>();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Repositorios y servicios para asignación de equipos
builder.Services.AddScoped<ProyectoFinalCalidad.Repositories.Interfaces.IContratoEquipoRepository, ProyectoFinalCalidad.Repositories.ContratoEquipoRepository>();
builder.Services.AddScoped<ProyectoFinalCalidad.Services.Interfaces.IContratoEquipoService, ProyectoFinalCalidad.Services.ContratoEquipoService>();

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
var app = builder.Build();

// Método asincrono para crear roles y usuarios automáticamente
async Task CreateRolesAndUsers(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = { "Administrador", "Empleado", "Usuario" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Crear usuario admin por defecto si no existe
    var adminUser = await userManager.FindByEmailAsync("admin@dominio.com");
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = "admin@dominio.com",
            Email = "admin@dominio.com"
        };

        var result = await userManager.CreateAsync(adminUser, "ContraseñaSegura123*");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Administrador");
        }
    }
}

using (var scope = app.Services.CreateScope())
{
    var serviceProviderScope = scope.ServiceProvider;
    await CreateRolesAndUsers(serviceProviderScope);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CheckEstadoUsuarioMiddleware>();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();