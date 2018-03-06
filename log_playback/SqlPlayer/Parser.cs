using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SqlPlayer
{
    public class Parser
    {
        private string _separator = "//ENTRY";
        private string _lastLine = null;

        protected LogEntry ParseHeader(string header)
        {
            var result = new LogEntry();
            if (header.Length == 0)
                return result;
            var info = header.Substring(_separator.Length);
            var stackIndex = info.IndexOf("STACK=");
            if (stackIndex >= 0)
            {
                result.Stack = info.Substring(stackIndex);
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

        public LogEntry ParseNext(StreamReader reader)
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

            var result = this.ParseHeader(_lastLine);
            while (true)
            {
                var line = reader.ReadLine();
                if (line.StartsWith(_separator))
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
