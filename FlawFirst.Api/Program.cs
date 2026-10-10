using System.Security.Claims;
using System.Text;
using FlawFirst.Api.Common;
using FlawFirst.Api.Data;
using FlawFirst.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.");

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwt = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("Thiếu cấu hình Jwt.");
if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key phải dài ít nhất 32 bytes.");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString).EnableDetailedErrors(builder.Environment.IsDevelopment()));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IProfanityFilter, ProfanityFilter>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, FriendlyAuthorizationResultHandler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .SelectMany(item => item.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Dữ liệu không hợp lệ." : error.ErrorMessage))
                .Distinct()
                .ToList();

            return new BadRequestObjectResult(new
            {
                message = "Dữ liệu không hợp lệ.",
                errors
            });
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Flaw-First Review Platform API",
        Version = "v1",
        Description =
            """
            Nền tảng đánh giá sản phẩm theo hướng bóc tách ưu điểm và hạn chế thực tế.

            Development: bỏ trống JWT thì MockAuthMiddleware đăng nhập user@flawfirst.local.
            Gửi header X-Mock-Role: Admin hoặc Authorization: Bearer mock-admin để vào vai quản trị.

            Tài khoản seed:
            - admin@flawfirst.local / Admin@123
            - user@flawfirst.local / User@123
            - binh.tran@flawfirst.local / User@123
            - chi.le@flawfirst.local / User@123

            Token JWT hạn 7 ngày, chứa NameIdentifier, Email và Role.
            """
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT theo dạng: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlName = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("FlawFirst.Api");

        int status;
        string message;
        string? detail = null;

        if (exception is AppException appException)
        {
            status = appException.StatusCode;
            message = appException.Message;
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
            message = "Đã xảy ra lỗi hệ thống.";
            logger.LogError(exception, "Unhandled exception");
            if (environment.IsDevelopment())
                detail = exception?.Message;
        }

        context.Response.StatusCode = status;
        var payload = new Dictionary<string, object?> { ["message"] = message };
        if (detail != null)
            payload["detail"] = detail;

        await context.Response.WriteAsJsonAsync(payload);
    });
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Flaw-First API v1");
    options.DocumentTitle = "Flaw-First Review Platform";
});

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();

if (app.Environment.IsDevelopment())
    app.UseMiddleware<MockAuthMiddleware>();

app.UseMiddleware<LockedAccountMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "FlawFirst.Api" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
        logger.LogInformation("FlawFirstDb đã sẵn sàng.");
    }
    catch (Exception exception)
    {
        logger.LogCritical(exception, "Không migrate được SQL Server. Hãy mở SSMS và kiểm tra connection string FlawFirstDb.");
        throw;
    }
}

app.Run();
