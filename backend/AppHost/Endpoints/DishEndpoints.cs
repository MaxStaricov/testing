using Domain.DTOs;
using Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;

namespace AppHost.Endpoints;

public static class DishEndpoints
{
    public static void MapDishEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dishes");

        group.MapGet("/", async (string? query, string? category, string? flags, IDishProvider provider) =>
        {
            var catList = category != null ? new List<string>(category.Split(',')) : null;
            var flagList = flags != null ? new List<string>(flags.Split(',')) : null;
            var res = await provider.GetAllDishesAsync(query, catList, flagList);
            return Results.Ok(res);
        });

        group.MapGet("/{id:guid}", async (Guid id, IDishProvider provider) =>
        {
            var res = await provider.GetDishAsync(id);
            return res != null ? Results.Ok(res) : Results.NotFound();
        });

        group.MapPost("/", async (DishCreateDto dto, IDishProvider provider) =>
        {
            try
            {
                var res = await provider.CreateDishAsync(dto);
                return Results.Created($"/api/dishes/{res.Id}", res);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPut("/{id:guid}", async (Guid id, DishUpdateDto dto, IDishProvider provider) =>
        {
            try
            {
                var res = await provider.UpdateDishAsync(id, dto);
                return Results.Ok(res);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:guid}", async (Guid id, IDishProvider provider) =>
        {
            await provider.DeleteDishAsync(id);
            return Results.NoContent();
        });
    }
}
