using System;
using System.Threading.Tasks;

namespace Sql2Neo4JWithML
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