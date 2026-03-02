using Microsoft.EntityFrameworkCore; // SQL Server ve EF Core için
using SurveyPortal.API.Data;          // AppDbContext sýnýfýna eriþmek için

var builder = WebApplication.CreateBuilder(args);

// --- BURAYI EKLEDÝK ---
// Veri tabaný baðlantýsýný (SQL Server) sisteme tanýtýyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// ----------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();