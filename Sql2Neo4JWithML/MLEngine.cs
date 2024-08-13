
namespace Sql2Neo4JWithML
{
    using Microsoft.ML;
    using Microsoft.ML.Data;
    using Microsoft.ML.Trainers;
    using System.Linq;

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

}
