using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace SqlPlayer
{
    public class ExecuteEntryEventArgs : EventArgs
    {
        public LogEntry Entry { get; set; }
    }

    public class Player : IDisposable
    {
        private StreamReader _logReader;
        private Parser _parser;
        private SqlExecuter _executer;

        public List<LogEntry> ExecutedEntries = new List<LogEntry>();
        public event EventHandler<ExecuteEntryEventArgs> OnPreExecute;
        public event EventHandler<ExecuteEntryEventArgs> OnPostExecute;

        public Player(string file, Parser parser, SqlExecuter executer)
        {
            _logReader = new StreamReader(file);
            _parser = parser;
            _executer = executer;
        }

        System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        LogEntry _lastEntry = null;
        int _index = 0;
        public float TotalDuration { get; private set; } = 0;
        public float TotalOriginalDuration { get; private set; } = 0;

        public async Task<bool> Step()
        {
            var entry = _parser.ParseNext(this._logReader);
            if (entry == null)
                return false;

            _index++;
            var delay = _lastEntry != null ? (entry.Time - _lastEntry.Time).TotalMilliseconds : 0;
            _lastEntry = entry;

            //await Task.Delay((int)delay);

            OnPreExecute?.Invoke(this, new ExecuteEntryEventArgs { Entry = entry });

            _stopwatch.Reset();
            await _executer.Execute(entry.Data, _stopwatch);
            var ms = (float)Math.Round(1000f * _stopwatch.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency, 3);
            entry.Duration = ms;
            this.ExecutedEntries.Add(entry);

            OnPostExecute?.Invoke(this, new ExecuteEntryEventArgs { Entry = entry });

            this.TotalOriginalDuration += entry.OriginalDuration;
            this.TotalDuration += entry.Duration;

            //Console.WriteLine("" + entry.Duration + "/" + entry.OriginalDuration + " Total:" + this.TotalDuration + "/" + this.TotalOriginalDuration);

            return true;
        }

        public void Dispose()
        {
            if (this._logReader != null)
            {
                this._logReader.Dispose();
                this._logReader = null;
            }
        }
    }
}
