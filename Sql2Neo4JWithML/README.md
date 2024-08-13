### Building a Complete Data Pipeline: Migrating SQL Server Data to Neo4j and Integrating a Product Recommendation System with ML.NET

In today's data-driven world, organizations often need to leverage various databases and machine learning (ML) models to extract insights and power intelligent applications. A common scenario involves migrating relational data from SQL Server to a graph database like Neo4j and integrating an ML-based recommendation system to personalize user experiences. This article guides you through building a complete data pipeline that achieves these objectives using .NET Core, ML.NET, and Neo4j.

### 1. Introduction

Relational databases like SQL Server are excellent for managing structured data, but as the complexity of relationships between entities increases, a graph database like Neo4j becomes more appropriate. Neo4j excels at handling complex, interconnected data and can easily visualize and analyze relationships. Additionally, integrating a recommendation system enhances the ability to provide personalized suggestions to users, making your application smarter and more user-friendly.

### 2. Project Setup

Let's start by setting up a .NET Core project that will serve as the foundation for our data pipeline. This project will include dependencies for interacting with SQL Server, performing machine learning tasks, and interfacing with Neo4j.

#### Project File (`.csproj`)

Ensure your project file includes the following dependencies:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.ML" Version="2.0.0" />
    <PackageReference Include="Microsoft.ML.Recommender" Version="0.21.1" />
    <PackageReference Include="Neo4jClient" Version="4.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="7.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="7.0.0" />
  </ItemGroup>

</Project>
```

### 3. Modeling Data with Entity Framework Core

We will use Entity Framework Core to interact with SQL Server. Below is a simple data model representing customers, products, orders, and order items.

#### Data Models (`ShoppingContext.cs`)

```csharp
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

public class Customer
{
    public int CustomerID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Order
{
    public int OrderID { get; set; }
    public int CustomerID { get; set; }
    public DateTime OrderDate { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Customer Customer { get; set; }
}

public class Product
{
    public int ProductID { get; set; }
    public string ProductName { get; set; }
    public string Category { get; set; }
    public decimal Price { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int OrderItemID { get; set; }
    public int OrderID { get; set; }
    public int ProductID { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Order Order { get; set; }
    public Product Product { get; set; }
}

public class ShoppingContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Your SQL Server connection string here");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Customers
        var customers = new List<Customer>
        {
            new Customer { CustomerID = 1, FirstName = "John", LastName = "Doe" },
            new Customer { CustomerID = 2, FirstName = "Jane", LastName = "Doe" },
            new Customer { CustomerID = 3, FirstName = "Michael", LastName = "Smith" },
            new Customer { CustomerID = 4, FirstName = "Michelle", LastName = "Johnson" },
            new Customer { CustomerID = 5, FirstName = "Chris", LastName = "Evans" }
        };

        modelBuilder.Entity<Customer>().HasData(customers);

        // Seed Products
        var products = new List<Product>
        {
            new Product { ProductID = 1, ProductName = "Laptop", Category = "Electronics", Price = 999.99m },
            new Product { ProductID = 2, ProductName = "Smartphone", Category = "Electronics", Price = 699.99m },
            new Product { ProductID = 3, ProductName = "Tablet", Category = "Electronics", Price = 299.99m },
            new Product { ProductID = 4, ProductName = "Headphones", Category = "Accessories", Price = 199.99m },
            new Product { ProductID = 5, ProductName = "Smartwatch", Category = "Accessories", Price = 249.99m }
        };

        modelBuilder.Entity<Product>().HasData(products);

        // Seed Orders and OrderItems
        var orders = new List<Order>
        {
            new Order { OrderID = 1, CustomerID = 1, OrderDate = DateTime.Now.AddDays(-10) },
            new Order { OrderID = 2, CustomerID = 2, OrderDate = DateTime.Now.AddDays(-8) },
            new Order { OrderID = 3, CustomerID = 3, OrderDate = DateTime.Now.AddDays(-6) },
            new Order { OrderID = 4, CustomerID = 4, OrderDate = DateTime.Now.AddDays(-4) },
            new Order { OrderID = 5, CustomerID = 5, OrderDate = DateTime.Now.AddDays(-2) }
        };

        modelBuilder.Entity<Order>().HasData(orders);

        var orderItems = new List<OrderItem>
        {
            new OrderItem { OrderItemID = 1, OrderID = 1, ProductID = 1, Quantity = 1, UnitPrice = 999.99m },
            new OrderItem { OrderItemID = 2, OrderID = 2, ProductID = 2, Quantity = 1, UnitPrice = 699.99m },
            new OrderItem { OrderItemID = 3, OrderID = 3, ProductID = 3, Quantity = 1, UnitPrice = 299.99m },
            new OrderItem { OrderItemID = 4, OrderID = 4, ProductID = 4, Quantity = 2, UnitPrice = 199.99m },
            new OrderItem { OrderItemID = 5, OrderID = 5, ProductID = 5, Quantity = 1, UnitPrice = 249.99m }
        };

        modelBuilder.Entity<OrderItem>().HasData(orderItems);
    }
}
```

### 4. Building the Recommendation Engine with ML.NET

With the data in SQL Server, the next step is to build a recommendation engine using ML.NET's `MatrixFactorizationTrainer`. This engine will predict which product a customer is likely to purchase next based on past purchase history.

#### Recommendation Model (`MLEngine.cs`)

```csharp
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.Recommender;

public class ProductRecommendation
{
    public uint CustomerID { get; set; }
    public uint ProductID { get; set; }
    public float Label { get; set; } // Represents a purchase
}

public class ProductPrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
}

public class MLEngine
{
    private readonly MLContext _mlContext;
    private readonly ITransformer _model;

