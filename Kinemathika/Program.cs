// WHAT IT DOES: Wires EF Core to use the "DefaultConnection" string.
using Kinemathika.Data;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
Env.Load();

var builder = WebApplication.CreateBuilder(args);
// var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
// var anonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
// var serviceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__Default")));

// R WEB API SERVICE
builder.Services.AddSingleton(new RApiService("http://localhost:43593/"));

// CHANGE SUPABASE ACCESS SERVICE DEPENDING ON ENVIRONMENT (CURRENTLY USES DEV!!!)
// builder.Services.AddSingleton(new SupabaseDevApiService(supabaseUrl, serviceRoleKey));

var app = builder.Build();

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
