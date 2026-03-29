using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SurveyPortal.API.Models;
using Microsoft.EntityFrameworkCore; // SQL Server ve EF Core için
using Microsoft.Extensions.DependencyInjection;
using SurveyPortal.API.Data;          // AppDbContext sýnýfýna eriþmek için
using SurveyPortal.API.Repositories.Concrete;
using SurveyPortal.API.Repositories.Interfaces;
using Microsoft.OpenApi.Models; // EKLENDÝ: Swagger Kilit butonu için gerekli kütüphane

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository ve UnitOfWork kayýtlarý (Dependency Injection)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<SurveyPortal.API.Services.ISurveyService, SurveyPortal.API.Services.SurveyService>();
builder.Services.AddScoped<SurveyPortal.API.Services.IQuestionService, SurveyPortal.API.Services.QuestionService>();
builder.Services.AddScoped<SurveyPortal.API.Services.IAnswerService, SurveyPortal.API.Services.AnswerService>();
builder.Services.AddScoped<SurveyPortal.API.Services.IOptionService, SurveyPortal.API.Services.OptionService>();
builder.Services.AddScoped<SurveyPortal.API.Helpers.JwtTokenGenerator>();

builder.Services.AddAutoMapper(config =>
{
    config.AddProfile(new SurveyPortal.API.Mappings.MapProfile());
});

// --- IDENTITY AYARLARI ---
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = true; // Þifrede rakam zorunlu
    options.Password.RequireLowercase = true; // Þifrede küçük harf zorunlu
    options.Password.RequireUppercase = true; // Þifrede büyük harf zorunlu
    options.Password.RequiredLength = 6; // Minimum 6 karakter
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// --- JWT AUTHENTICATION AYARLARI ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Lütfen 'Bearer' yazýp bir boþluk býraktýktan sonra Token'ýnýzý yapýþtýrýn. \r\n\r\nÖrnek: 'Bearer eyJhbGci...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await SurveyPortal.API.Helpers.DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

app.Run();