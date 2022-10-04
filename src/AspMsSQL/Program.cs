using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspMsSQL.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(connectionString));
var host = builder.Configuration["DBHOST"] ?? "127.0.0.1";
var port = builder.Configuration["DBPORT"] ?? "1444";
var user = builder.Configuration["DBUSER"] ?? "sa";
var pwd = builder.Configuration["DBPASSWORD"] ?? "SqlPassword!";
var db = builder.Configuration["DBNAME"] ?? "YellowDB";

var conStr = $"Server=tcp:{host},{port};Database={db};UID={user};PWD={pwd};";

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(conStr));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope()) {
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<ApplicationDbContext>();    
    context.Database.Migrate();
}

app.Run();
