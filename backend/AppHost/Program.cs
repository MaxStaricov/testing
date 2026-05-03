using AppHost.Endpoints;
using DB;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Infrastructure.Services;
using LinqToDB.AspNet;
using LinqToDB.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LinqToDB;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 52428800;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       "Host=localhost;Port=5432;Database=recipes;Username=postgres;Password=postgres";

builder.Services.AddLinqToDBContext<AppDbContext>((provider, options) =>
    options.UsePostgreSQL(connectionString));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IProductProvider, ProductProvider>();
builder.Services.AddScoped<IDishProvider, DishProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapProductEndpoints();
app.MapDishEndpoints();

app.Run();

// Make Program class available for testing
public partial class Program { }