using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
            var columns = _connection.GetSchema("Columns")
                .AsEnumerable()
                .OrderBy(row => (string)row[0])
                .ThenBy(row => (string)row[1])
                .ThenBy(row => (string)row[2])
                .ThenBy(row => (int)row[4]);

            foreach (IGrouping<string, DataRow> schema in schemas)
            {
                Line($"public static class {schema.Key}").Line("{");
                foreach (DataRow table in schema)
                {
                    string tableName = (string)table[2];
                    Line($@"public const string {tableName}_Table = ""[{schema.Key}].[{tableName}]"";");

                    GenerateColumns(schema.Key, tableName, columns);
                }
                Line("}");
            }
        }

        private void GenerateColumns(string schema, string table, IEnumerable<DataRow> columns)
        {
            var tableColumns = from column in columns
                               where column.Field<string>(1) == schema && column.Field<string>(2) == table
                               orderby column.Field<int>(4)
                               select column.Field<string>(3);

            Line($"public static class {table}").Line("{");
            foreach (string tableColumn in tableColumns)
                Line($@"public const string {tableColumn} = ""[{tableColumn}]"";");
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