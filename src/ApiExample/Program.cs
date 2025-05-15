using System.Diagnostics;
using ApiExample;
using DbThing;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Host.UseSerilog((_, loggerConfig) => loggerConfig.ReadFrom.Configuration(builder.Configuration));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var repo = new DbRepository(builder.Configuration);

app.MapGet("/person", async () => await (GetListHelper<Person>("usp_get_employees", "/person")))
    .WithName("GetPeople");

app
    .MapGet("/order", async () => await (GetListHelper<Order>("usp_get_orders_with_product_details", "/order")))
    .WithName("GetOrders");

app.Run();
return;

async Task<List<T>> GetListHelper<T>(string proc, string endpoint) where T : IDbModel, new()
{
    var sw = Stopwatch.StartNew();
    var results = await repo.QueryAsync<T>(proc);
    sw.Stop();
    Log.Information("Finished call to {EndPoint}, took {Time} seconds.", endpoint, sw.Elapsed);
    return results;
}