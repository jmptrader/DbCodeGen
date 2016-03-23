using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Database.CodeGen
{
    internal abstract class CodeBuilder
    {
        protected readonly DbConnection _connection;
        protected readonly Config _config;
        protected readonly StringBuilder _code = new StringBuilder();
        protected int _indent = 0;

        internal CodeBuilder(DbConnection connection, Config config)
        {
            _connection = connection;
            _config = config;
        }

        internal string Generate()
        {
            bool hasNamespace = !string.IsNullOrWhiteSpace(_config.Code.Ns);
            if (hasNamespace)
            {
                Line("using NodaTime;");
                Line("");
                Line($"namespace {_config.Code.Ns}").Line("{");
            }

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

        protected abstract void GenerateTables();
        protected abstract void GenearateClass(string schema, string table, IEnumerable<DataRow> columns);

        protected CodeBuilder Line(string line)
        {
            if (line.Equals("}"))
                _indent -= 4;
            _code.AppendLine($"{Indent}{line}");
            if (line.Equals("{"))
                _indent += 4;
            return this;
        }
        protected CodeBuilder LineToProperty(string line)
        {
            _indent += 4;
            _code.AppendLine($"{Indent}{line}");
            _indent -= 4;
            return this;
        }
        protected string DataColumnType(string type)
        {
            if (type == "bit")
            {
                return ("bool");
            }
            else if (type == "decimal")
            {
                return ("float");
            }
            else if (type == "uniqueidentifier")
            {
                return ("Guid");
            }
            else if (type.Contains("date") || type.Contains("time"))
            {
                return ("DateTime");
            }
            else if (type.Contains("varchar"))
            {
                return ("string");
            }
            else
            {
                return type;
            }
        }

        protected string Indent => new string(' ', _indent);

        protected IEnumerable<DataRow> GetRowDetails()
        {
            return _connection.GetSchema("Columns")
               .AsEnumerable()
               .OrderBy(row => (string)row[0])
               .ThenBy(row => (string)row[1])
               .ThenBy(row => (string)row[2])
               .ThenBy(row => (int)row[4]);
        }
        protected IEnumerable<IGrouping<string, DataRow>> GetSchemaDetails()
        {
            return _connection.GetSchema("Tables")
                .AsEnumerable()
                .OrderBy(row => (string)row[2])
                .GroupBy(row => (string)row[1]);
        }


    }
}