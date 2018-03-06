using System;
using System.Collections.Generic;
using System.Text;

namespace SqlPlayer
{
    public class LogEntry
    {
        public DateTime Time;
        public float OriginalDuration = 0;
        public int Index;
        public string Data;
        public string Type;
        public string Stack;

        public float Duration = 0;
    }

}
