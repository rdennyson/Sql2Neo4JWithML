using Neo4jClient;
namespace Sql2Neo4JWithML
{
    using Neo4jClient;
    using Sql2Neo4JWithML;
    using System;
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

}

