using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMySql<ApplicationDbContext>(builder.Configuration["Database:MySql"],
    new MySqlServerVersion(new Version(8, 0, 33)),
    options => options.EnableRetryOnFailure());
var app = builder.Build();
var configuration = app.Configuration;
ProductRepository.Init(configuration);

app.MapPost("/products", (ProductRequest productRequest, ApplicationDbContext context) =>
{
  var Category = context.Categories.Where(c => c.Id == productRequest.CategoryId).FirstOrDefault();
    var product = new Product
    {
        Code = productRequest.Code,
        Name = productRequest.Name,
        Description = productRequest.Description,
        Category = Category
    };
    context.Products.Add(product);
    context.SaveChanges();
    return Results.Created($"/products/{product.Id}", product.Id); 
});

app.MapGet("products/{code}", ([FromRoute] string code) =>
{
    var product = ProductRepository.GetBy(code);
    if (product != null)
    {
        Console.WriteLine("product found");
        return Results.Ok(product);
    }

    return Results.NotFound();

});

app.MapPut("products", (Product product) =>
{
    var productSaved = ProductRepository.GetBy(product.Code);
    productSaved.Name = product.Name;
    return Results.Ok(productSaved);
});

app.MapDelete("products/{code}", ([FromRoute] string code) =>
{
    var productSaved = ProductRepository.GetBy(code);
    ProductRepository.Remove(productSaved);
    return Results.Ok();
});

if (app.Environment.IsStaging())
    app.MapGet("/configuration/database", (IConfiguration configuration) =>
    {
        return Results.Ok($"{configuration["database:connection"]}/{configuration["database:port"]}");
    });

app.Run();
