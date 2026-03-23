using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BirackaMestaReport.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _config;
        public bool Error { get; private set; }

        public LoginModel(IConfiguration config) => _config = config;

        public IActionResult OnGet(string? logout)
        {
            if (logout == "1")
            {
                HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage();
            }
            if (User.Identity?.IsAuthenticated ?? false)
                return Redirect("/");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string password)
        {
            var correct = _config["AdminPassword"] ?? "posmatraci2026";
            if (password != correct)
            {
                Error = true;
                return Page();
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "admin") };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Redirect("/");
        }
    }
}
