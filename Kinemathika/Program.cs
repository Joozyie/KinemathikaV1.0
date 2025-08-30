// WHAT IT DOES: Wires EF Core to use the "DefaultConnection" string.
using Kinemathika.Data;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.IdentityModel.Tokens;
Env.Load();

// ================ CONFIGURATIONS ================
var builder = WebApplication.CreateBuilder(args);
// Supabase - POSTGREST API Config
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var anonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
// var serviceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");  // Remove when switching to prod
// Supabase - POSTGRES DB Config
var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
var db = Environment.GetEnvironmentVariable("POSTGRES_DB");
var user = Environment.GetEnvironmentVariable("POSTGRES_USER");
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
var ssl = Environment.GetEnvironmentVariable("POSTGRES_SSLMODE");

// ================ DEPENDENCIES =====================
builder.Services.AddControllersWithViews();
// R WEB API SERVICE
builder.Services.AddSingleton(new RApiService("http://localhost:27259/")); // Subject to change in prod
// CHANGE SUPABASE ACCESS SERVICE DEPENDING ON ENVIRONMENT (CURRENTLY USES DEV!!!)
builder.Services.AddSingleton(new SupabaseAuth(supabaseUrl ?? "", anonKey ?? ""));
// Supabase Postgres DB Context
var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={password};Ssl Mode={ssl}";
builder.Services.AddDbContext<SbDbContext>(options =>
    options.UseNpgsql(connectionString));
// Token Service (for supabase-side auth and tokens)
builder.Services.AddScoped<SupabaseTokenManager>();
// HTTP Context Accessor (for special cookies)
builder.Services.AddHttpContextAccessor();
// ASP.NET Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";       // redirect if not logged in
        options.AccessDeniedPath = "/Account/Denied"; // redirect if unauthorized
    });
builder.Services.AddAuthorization();

// ================ POST BUILD CONFIGURATIONS =====================
var app = builder.Build();
// Report Generation Config
Rotativa.AspNetCore.RotativaConfiguration.Setup(app.Environment.WebRootPath, "rotativa");
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.Run();
