using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using NLog;

namespace Optimization
{
    public class Program
    {
        public static void Main(string[] args)
        {
            _config = LoadConfig(args);
            File.Copy(_config.ConfigPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"), true);

            var path = _config.AlgorithmLocation;
            if (!string.IsNullOrEmpty(path))
            {
                File.Copy(path, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(path)), true);
                var pdb = path.Replace(Path.GetExtension(path), ".pdb");
                if (File.Exists(pdb))
                {
                    File.Copy(pdb, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(pdb)), true);
                }
            }

            AppDomainManager.Initialize(_config);

            var instance = (OptimizerFitness) Assembly.GetExecutingAssembly().CreateInstance(_config.FitnessTypeName,
                false, BindingFlags.Default, null,
                new[] {_config}, null, null);

            var manager = new GeneManager(_config, instance);
            manager.Start();

            Console.ReadKey();
        }

        private static OptimizerConfiguration LoadConfig(string[] args)
        {
            var path = "optimization.json";
            if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                path = args[0];
            }

            return JsonConvert.DeserializeObject<OptimizerConfiguration>(File.ReadAllText(path));
        }

        #region Declarations

        private static OptimizerConfiguration _config;
        public static Logger Logger = LogManager.GetLogger("optimizer");

        #endregion
    }
}