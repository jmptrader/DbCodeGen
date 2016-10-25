using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

using NodaTime;

namespace Database.CodeGen
{
    internal abstract class CodeBuilder
    {
        private readonly DbConnection _connection;
        protected Config Config { get; }
        private readonly StringBuilder _code = new StringBuilder();
        private int _indent;

        internal CodeBuilder(DbConnection connection, Config config)
        {
            _connection = connection;
            Config = config;
        }

        internal string Generate()
        {
            bool hasNamespace = !string.IsNullOrWhiteSpace(Config.Code.Ns);
            if (hasNamespace)
            {
                Line("using NodaTime;");
                Line("");
                Line($"namespace {Config.Code.Ns}").Line("{");
            }

            GenerateCode();

            if (hasNamespace)
                Line("}");

            return _code.ToString();
        }

        protected abstract void GenerateCode();

        protected CodeBuilder Line(string line)
        {
            if (line.Equals("}"))
                _indent -= 4;
            _code.AppendLine($"{Indent}{line}");
            if (line.Equals("{"))
                _indent += 4;
            return this;
        }

        private static Type GetDataColumnType(string type)
        {
            Type dataType;
            if (_directColumnMappings.TryGetValue(type, out dataType))
                return dataType;
            dataType = _containsColumnMappings
                .FirstOrDefault(kvp => type.Contains(kvp.Key))
                .Value;
            return dataType ?? typeof (object);
        }

        private static readonly Dictionary<string, Type> _directColumnMappings = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase) {
            {"bit", typeof (bool)},
            {"int", typeof (int)},
            {"tinyint", typeof (byte)},
            {"numeric", typeof (double)},
            {"decimal", typeof (double)},
            {"money", typeof (decimal)},
            {"uniqueidentifier", typeof (Guid)},
            {"binary", typeof(byte[]) },
            {"date", typeof(LocalDate) },
            {"time", typeof(LocalTime) },
            {"datetime", typeof(Instant) },
            {"datetime2", typeof(Instant) },
        };

        private static readonly Dictionary<string, Type> _containsColumnMappings = new Dictionary<string, Type> {
            {"char", typeof (string)}
        };

        private string Indent => new string(' ', _indent);

        protected ILookup<string, Table> GetSchemaDetails()
        {
            ILookup<string, Table> schemas = _connection.GetSchema("Tables")
                .AsEnumerable()
                .Where(row => !Config.Code.ExcludeSchemas.Any(s => s.Equals(row.Field<string>(1), StringComparison.OrdinalIgnoreCase)))
                .OrderBy(row => row.Field<string>(2))
                .ToLookup(row => row.Field<string>(1), row => new Table {
                    Schema = row.Field<string>(1),
                    Name = row.Field<string>(2)
                });
            return schemas;
        }

        protected IEnumerable<Column> GetColumnDetails(bool getIndexDetails)
        {
            EnumerableRowCollection<DataRow> indexColumns = null;
            if (getIndexDetails)
            {
                indexColumns = _connection.GetSchema("IndexColumns").AsEnumerable()
                    .OrderBy(row => row.Field<string>(4)) //Schema
                    .ThenBy(row => row.Field<string>(5)) //Table
                    .ThenBy(row => row.Field<int>(7)); //Ordinal
            }

            return _connection.GetSchema("Columns").AsEnumerable()
                .Select(row => {
                    Column column = null;

                    var schema = row.Field<string>(1);
                    var table = row.Field<string>(2);
                    var name = row.Field<string>(3);

                    if (getIndexDetails && indexColumns != null)
                    {
                        DataRow indexRow = indexColumns.FirstOrDefault(r =>
                            schema.Equals(r.Field<string>(4)) &&
                                table.Equals(r.Field<string>(5)) &&
                                name.Equals(r.Field<string>(6)));
                        if (indexRow != null)
                        {
                            column = new IndexedColumn {
                                IndexKind = indexRow.Field<byte>(8) == 36 ? IndexKind.PrimaryKey : IndexKind.Unique
                            };
                        }
                    }

                    if (column == null)
                        column = new Column();

                    column.Schema = schema;
                    column.Table = table;
                    column.Name = name;
                    column.Ordinal = row.Field<int>(4);
                    column.IsNullable = row.Field<string>(6).Equals("YES", StringComparison.OrdinalIgnoreCase);
                    column.Type = GetDataColumnType(row.Field<string>(7));
                    column.DbType = row.Field<string>(7);

                    return column;
                })
                .OrderBy(c => c.Schema).ThenBy(c => c.Table).ThenBy(c => c.Ordinal);
        }
    }
}
