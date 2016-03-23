using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Database.CodeGen
{
    internal sealed class EntityCodeBuilder : CodeBuilder
    {
        internal EntityCodeBuilder(DbConnection connection, Config config) : base(connection, config)
        {
        }

        protected override void GenerateCode()
        {
            ILookup<string, Table> schemas = GetSchemaDetails();
            IEnumerable<Column> columns = GetColumnDetails(getIndexDetails: true);
            foreach (IGrouping<string, Table> schema in schemas)
            {
                foreach (Table table in schema)
                    GenerateClassFor(schema.Key, table.Name, columns);
            }
        }

        private void GenerateClassFor(string schema, string table, IEnumerable<Column> columns)
        {
            IEnumerable<Column> tableColumns =
                from col in columns
                where col.Schema == schema && col.Table == table
                select col;

            Line("");
            Line($"public sealed class {table}Entity");
            Line("{");
            foreach (Column tableColumn in tableColumns)
            {
                bool isNullable = tableColumn.IsNullable ||
                    (tableColumn as IndexedColumn)?.IndexKind == IndexKind.PrimaryKey;
                string nullChar = tableColumn.Type.IsValueType && isNullable ? "?" : string.Empty;
                Line($"public {tableColumn.Type.FullName}{nullChar} {tableColumn.Name} {{ get; set }}");
                if (tableColumn.Type == typeof (object))
                    Line($"--- {tableColumn.DbType}");
            }
            Line("}");
        }
    }
}
