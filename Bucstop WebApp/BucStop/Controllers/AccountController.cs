using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BucStop.Controllers;

public class AccountController : Controller
{
    public string email { get; set; } = string.Empty;

    private readonly ILogger<AccountController> _logger;

    public AccountController(ILogger<AccountController> logger)
    {
        _logger = logger;
    }
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string email)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        if (Regex.IsMatch(email, @"\b[A-Za-z0-9._%+-]+@etsu\.edu\b"))
        {
            // If authentication is successful, create a ClaimsPrincipal and sign in the user
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.NameIdentifier, "user_id"),
            };

            var claimsIdentity = new ClaimsIdentity(claims, "custom");
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Sign in the user
            await HttpContext.SignInAsync("CustomAuthenticationScheme", userPrincipal);

            stopwatch.Stop();

            _logger.LogInformation("{Category}: Successful Login Page Loaded in {LoadTime}ms.", "PageLoadTimes", stopwatch.ElapsedMilliseconds);

            return RedirectToAction("Index", "Home");
        }
        else
        {
            // Authentication failed, return to the login page with an error message
            _logger.LogWarning("{Category}: Invalid login attempt for {Email}", "InvalidLogin", email);
            ModelState.AddModelError(string.Empty, "Only ETSU students can play, sorry :(");

            stopwatch.Stop();

            _logger.LogInformation("{Category}: Denied Login Page Loaded in {LoadTime}ms.", "PageLoadTimes", stopwatch.ElapsedMilliseconds);

            return View();
        }
    }

    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User logged out.");
        await HttpContext.SignOutAsync("CustomAuthenticationScheme");
        return RedirectToAction("Login");
    }


}