    public MLEngine()
    {
        _mlContext = new MLContext();

        // Load data from SQL Server
        var data = LoadData();

        // Train model
        _model = TrainModel(data);
    }

    private IDataView LoadData()
    {
        using (var context = new ShoppingContext())
        {
            var purchaseData = context.OrderItems
                .Select(oi => new ProductRecommendation
                {
                    CustomerID = (uint)oi.Order.CustomerID,
                    ProductID = (uint)oi.ProductID,
                    Label = 1 // Represents a purchase
                }).ToList();

            return _mlContext.Data.LoadFromEnumerable(purchaseData);
        }
    }

    private ITransformer TrainModel(IDataView data)
    {
        var options = new MatrixFactorizationTrainer.Options
        {
            MatrixColumnIndexColumnName = nameof(ProductRecommendation.CustomerID),
            MatrixRowIndexColumnName = nameof(ProductRecommendation.ProductID),
            LabelColumnName = nameof(ProductRecommendation.Label),
            NumberOfIterations = 20,
            ApproximationRank = 100,
            LossFunction = MatrixFactorizationTrainer.LossFunctionType.SquareLossOneClass,
           

 Alpha = 0.01,
            Lambda = 0.025
        };

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(nameof(ProductRecommendation.CustomerID))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey(nameof(ProductRecommendation.ProductID)))
            .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(options));

        return pipeline.Fit(data);
    }

    public float PredictProduct(uint customerId, uint productId)
    {
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<ProductRecommendation, ProductPrediction>(_model);

        var testProduct = new ProductRecommendation
        {
            CustomerID = customerId,
            ProductID = productId
        };

        var prediction = predictionEngine.Predict(testProduct);
        return prediction.Score; // Higher scores indicate a higher likelihood of purchase
    }
}
```

### 5. Migrating Data to Neo4j and Storing Recommendations

With the recommendation engine ready, the next step is to migrate all entities and relationships from SQL Server to Neo4j. Additionally, we'll store the product recommendations in Neo4j for each customer.

#### Data Migration and Recommendation (`DataMigration.cs`)

```csharp
using Neo4jClient;
using System.Linq;
using System.Threading.Tasks;

public class DataMigration
{
    private readonly IGraphClient _neo4jClient;
    private readonly MLEngine _mlEngine;

    public DataMigration()
    {
        _neo4jClient = new BoltGraphClient(new Uri("bolt://localhost:7687"), "neo4j", "your_password");
        _neo4jClient.ConnectAsync().Wait();

        _mlEngine = new MLEngine();
    }

