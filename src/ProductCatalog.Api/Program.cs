
using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CatalogDbConnection")));

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Create the database if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    dbContext.Database.EnsureCreated();
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "ProductCatalog.Api"
}));

// Get all products
app.MapGet("/api/products", async (AppDbContext db) =>
{
    var products = await db.Products.ToListAsync();
    return Results.Ok(products);
});

// Add a product
app.MapPost("/api/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();
