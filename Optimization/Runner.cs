﻿using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace Optimization
{

    public class Runner : MarshalByRefObject
    {

        private BacktestingResultHandler _resultsHandler;
        IOptimizerConfiguration _config;

        public Dictionary<string, string> Run(Dictionary<string, object> items)
        {
            string plain = string.Join(",", items.Select(s => s.Value));

            Dictionary<string, Dictionary<string, string>> results = AppDomainManager.GetResults(AppDomain.CurrentDomain);
            _config = AppDomainManager.GetConfig(AppDomain.CurrentDomain);

            if (results.ContainsKey(plain))
            {
                return results[plain];
            }

            foreach (var pair in items)
            {
                Config.Set(pair.Key, pair.Value.ToString());
            }

            LaunchLean();

            results.Add(plain, _resultsHandler.FinalStatistics);
            AppDomainManager.SetResults(AppDomain.CurrentDomain, results);

            return _resultsHandler.FinalStatistics;
        }

        private void LaunchLean()
        {
            Config.Set("environment", "backtesting");

            if (!string.IsNullOrEmpty(_config.AlgorithmTypeName))
            {
                Config.Set("algorithm-type-name", _config.AlgorithmTypeName);
            }

            if (!string.IsNullOrEmpty(_config.AlgorithmLocation))
            {
                Config.Set("algorithm-location", Path.GetFileName(_config.AlgorithmLocation));
            }

            if (!string.IsNullOrEmpty(_config.DataFolder))
            {
                Config.Set("data-folder", _config.DataFolder);
            }

            if (_config.StartDate.HasValue)
            {
                Config.Set("startDate", _config.StartDate.Value.ToString("O"));
            }

            if (_config.EndDate.HasValue)
            {
                Config.Set("endDate", _config.EndDate.Value.ToString("O"));
            }

            var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            systemHandlers.Initialize();

            var logFileName = "log_" + Guid.NewGuid().ToString() + ".txt";
            var logHandlers = new ILogHandler[] { new FileLogHandler(logFileName, true) };

            using (Log.LogHandler = new CompositeLogHandler(logHandlers))
            {
                LeanEngineAlgorithmHandlers leanEngineAlgorithmHandlers;
                try
                {
                    leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
                    _resultsHandler = (BacktestingResultHandler)leanEngineAlgorithmHandlers.Results;
                }
                catch (CompositionException compositionException)
                {
                    Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                    throw;
                }
                string algorithmPath;
                AlgorithmNodePacket job = systemHandlers.JobQueue.NextJob(out algorithmPath);
                try
                {
                    var _engine = new Engine(systemHandlers, leanEngineAlgorithmHandlers, Config.GetBool("live-mode"));
                    _engine.Run(job, algorithmPath);
                }
                finally
                {
                    Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);

                    // clean up resources
                    systemHandlers.Dispose();
                    leanEngineAlgorithmHandlers.Dispose();
                }
            }
        }

    }
}
