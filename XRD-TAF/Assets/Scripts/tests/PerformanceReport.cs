using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using model;
using model.properties;
using util;

namespace tests
{
    public class PerformanceReport
    {
        private DateTime _created;
        private DeviceInfo _deviceInfo;
        private InputInfo _inputInfo;
        private LinkedList<TimeInterval> _total, _shader, _buffer, _io;
        private Stack<TimeInterval> _intervals;
        // TODO: use stack or deque for nested intervals.

        public readonly List<string> headRow = new List<string> {
            "time-total", "time-shader", "time-buffer", "time-io",          // time
            "mode", "dim-res", "dim-n", "dim-m", "dim-k",                   // input info
            "OS", "CPU", "GPU", "RAM", "VRAM", "CPU-cores", "CPU-freq",     // device info
            "date"
        };

        public override string ToString()
        {
            return string.Format(
                "{0}({1}\n{2}\n{3}\n{4}, {5}, {6}, {7}",
                nameof(PerformanceReport),
                _created,
                _deviceInfo.ToString(),
                _inputInfo.ToString(),
                "Total: " + _total.Count + " elements",
                "Shader: " + _shader.Count + " elements",
                "Buffer: " + _buffer.Count + " elements",
                "IO: " + _io.Count + " elements"
                );
        }

        public PerformanceReport(InputInfo inputInfo)
        {
            _created = DateTime.Now;
            _deviceInfo = new DeviceInfo 
            {
                os = UnityEngine.SystemInfo.operatingSystem,
                cpu = UnityEngine.SystemInfo.processorType,
                cpuCores = UnityEngine.SystemInfo.processorCount.ToString(),
                cpuFrequency = UnityEngine.SystemInfo.processorFrequency.ToString(),
                gpu = UnityEngine.SystemInfo.graphicsDeviceName,
                vram = UnityEngine.SystemInfo.graphicsMemorySize.ToString(),
                ram = UnityEngine.SystemInfo.systemMemorySize.ToString()
            };
            _inputInfo = inputInfo;
            _total = new LinkedList<TimeInterval>();
            _shader = new LinkedList<TimeInterval>();
            _buffer = new LinkedList<TimeInterval>();
            _io = new LinkedList<TimeInterval>();
            _intervals = new Stack<TimeInterval>();
        }

        #region Objects

        /// <summary>
        /// Contains the dimensions of all data objects used, and the selected absorption mode. 
        /// </summary>
        public struct InputInfo
        {
            internal int mode;
            internal int res;
            internal int n;
            internal int m;
            internal int k;
            internal IEnumerable<string> AsList() => new List<int> {mode, res, n, m, k}.Select(i => i.ToString());

            public override string ToString()
            {
                return $"[mode={mode}, res={res}, n={n}, m={m}, k={k}]";
            }

            public static InputInfo FromPreset(Preset preset)
            {
                var inputInfo = new InputInfo();

                var nrAngles = 0;
                if (preset.properties.absorption.mode != AbsorptionProperties.Mode.Area)
                {
                    nrAngles = Parser.ImportAngles(Path.Combine(Directory.GetCurrentDirectory(),
                            Settings.DefaultValues.InputFolderName, preset.properties.angle.pathToAngleFile + ".txt"))
                        .Length;
                }

                inputInfo.mode = (int) preset.properties.absorption.mode;
                inputInfo.res = preset.properties.sample.gridResolution;
                
                switch (preset.properties.absorption.mode)
                {
                    case AbsorptionProperties.Mode.Point:
                        inputInfo.n = nrAngles;
                        inputInfo.m = 1;
                        inputInfo.k = 1; 
                        break;
                    case AbsorptionProperties.Mode.Area:
                        inputInfo.n = preset.properties.detector.resolution.x;
                        inputInfo.m = preset.properties.detector.resolution.y;
                        inputInfo.k = 1;
                        break;
                    case AbsorptionProperties.Mode.Integrated:
                        inputInfo.n = nrAngles;
                        inputInfo.m = 1;
                        inputInfo.k = preset.properties.angle.angleCount;
                        break;
                }

                return inputInfo;
            }
        }

        /// <summary>
        /// Contains all hardware and software related to performance.
        /// (except disk info, which is difficult to retrieve).
        /// </summary>
        private struct DeviceInfo
        {
            internal string os, cpu, gpu, ram, vram, cpuCores, cpuFrequency;
            internal IEnumerable<string> AsList() => new List<string> {os, cpu, gpu, ram, vram, cpuCores, cpuFrequency};

            public override string ToString()
            {
                return string.Format(
                    "DeviceInfo(OS={0}, CPU={1}, GPU={2}, RAM={3}, VRAM={4}, CPU-cores={5}, cpu-Freq={6})", os, cpu,
                    gpu, ram, vram, cpuCores, cpuFrequency);
            }
        }

        /// <summary>
        /// Represents a time interval in the code, represented by the duration it took the code to run.
        /// </summary>
        public class TimeInterval
        {
            public enum Category
            {
                Total, Shader, Buffer, IO
            }

            internal readonly Stopwatch sw;
            internal readonly Category category;

            public TimeInterval(Category category)
            {
                sw = new Stopwatch();
                this.category = category;
            }
        }

        #endregion
        
        #region Methods

        public void Record(TimeInterval.Category category, Action action)
        {
            var interval = new TimeInterval(category);
            interval.sw.Start();
            action.Invoke();
            interval.sw.Stop();
            Shelve(interval);
        }

        private void Shelve(TimeInterval interval)
        {
            switch (interval.category)
            {
                case TimeInterval.Category.Total: 
                    _total.AddLast(interval);
                    break;
                case TimeInterval.Category.Shader:
                    _shader.AddLast(interval);
                    break;
                case TimeInterval.Category.Buffer:
                    _buffer.AddLast(interval);
                    break;
                case TimeInterval.Category.IO:
                    _io.AddLast(interval);
                    break;
            }
        }

        /// <summary>
        /// Starts the stopwatch of a new TimeInterval, and adds the interval to the stack.
        /// </summary>
        public void Start(TimeInterval.Category category)
        {
            var interval = new TimeInterval(category);
            interval.sw.Start();
            _intervals.Push(interval);
        }

        /// <summary>
        /// Gets the next TimeInterval on the stack, stops the stopwatch inside, and saves the interval.
        /// </summary>
        public void Stop(TimeInterval.Category category)
        {
            var interval = _intervals.Pop();
            interval.sw.Stop();
            Shelve(interval);
        }
        
        private IEnumerable<string> GetTimes()
        {
            return new List<TimeSpan> {GetSum(_total), GetSum(_shader), GetSum(_buffer), GetSum(_io)}
                .Select(ts => ts.ToString("c"));
        }

        private static TimeSpan GetSum(IReadOnlyCollection<TimeInterval> timeEvents)
        {
            if (timeEvents.ToList().Count == 0) return TimeSpan.Zero;
            return timeEvents
                .Select(e => e.sw.Elapsed)
                .Aggregate((sum, ts) => sum.Add(ts));
        }

        /// <summary>
        /// Make a list of parameters, representing a row in a benchmark table.
        /// </summary>
        public IEnumerable<string> MakeRow()
        {
            var row = new List<string>();
            
            row.AddRange(GetTimes());
            row.AddRange(_inputInfo.AsList());
            row.AddRange(_deviceInfo.AsList());
            row.Add(_created.ToString("yyyy-MM-dd"));

            return row;
        }

        #endregion
    }
}