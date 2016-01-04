using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Database.CodeGen
{
    internal sealed class CodeBuilder
    {
        private readonly DbConnection _connection;
        private readonly Config _config;

        private readonly StringBuilder _code = new StringBuilder();
        private int _indent = 0;

        internal CodeBuilder(DbConnection connection, Config config)
        {
            _connection = connection;
            _config = config;
        }

        internal string Generate()
        {
            bool hasNamespace = !string.IsNullOrWhiteSpace(_config.Code.Ns);
            if (hasNamespace)
                Line($"namespace {_config.Code.Ns}").Line("{");

            bool hasWrapper = !string.IsNullOrWhiteSpace(_config.Code.WrapperClass);
            if (hasWrapper)
                Line($"public static class {_config.Code.WrapperClass}").Line("{");

            GenerateTables();
            GenerateColumns();

            if (hasWrapper)
                Line("}");

            if (hasNamespace)
                Line("}");

            return _code.ToString();
        }

        private void GenerateTables()
        {
            var schemas = _connection.GetSchema("Tables")
                .AsEnumerable()
                .OrderBy(row => (string)row[2])
                .GroupBy(row => (string)row[1]);
            foreach (IGrouping<string, DataRow> schema in schemas)
            {
                Line($"public static class {schema.Key}").Line("{");
                foreach (DataRow table in schema)
                    Line($@"public const string {table[2]} = ""{table[2]}"";");
                Line("}");
            }
        }

        private void GenerateColumns()
        {
            List<DataRow> columns = _connection.GetSchema("Columns")
                .AsEnumerable()
                .OrderBy(row => (string)row[0])
                .ThenBy(row => (string)row[1])
                .ThenBy(row => (string)row[2])
                .ThenBy(row => (int)row[4])
                .ToList();

            string previousId = null;
            foreach (DataRow column in columns)
            {
                string currentId = $"{column[0]}.{column[1]}.{column[2]}";
                if (currentId != previousId)
                {
                    if (previousId != null)
                        Line("}");
                    Line($"public static class {column[2]}").Line("{");
                    previousId = currentId;
                }

                Line($@"public const string {column[3]} = ""{column[3]}"";");
            }
            if (columns.Count > 0)
                Line("}");
        }

        private CodeBuilder Line(string line)
        {
            if (line.Equals("}"))
                _indent -= 4;
            _code.AppendLine($"{Indent}{line}");
            if (line.Equals("{"))
                _indent += 4;
            return this;
        }

        private string Indent => new string(' ', _indent);
    }
}