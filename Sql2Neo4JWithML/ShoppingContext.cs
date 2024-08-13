namespace Sql2Neo4JWithML
{
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


}
