using System.Net;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http.Features;
using webWarehouseInventoryManagement.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using webWarehouseInventoryManagement.DataAccess.Data;
using webWarehouseInventoryManagement.DataAccess.Models;
using webWarehouseInventoryManagement.DataAccess.DbAccess;
using Microsoft.AspNetCore.Authentication.Cookies;
using webWarehouseInventoryManagement.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(6); // Set session timeout to 6 hours
});

builder.Services.AddControllersWithViews();

builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));
builder.Services.Configure<AmazonSheetDefaults>(builder.Configuration.GetSection("AmazonSheetDefaults"));
builder.Services.AddScoped<TokenService>();

builder.Services.AddTransient<AccessService>();
builder.Services.AddTransient<IUser,User>();
builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.AddTransient<ISqlDataAccess, SqlDataAccess>();
builder.Services.AddTransient<ILog , Log>();
builder.Services.AddTransient<IUsersActivityLog, UsersActivityLog>();
builder.Services.AddTransient<IListingFormService, ListingFormService>();


builder.Services.AddAuthentication(auth =>
{
    auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // For JWT token validation
    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // For Google login
    auth.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var jwtConfig = builder.Configuration.GetSection("JwtConfig").Get<JwtConfig>();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig.Issuer,
        ValidAudience = jwtConfig.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret))
    };
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"];
    options.ClientSecret = builder.Configuration["Google:ClientSecret"];
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 3L * 1024 * 1024 * 1024; // 3GB
});


var app = builder.Build();

// Increase the file size limit
app.Use(async (context, next) =>
{
    context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 3L * 1024 * 1024 * 1024; // 3GB
    await next();
});

app.UseStatusCodePages(async contextAccessor =>
{
    var response = contextAccessor.HttpContext.Response;

    if (response.StatusCode == (int)HttpStatusCode.Unauthorized ||
        response.StatusCode == (int)HttpStatusCode.Forbidden)
    {
        response.Redirect("/");
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Shared/Error");
}

app.UseSession();
app.Use(async (context, next) =>
{
    var token = context.Session.GetString("Token");
    if (!string.IsNullOrEmpty(token))
    {
        context.Request.Headers.Add("Authorization", "Bearer " + token);
    }
    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<PageAccessMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();
