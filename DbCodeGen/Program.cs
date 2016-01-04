using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Database.CodeGen
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Run(args).Wait();
            Console.Read();
        }

        private static async Task Run(IReadOnlyList<string> args)
        {
            string configFilePath = GetConfigFilePath(args);
            if (configFilePath == null)
                throw new ApplicationException("Configuration file not specified.");

            Config config = ReadConfig(configFilePath);

            string outputFilePath = !string.IsNullOrWhiteSpace(config.Output) ? config.Output : Path.Combine(Directory.GetCurrentDirectory(), "DbMetadata.cs");

            Console.WriteLine($"Connection string: {config.Connection.ConnectionString}");
            Console.WriteLine($"Connection type  : {config.Connection.Type}");
            Console.WriteLine($"Output           : {outputFilePath}");

            using (DbConnection connection = await CreateConnection(config))
            {
                var builder = new CodeBuilder(connection, config);
                string code = builder.Generate();
                File.WriteAllText(outputFilePath, code);
                Console.WriteLine(code);
            }
        }

        private static string GetConfigFilePath(IReadOnlyList<string> args)
        {
            if (args.Count == 0)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Config.json");
                return File.Exists(path) ? path : null;
            }

            var file = new FileInfo(args[0]);
            return file.Exists ? file.FullName : null;
        }

        private static Config ReadConfig(string configFilePath)
        {
            using (var fs = new FileStream(configFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(fs))
            {
                var deserializer = new JsonSerializer();
                var config = (Config)deserializer.Deserialize(reader, typeof(Config));
                return config;
            }
        }

        private static async Task<DbConnection> CreateConnection(Config config)
        {
            DbProviderFactory providerFactory = DbProviderFactories.GetFactory(config.Connection.Type ?? "System.Data.SqlClient");
            DbConnection connection = providerFactory.CreateConnection();
            if (connection == null)
                throw new ApplicationException("Could not create a connection from the specified configuration.");
            connection.ConnectionString = config.Connection.ConnectionString;
            await connection.OpenAsync();
            return connection;
        }
    }

    public static class Tables
    {
        public static class dbo
        {
            public const string Roles = "[dbo].[Roles]";
        }

        public static class Roles
        {
            public const string Id = "[Id]";
            public const string Name = "[Name]";
        }
    }
}
