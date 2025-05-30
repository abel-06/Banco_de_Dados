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
    if (productRequest.Tags != null)
    {
        product.Tags = new List<Tag>();
        foreach (var tag in productRequest.Tags)
        {
            product.Tags.Add(new Tag { Name = tag });
        }
    }
    context.Products.Add(product);
    context.SaveChanges();
    return Results.Created($"/products/{product.Id}", product.Id); 
});

app.MapGet("products/{id}", ([FromRoute] int id, ApplicationDbContext context) =>
{
    var product = context.Products
        .Include(p => p.Category)
        .Include(p => p.Tags)
        .Where(p => p.Id == id)
        .First();
    if (product != null)
    {
        Console.WriteLine("product found");
        return Results.Ok(product);
    }

    return Results.NotFound();

});

app.MapPut("products/{id}", ([FromRoute] int id, ProductRequest productRequest, ApplicationDbContext context) =>
{
    var product = context.Products
        .Include(p => p.Tags)
        .Where(p => p.Id == id).First();
    var Category = context.Categories.Where(c => c.Id == productRequest.CategoryId).FirstOrDefault();

    product.Code = productRequest.Code;
    product.Name = productRequest.Name;
    product.Description = productRequest.Description;
    product.Category = Category;
    product.Tags = new List<Tag>();
     if (productRequest.Tags != null)
    {
        product.Tags = new List<Tag>();
        foreach (var tag in productRequest.Tags)
        {
            product.Tags.Add(new Tag { Name = tag });
        }
    }

    context.SaveChanges();
    return Results.Ok();
});

app.MapDelete("products/{id}", ([FromRoute] int id, ApplicationDbContext context) =>
{
    var product = context.Products.Where(p => p.Id == id).First();
    context.Products.Remove(product);
    context.SaveChanges();
    return Results.Ok();
});

if (app.Environment.IsStaging())
    app.MapGet("/configuration/database", (IConfiguration configuration) =>
    {
        return Results.Ok($"{configuration["database:connection"]}/{configuration["database:port"]}");
    });

app.Run();
