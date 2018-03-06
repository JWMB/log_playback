﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlPlayer
{
    class ArgumentOptions
    {
        [Option('c', "conn", Required = false, HelpText = "Connection string",
            Default = @"Data Source=localhost\SQLEXPRESS;Integrated Security=True;MultipleActiveResultSets=True;Initial Catalog=tretton37jonas")]
        public string ConnectionString { get; set; }

        [Option('p', "provider", Default = "System.Data.SqlClient", HelpText = "System.Data.Common ProviderName")]
        public string ProviderName { get; set; }

        [Option(Required = false, HelpText = "Input file to be processed", Default = @"C:\temp\sqllog\log.txt")]
        public string LogPath { get; set; }

        [Option(Required = false, HelpText = "Num clients to run", Default = 1)]
        public int NumClients { get; set; }

    }
    class Program
    {
        public static void Main(string[] args) //async Task 
        {
            var result = CommandLine.Parser.Default.ParseArguments<ArgumentOptions>(args)
                .WithParsed<ArgumentOptions>(async opts => await Run(opts))
                .WithNotParsed<ArgumentOptions>((errs) => HandleParseError(errs));
            Console.ReadLine();
        }
        static void HandleParseError(IEnumerable<Error> errors)
        {
            if (System.Diagnostics.Debugger.IsAttached)
                Console.ReadKey();
        }

        static async Task Run(ArgumentOptions options)
        {
            //options = new ArgumentOptions
            //{
            //    ConnectionString = @"Data Source=localhost\SQLEXPRESS;Integrated Security=True;MultipleActiveResultSets=True;Initial Catalog=tretton37jonas",
            //    ProviderName = "System.Data.SqlClient",
            //    LogPath = @"C:\temp\sqllog\logLOCAL.txt"
            //};

            Console.WriteLine("Stopwatch is " + (System.Diagnostics.Stopwatch.IsHighResolution ? "hi-res" : "lo-res"));

            var tasks = new int[options.NumClients].Select((_, i) => {
                return RunClient(options.ConnectionString, options.ProviderName, options.LogPath, "p" + i);
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

            while (true)
            {
                try
                {
                    if (!await player.Step())
                        break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Index: " + player.CurrentEntry.Index);
                    Console.WriteLine("" + player.CurrentEntry.Data);
                }
            }
            Console.WriteLine("" + playerId + " " + player.TotalDuration + "/" + player.TotalOriginalDuration);
            executer.Dispose();
            player.Dispose();
        }
    }
}
