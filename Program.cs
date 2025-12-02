using Microsoft.EntityFrameworkCore;
using Web_Recocycle.Data;
using Web_Recocycle.Models;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddTransient<DatabaseHelper>();
builder.Services.AddScoped<DatabaseHelper1>();
builder.Services.AddScoped<DatabaseHelper2>();
builder.Services.AddScoped<DatabaseHelper3>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();


RotativaConfiguration.Setup(app.Environment.WebRootPath);


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



app.Run();
