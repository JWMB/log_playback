using System;
using System.Linq;
using System.Threading.Tasks;

namespace SqlPlayer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Stopwatch is " + (System.Diagnostics.Stopwatch.IsHighResolution ? "hi-res" : "lo-res"));

            var input = new
            {
                ConnectionString = @"Data Source=localhost\SQLEXPRESS;Integrated Security=True;MultipleActiveResultSets=True;Initial Catalog=tretton37jonas",
                ProviderName = "System.Data.SqlClient",
                LogPath = @"C:\temp\sqllog\logLOCAL.txt"
            };
            var tasks = new int[2].Select((_, i) => {
                return RunClient(input.ConnectionString, input.ProviderName, input.LogPath, "p" + i);
            });
            await Task.WhenAll(tasks);
            
            Console.WriteLine("Complete");
            Console.ReadKey();
        }

        private static async Task RunClient(string connectionString, string providerName, string logPath, string playerId = null)
        {
            var executer = SqlExecuter.Create(connectionString, providerName);
            var player = new Player(logPath, new Parser(), executer);
            player.OnPostExecute += (sender, args) => {
                if (args.Entry.Index % 100 == 1)
                    Console.WriteLine("" + playerId + " " + args.Entry.Index + " " + args.Entry.Type + ":" + args.Entry.Time);
            };

            while (await player.Step())
                ;
            Console.WriteLine("" + playerId + " " + player.TotalDuration + "/" + player.TotalOriginalDuration);
            executer.Dispose();
            player.Dispose();
        }
    }
}
