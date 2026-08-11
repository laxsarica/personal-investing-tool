using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ScreenEdge.AI;

public class ModelBuilder
{
    public static string ModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScreenerModel.zip");

    public static void TrainModel(string csvPath)
    {
        var mlContext = new MLContext(seed: 0);

        // 1. Load and prepare data
        Console.WriteLine("Loading data...");
        var data = LoadAndPreprocessData(csvPath);
        
        if (data.Count == 0)
        {
            Console.WriteLine("No valid training data found.");
            return;
        }

        IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(data);

        // Split data into training and testing sets (80/20)
        var split = mlContext.Data.TrainTestSplit(trainingDataView, testFraction: 0.2);

        // 2. Build Pipeline
        // Features we want to use: Strategy (OneHot), TimeFrame (OneHot), RsiWeekly, Volume, EntryPrice
        var pipeline = mlContext.Transforms.Conversion.ConvertType(nameof(ScreenerModelInput.IsWin), outputKind: DataKind.Boolean)
            .Append(mlContext.Transforms.Categorical.OneHotEncoding("StrategyEncoded", nameof(ScreenerModelInput.Strategy)))
            .Append(mlContext.Transforms.Categorical.OneHotEncoding("TimeFrameEncoded", nameof(ScreenerModelInput.TimeFrame)))
            .Append(mlContext.Transforms.Concatenate("Features", 
                "StrategyEncoded",
                "TimeFrameEncoded",
                nameof(ScreenerModelInput.RsiWeekly), 
                nameof(ScreenerModelInput.Volume),
                nameof(ScreenerModelInput.EntryPrice)))
            .Append(mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumnName: nameof(ScreenerModelInput.IsWin), featureColumnName: "Features"));

        // 3. Train Model
        Console.WriteLine($"Training model on {data.Count} records...");
        var model = pipeline.Fit(split.TrainSet);

        // 4. Evaluate Model
        Console.WriteLine("Evaluating model...");
        var predictions = model.Transform(split.TestSet);
        var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: nameof(ScreenerModelInput.IsWin));

        Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"Auc: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"F1Score: {metrics.F1Score:P2}");

        // 5. Save Model
        mlContext.Model.Save(model, trainingDataView.Schema, ModelPath);
        Console.WriteLine($"Model saved to: {ModelPath}");
    }

    private static List<ScreenerModelInput> LoadAndPreprocessData(string path)
    {
        var list = new List<ScreenerModelInput>();
        
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        
        string header = sr.ReadLine(); // skip header
        string line;
        
        while ((line = sr.ReadLine()) != null)
        {
            var cols = line.Split(',');
            if (cols.Length < 17) continue;

            var outcome = cols[16];
            if (outcome == "Neutral" || string.IsNullOrWhiteSpace(outcome)) continue; // skip unresolved trades

            var input = new ScreenerModelInput
            {
                Symbol = cols[0],
                Strategy = cols[1],
                TimeFrame = cols[2],
                SignalDate = cols[3],
                EntryPrice = float.TryParse(cols[4], out var ep) ? ep : 0,
                RsiDaily = float.TryParse(cols[5], out var rd) ? rd : 0,
                RsiWeekly = float.TryParse(cols[6], out var rw) ? rw : 0,
                RsiMonthly = float.TryParse(cols[7], out var rm) ? rm : 0,
                Volume = float.TryParse(cols[8], out var v) ? v : 0,
                Pattern = cols[9],
                Outcome = outcome,
                IsWin = outcome == "Win"
            };
            
            list.Add(input);
        }
        
        return list;
    }

    private static ITransformer _mlModel;
    private static MLContext _predContext = new MLContext();
    private static ThreadLocal<PredictionEngine<ScreenerModelInput, ScreenerModelOutput>> _enginePool = new ThreadLocal<PredictionEngine<ScreenerModelInput, ScreenerModelOutput>>(() =>
    {
        if (_mlModel == null)
        {
            if (!File.Exists(ModelPath)) throw new FileNotFoundException($"Model not found at {ModelPath}. Please train first.");
            _mlModel = _predContext.Model.Load(ModelPath, out _);
        }
        return _predContext.Model.CreatePredictionEngine<ScreenerModelInput, ScreenerModelOutput>(_mlModel);
    });

    public static ScreenerModelOutput Predict(ScreenerModelInput input)
    {
        try 
        {
            return _enginePool.Value.Predict(input);
        }
        catch 
        {
            return new ScreenerModelOutput { Probability = 0f };
        }
    }
}
