using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using model;
using model.properties;
using UnityEngine;
using util;
using Logger = util.Logger;

namespace tests
{
    public class Benchmark
    {
        private readonly DataHandler _dataHandler;
        private readonly Preset _preset;
        private List<PerformanceReport.InputInfo> _inputInfos;
        private List<PerformanceReport> _reports;
        private bool _done;

        public Benchmark(DataHandler dataHandler, Preset preset, List<PerformanceReport.InputInfo> inputInfos)
        {
            _dataHandler = dataHandler;
            _preset = preset;
            _inputInfos = inputInfos;
        }

        private bool Ready()
        {
            try
            {
                if (_dataHandler == null)
                    throw new InvalidOperationException($"{_dataHandler.GetType().Name} must be set.");
                if (_preset == null)
                    throw new InvalidOperationException($"{nameof(Preset)} must be set.");
                if (_inputInfos == null)
                    throw new InvalidOperationException($"{nameof(_inputInfos)} must be set.");
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e);
                return false;
            }

            if (_inputInfos.Count == 0) return false;
            return true;
        }
        
        /// <summary>
        /// Run benchmark in DataHandler.
        /// </summary>
        /// <param name="write">Toggle to write the computation results to disk.</param>
        public Benchmark Run(bool write = true)
        {
            if (!Ready()) return this;

            var clipBackup = Settings.flags.clipAngles;
            Settings.flags.clipAngles = false;
            
            _reports = new List<PerformanceReport>();
            var logger = new Logger()
                .SetPrintFilter(new List<Logger.EventType> {Logger.EventType.Warning, Logger.EventType.Performance})
                .SetWriteFilter(new List<Logger.EventType> {Logger.EventType.Warning});

            foreach (var inputInfo in _inputInfos)
            {
                // set preset fields
                Adjust(inputInfo);
                var angles = MathTools.LinSpace1D(0.0, 180.0, inputInfo.n, false);
                
                var report = new PerformanceReport(inputInfo);
                _dataHandler.Compute(_preset, logger, report, write, angles);
                _reports.Add(_dataHandler.GetPerformanceReport());
                logger.Log(Logger.EventType.Performance, _dataHandler.GetPerformanceReport().ToString());
            }
            _done = true;
            Settings.flags.clipAngles = clipBackup;
            
            return this;
        }
        
        /// <summary>
        /// Adjust the preset to the given input info, and vice-versa.
        /// </summary>
        /// <param name="inputInfo"></param>
        private void Adjust(PerformanceReport.InputInfo inputInfo)
        {
            _preset.properties.absorption.mode = (AbsorptionProperties.Mode) inputInfo.mode;
            _preset.properties.sample.gridResolution = inputInfo.res;

            switch ((AbsorptionProperties.Mode) inputInfo.mode)
            {
                case AbsorptionProperties.Mode.Point:
                    inputInfo.m = 1;
                    inputInfo.k = 1; 
                    break;
                case AbsorptionProperties.Mode.Area:
                    _preset.properties.detector.resolution.x = inputInfo.n;
                    _preset.properties.detector.resolution.y = inputInfo.m;
                    inputInfo.k = 1;
                    break;
                case AbsorptionProperties.Mode.Integrated:
                    inputInfo.m = 1;
                    _preset.properties.angle.angleCount = inputInfo.k;
                    break;
            }
        }

        public Benchmark Save()
        {
            if (!_done) return this;
            SaveResults();
            return this;
        }

        private void SaveResults(string separator = "\t")
        {
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), Settings.DefaultValues.BenchmarkFolderName);
            Directory.CreateDirectory(saveDir);
            var path = Path.Combine(saveDir, $"{DateTime.Now:yyyy-MM-ddTHH.mm.ss} " + Settings.DefaultValues.BenchmarkFileName);

            var rows = new List<string> {string.Join(separator, _reports[0].headRow)};
            rows.AddRange(_reports
                .Select(r => string.Join(separator, r.MakeRow()))
                .ToList()
            );
            
            using (var fileStream = File.Create(path))
            using (var buffered = new BufferedStream(fileStream))
            using (var writer = new StreamWriter(buffered))
            {
                foreach (var row in rows) 
                    writer.WriteLine(row);
            }
        }
    }
}