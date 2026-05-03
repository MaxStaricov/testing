using Domain.DTOs;
using Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;

namespace AppHost.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products");

        group.MapGet("/", async (string? query, string? category, string? necessity, string? flags, string? sort, IProductProvider provider) =>
        {
            var catList = category != null ? new List<string>(category.Split(',')) : null;
            var flagList = flags != null ? new List<string>(flags.Split(',')) : null;
            var res = await provider.GetAllProductsAsync(query, catList, flagList, necessity, sort);
            return Results.Ok(res);
        });

        group.MapGet("/{id:guid}", async (Guid id, IProductProvider provider) =>
        {
            var res = await provider.GetProductAsync(id);
            return res != null ? Results.Ok(res) : Results.NotFound();
        });

        group.MapPost("/", async (ProductCreateDto dto, IProductProvider provider) =>
        {
            try
            {
                var res = await provider.CreateProductAsync(dto);
                return Results.Created($"/api/products/{res.Id}", res);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPut("/{id:guid}", async (Guid id, ProductUpdateDto dto, IProductProvider provider) =>
        {
            try
            {
                var res = await provider.UpdateProductAsync(id, dto);
                return Results.Ok(res);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:guid}", async (Guid id, IProductProvider provider) =>
        {
            try
            {
                await provider.DeleteProductAsync(id);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
    }
}
