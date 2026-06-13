using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThoiTrang.Data;
using ThoiTrang.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký DbContext (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dịch vụ băm mật khẩu
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Dịch vụ gửi email (SMTP)
builder.Services.AddScoped<ThoiTrang.Services.IEmailSender, ThoiTrang.Services.SmtpEmailSender>();

// Xác thực bằng Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/DangNhap";
        options.AccessDeniedPath = "/Home/DangNhap";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        // Trả 401 (không redirect) cho POST / AJAX request — để JS xử lý đúng
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                // POST request hoặc có header X-Requested-With (AJAX) → trả 401 trực tiếp
                if (ctx.Request.Method == "POST" ||
                    ctx.Request.Headers.ContainsKey("X-Requested-With"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "application/json";
                    return ctx.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Method == "POST" ||
                    ctx.Request.Headers.ContainsKey("X-Requested-With"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    ctx.Response.ContentType = "application/json";
                    return ctx.Response.WriteAsync("{\"error\":\"Forbidden\"}");
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// ===== Seed mật khẩu thật cho tài khoản mẫu (chỉ chạy nếu còn placeholder) =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    var admin = db.Users.FirstOrDefault(u => u.Email == "admin@monowear.vn");
    if (admin != null && admin.PasswordHash == "HASH_ADMIN")
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

    var khach = db.Users.FirstOrDefault(u => u.Email == "khoi@example.com");
    if (khach != null && khach.PasswordHash == "HASH_USER")
        khach.PasswordHash = hasher.HashPassword(khach, "Khach@123");

    db.SaveChanges();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