    public async Task MigrateDataAndCreateRecommendations()
    {
        using (var context = new ShoppingContext())
        {
            // Migrate Customers
            var customers = context.Customers.ToList();
            foreach (var customer in customers)
            {
                await _neo4jClient.Cypher
                    .Merge("(c:Customer {CustomerID: $CustomerID})")
                    .OnCreate()
                    .Set("c = {CustomerID: $CustomerID, FirstName: $FirstName, LastName: $LastName}")
                    .WithParams(new
                    {
                        customer.CustomerID,
                        customer.FirstName,
                        customer.LastName
                    })
                    .ExecuteWithoutResultsAsync();
            }

            // Migrate Products
            var products = context.Products.ToList();
            foreach (var product in products)
            {
                await _neo4jClient.Cypher
                    .Merge("(p:Product {ProductID: $ProductID})")
                    .OnCreate()
                    .Set("p = {ProductID: $ProductID, ProductName: $ProductName, Category: $Category, Price: $Price}")
                    .WithParams(new
                    {
                        product.ProductID,
                        product.ProductName,
                        product.Category,
                        product.Price
                    })
                    .ExecuteWithoutResultsAsync();
            }

            // Migrate Orders and Create Relationships with Customers
            var orders = context.Orders.ToList();
            foreach (var order in orders)
            {
                await _neo4jClient.Cypher
                    .Merge("(o:Order {OrderID: $OrderID})")
                    .OnCreate()
                    .Set("o = {OrderID: $OrderID, OrderDate: $OrderDate}")
                    .WithParams(new
                    {
                        order.OrderID,
                        order.OrderDate
                    })
                    .ExecuteWithoutResultsAsync();

                // Create relationship between Customer and Order
                await _neo4jClient.Cypher
                    .Match("(c:Customer {CustomerID: $CustomerID})", "(o:Order {OrderID: $OrderID})")
                    .Create("(c)-[:PLACED]->(o)")
                    .WithParams(new
                    {
                        order.CustomerID,
                        order.OrderID
                    })
                    .ExecuteWithoutResultsAsync();
            }

            // Migrate OrderItems and Create Relationships between Orders and Products
            var orderItems = context.OrderItems.ToList();
            foreach (var orderItem in orderItems)
            {
                await _neo4jClient.Cypher
                    .Merge("(oi:OrderItem {OrderItemID: $OrderItemID})")
                    .OnCreate()
                    .Set("oi = {OrderItemID: $OrderItemID, Quantity: $Quantity, UnitPrice: $UnitPrice}")
                    .WithParams(new
                    {
                        orderItem.OrderItemID,
                        orderItem.Quantity,
                        orderItem.UnitPrice
                    })
                    .ExecuteWithoutResultsAsync();

                // Create relationship between Order and Product via OrderItem
                await _neo4jClient.Cypher
                    .Match("(o:Order {OrderID: $OrderID})", "(p:Product {ProductID: $ProductID})")
                    .Create("(o)-[:CONTAINS {Quantity: $Quantity, UnitPrice: $UnitPrice}]->(p)")
                    .WithParams(new
                    {
                        orderItem.OrderID,
                        orderItem.ProductID,
                        orderItem.Quantity,
                        orderItem.UnitPrice
                    })
                    .ExecuteWithoutResultsAsync();
            }

            // Create Recommendations for Each Customer
            foreach (var customer in customers)
            {
                float bestScore = float.MinValue;
                Product bestProduct = null;

                foreach (var product in products)
                {
                    var score = _mlEngine.PredictProduct((uint)customer.CustomerID, (uint)product.ProductID);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestProduct = product;
                    }
                }

                if (bestProduct != null)
                {
                    // Store the recommended product in Neo4j
                    await _neo4jClient.Cypher
                        .Match("(c:Customer {CustomerID: $CustomerID})", "(p:Product {ProductID: $ProductID})")
                        .Create("(c)-[:RECOMMENDED_NEXT]->(p)")
                        .WithParams(new
                        {
                            customer.CustomerID,
                            bestProduct.ProductID
                        })
                        .ExecuteWithoutResultsAsync();
                }
            }
        }
    }
}
```

### 6. Running the Data Pipeline

Finally, to execute the migration and recommendation process, modify your `Program.cs` to invoke the migration method:

#### Running the Migration (`Program.cs`)

```csharp
using System;
using System.Threading.Tasks;

namespace SqlToNeo4j
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var migration = new DataMigration();
            await migration.MigrateDataAndCreateRecommendations();

            Console.WriteLine("Data migration and recommendations completed.");
        }
    }
}
```

### 7. Execute the Application

Once everything is set up, you can run your .NET Core application to migrate data from SQL Server to Neo4j and generate product recommendations:

```bash
dotnet restore
dotnet build
dotnet run
```

### Conclusion

By following this guide, you've created a complete data pipeline that migrates relational data from SQL Server to Neo4j, integrates a recommendation engine using ML.NET, and stores the results in Neo4j. This solution allows you to leverage the strengths of both relational and graph databases while enhancing your application's intelligence with personalized product recommendations.

This approach can be extended further with more sophisticated models, larger datasets, and additional business logic, making it a solid foundation for building intelligent, data-driven applications.