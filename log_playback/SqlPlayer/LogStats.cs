using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlPlayer
{
    class LogEntryStats
    {
        public LogEntry Entry;
        public float AccumulatedDuration;
    }

    public class ChunkStats
    {
        public int Index;
        public int Count;
        public float TotalDuration;
        public float AccumulatedDuration;
        public float AppDuration;
        public float DiffAccDur;
        public float DiffAppDur;
        public string Stack;
    }
    class LogStats
    {
        public static List<ChunkStats> GatherStats(string logPath)
        {
            var rxRemoveFromStack = new System.Text.RegularExpressions.Regex(@",DbCmdWrapper.+");
            using (var logReader = new System.IO.StreamReader(logPath))
            {
                var ps = new StreamParser<LogEntry>(logReader, new ParseLog());
                var byConsecutiveCallSite = new List<List<LogEntryStats>>();
                var totalAccumulated = 0f;
                List<LogEntryStats> currentList = null;
                DateTime appStart = DateTime.MinValue;
                foreach (var item in ps)
                {
                    if (!string.IsNullOrWhiteSpace(item.Stack))
                    {
                        if (appStart == DateTime.MinValue)
                            appStart = item.Time;
                        item.Stack = rxRemoveFromStack.Replace(item.Stack, "");
                        currentList = new List<LogEntryStats>();
                        byConsecutiveCallSite.Add(currentList);
                    }
                    totalAccumulated += item.OriginalDuration;
                    currentList.Add(new LogEntryStats { Entry = item, AccumulatedDuration = totalAccumulated });
                    if (item.Index % 1000 == 1)
                        Console.WriteLine("" + item.Index);
                }
                var stats = byConsecutiveCallSite.Where(list => list.Count() > 0).Select(list => new {
                    Count = list.Count(),
                    TotalDuration = list.Sum(_ => _.Entry.OriginalDuration),
                    First = list.First()
                });
                var significantStats = stats.Where(_ => _.Count >= 20 || _.TotalDuration >= 1000).ToList();
                //Always include last stats entry (to get total time)
                if (significantStats.Count > 0 && significantStats[significantStats.Count - 1] != stats.Last())
                    significantStats.Add(stats.Last());
                var result = significantStats.Select((stat, i) => new ChunkStats {
                    Index = stat.First.Entry.Index,
                    Count = stat.Count,
                    TotalDuration = stat.TotalDuration,
                    AccumulatedDuration = stat.First.AccumulatedDuration,
                    AppDuration = (float)(stat.First.Entry.Time - appStart).TotalMilliseconds,
                    DiffAccDur = i == 0 ? 0 : stat.First.AccumulatedDuration - significantStats[i - 1].First.AccumulatedDuration,
                    DiffAppDur = i == 0 ? 0 : (float)(stat.First.Entry.Time - significantStats[i - 1].First.Entry.Time).TotalMilliseconds,
                    Stack = stat.First.Entry.Stack
                }).ToList();

                return result;
            }
        }
    }
}
