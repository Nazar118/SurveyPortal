using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics; 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SurveyPortal.API.Data;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Concrete;
using SurveyPortal.API.Repositories.Interfaces;
using System.Text;
using SurveyPortal.API.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository ve UnitOfWork kayıtları
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<SurveyPortal.API.Services.ISurveyService, SurveyPortal.API.Services.SurveyService>();
builder.Services.AddScoped<SurveyPortal.API.Services.IQuestionService, SurveyPortal.API.Services.QuestionService>();
builder.Services.AddScoped<SurveyPortal.API.Services.IAnswerService, SurveyPortal.API.Services.AnswerService>();
builder.Services.AddScoped<SurveyPortal.API.Services.IOptionService, SurveyPortal.API.Services.OptionService>();
builder.Services.AddScoped<SurveyPortal.API.Services.IResultService, SurveyPortal.API.Services.ResultService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<SurveyPortal.API.Helpers.JwtTokenGenerator>();

builder.Services.AddAutoMapper(config =>
{
    config.AddProfile(new SurveyPortal.API.Mappings.MapProfile());
});

// --- IDENTITY AYARLARI ---
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
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
        Description = "Lütfen 'Bearer' yazıp bir boşluk bıraktıktan sonra Token'ınızı yapıştırın. \r\n\r\nÖrnek: 'Bearer eyJhbGci...'",
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

app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (contextFeature != null)
        {

            var errorResponse = new
            {
                success = false,
                message = "Sunucu tarafında beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                detail = contextFeature.Error.Message // Geliştirici olarak hatanın sebebini görebilmen için
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await SurveyPortal.API.Helpers.DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

app.Run();