namespace webWarehouseInventoryManagement.Service
{
    public class PageAccessMiddleware
    {
        private readonly RequestDelegate _next;

        public PageAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = context.User;

            if (user.Identity.IsAuthenticated)
            {
                // Bypass authentication and claim checks for AJAX requests
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    await _next(context);
                    return;
                }

                // Get accessible pages from user claims
                var accessiblePages = user.Claims.FirstOrDefault(c => c.Type == "AccessiblePages")?.Value?.Split(',');

                // Extract current path
                var currentPage = context.Request.Path.Value;
                if (currentPage.Contains("logout"))
                {
                    await _next(context);
                    return;
                }

                // Check if the user has access to the current page
                if (accessiblePages == null ||
                    (!accessiblePages.Any(p =>
                        currentPage.StartsWith(p.Replace("/Index", ""), StringComparison.OrdinalIgnoreCase))
                     && !context.User.IsInRole("Admin")))
                {
                    var firstAccessiblePage = accessiblePages.FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstAccessiblePage))
                    {
                        context.Response.Redirect($"{firstAccessiblePage}/Index");
                    }
                    else
                    {
                        context.Response.Redirect("/Login/Login");
                    }
                    return;
                }

            }

            await _next(context);
        }

    }
}
