using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SqlPlayer
{
    public class StreamParser<TEntry> : IEnumerable<TEntry>, IEnumerator<TEntry>
    {
        StreamReader reader;
        ParseFromStream<TEntry> parser;
        public StreamParser(StreamReader reader, ParseFromStream<TEntry> parser)
        {
            this.reader = reader;
            this.parser = parser;
        }

        public TEntry Current { get; private set; }
        object IEnumerator.Current => this.Current;
        public void Dispose()
        {
        }
        public bool MoveNext()
        {
            this.Current = this.parser.ParseNext(this.reader);
            return this.Current != null;
        }
        public void Reset()
        {
            this.reader.BaseStream.Position = 0;
        }
        public IEnumerator<TEntry> GetEnumerator()
        {
            return (IEnumerator<TEntry>)this;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public abstract class ParseFromStream<T>
    {
        public abstract T ParseNext(StreamReader reader);
    }

    public class ParseLog : ParseFromStream<LogEntry>
    {
        private System.Text.RegularExpressions.Regex _rxIgnoreLines = new System.Text.RegularExpressions.Regex(@"^//HEADER");
        //private string _fileHeader = "//HEADER";
        private string _itemSeparator = "//ENTRY";
        private string _lastLine = null;

        protected LogEntry ParseItemHeader(string header)
        {
            var result = new LogEntry();
            if (header.Length == 0)
                return result;
            var info = header.Substring(_itemSeparator.Length);
            var stackIndex = info.IndexOf("STACK=");
            if (stackIndex >= 0)
            {
                result.Stack = info.Substring(stackIndex + 6);
                info = info.Remove(stackIndex);
            }
            var props = info.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(kv => kv.Split('=').Select(_ => _.Trim()).ToList())
                .Where(kv => kv.Count == 2)
                .ToDictionary(kv => kv[0], kv => kv[1]);
            result.Time = props.ContainsKey("TIME") ? DateTime.Parse(props["TIME"]) : DateTime.MinValue;
            result.OriginalDuration = props.ContainsKey("DURATION") ? float.Parse(props["DURATION"]) : 0;
            result.Type = props.ContainsKey("TYPE") ? props["TYPE"] : "";
            result.Index = props.ContainsKey("INDEX") ? int.Parse(props["INDEX"]) : -1;
            return result;
        }

        public override LogEntry ParseNext(StreamReader reader)
        {
            if (reader.EndOfStream)
                return null;
            var sbItem = new StringBuilder();
            if (_lastLine == null)
            {
                if (_lastLine == null)
                    _lastLine = "";
                this.ParseNext(reader);
            }

            var result = this.ParseItemHeader(_lastLine);
            while (true)
            {
                var line = reader.ReadLine();
                if (_rxIgnoreLines.IsMatch(line))
                    continue;
                if (line.StartsWith(_itemSeparator))
                {
                    _lastLine = line;
                    break;
                }
                sbItem.AppendLine(line);
                if (reader.EndOfStream)
                    break;
            }
            result.Data = sbItem.ToString().Trim();
            return result;
        }
    }
}
