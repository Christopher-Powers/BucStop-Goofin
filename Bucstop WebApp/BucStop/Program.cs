using BucStop;
using Serilog;
using Serilog.Filters;

/*
 * This is the base program which starts the project.
 */

var builder = WebApplication.CreateBuilder(args);

// Sets up Serilog for logging to console and log file
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/logs.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7) // Creates a new log file each day and keeps up to 7 log files (7 days)
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.WithProperty("Category", "APIRequests"))
        .WriteTo.File("Logs/api_requests.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)) // Creates a new log file that takes in failed api_requests
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.WithProperty("Category", "InvalidLogin"))
        .WriteTo.File("Logs/invalid_login.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)) // Creates a new log file that takes in failed login errors
     .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.WithProperty("Category", "GameSuccess"))
        .WriteTo.File("Logs/game_success.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)) // Creates a new log file that takes in successful api_requests
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

var provider=builder.Services.BuildServiceProvider();
var configuration=provider.GetRequiredService<IConfiguration>();

builder.Services.AddHttpClient<MicroClient>(client =>
{
    var baseAddress = new Uri(configuration.GetValue<string>("Gateway"));

    client.BaseAddress = baseAddress;
});

builder.Services.AddAuthentication("CustomAuthenticationScheme").AddCookie("CustomAuthenticationScheme", options =>
{
    options.LoginPath = "/Account/Login";
});

builder.Services.AddSingleton<GameService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

//Handles routing to "separate" game pages by setting the Play page to have subpages depending on ID
app.MapControllerRoute(
    name: "Games",
    pattern: "Games/{action}/{id?}",
    defaults: new { controller = "Games", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
