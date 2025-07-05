using System.Data;
using System.Diagnostics;
using ApiExample;
using DbThing;
using DbThing.Common.Interfaces;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Host.UseSerilog((_, loggerConfig) => loggerConfig.ReadFrom.Configuration(builder.Configuration));
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DbThing Sample API",
        Version = "v1",
        Description = "Sample API to query AdventureWorks data using DbThing!",
    });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();                        
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DbThing Sample API");
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var repo = new DbRepository(builder.Configuration);

// Example of using DbThing to get objects whose properties are all primitives.
app.MapGet("/person", async () => await (GetListHelper<Person>("usp_get_employees", "/person")))
    .WithName("GetPeople");

// Example of using DbThing to get objects who has properties that are "complex" objects. 
app
    .MapGet("/order", async () => await (GetListHelper<Order>("usp_get_orders_with_product_details", "/order")))
    .WithName("GetOrders");

// Example of using DbThing with raw query text instead of a stored procedure. 
app.MapGet("/product", async () =>
{
    var sw = Stopwatch.StartNew();
    var result = await repo.QueryAsync<Product>("""
                                   SELECT 
                                   	Name, 
                                   	ProductNumber,
                                   	Color,
                                   	ListPrice
                                   FROM Production.Product
                                   """, type: CommandType.Text);
    sw.Stop();
    Log.Information("Finished call to {EndPoint}, took {Time} seconds.", "/product", sw.Elapsed);
    return result;
})
.WithName("GetProducts");

app.Run();
return;

async Task<List<T>> GetListHelper<T>(string proc, string endpoint, CommandType type=CommandType.StoredProcedure) where T : IDbModel, new()
{
    var sw = Stopwatch.StartNew();
    var results = await repo.QueryAsync<T>(proc);
    sw.Stop();
    Log.Information("Finished call to {EndPoint}, took {Time} seconds.", endpoint, sw.Elapsed);
    return results;
}