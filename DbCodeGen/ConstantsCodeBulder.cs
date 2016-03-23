using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Database.CodeGen
{
    internal sealed class ConstantsCodeBulder : CodeBuilder
    {
        internal ConstantsCodeBulder(DbConnection connection, Config config) : base(connection, config)
        {
        }

        protected override void GenerateCode()
        {
            bool hasWrapper = !string.IsNullOrWhiteSpace(Config.Code.WrapperClass);
            if (hasWrapper)
            {
                Line($"public static class {Config.Code.WrapperClass}");
                Line("{");
            }

            ILookup<string, Table> schemas = GetSchemaDetails();
            IEnumerable<Column> columns = GetColumnDetails(getIndexDetails: false);

            foreach (IGrouping<string, Table> schema in schemas)
            {
                Line($"public static class {schema.Key}");
                Line("{");

                foreach (Table table in schema)
                {
                    Line($@"public const string {table.Name}T = ""[{schema.Key}].[{table.Name}]"";");
                    GenerateClassFor(schema.Key, table.Name, columns);
                }

                Line("}");
            }

            if (hasWrapper)
                Line("}");
        }

        private void GenerateClassFor(string schema, string table, IEnumerable<Column> columns)
        {
            IEnumerable<string> columnNames = from column in columns
                where column.Schema == schema && column.Table == table
                orderby column.Ordinal
                select column.Name;
            Line("");
            Line($"public static class {table}");
            Line("{");
            foreach (string columnName in columnNames)
                Line($@"public const string {columnName} = ""[{columnName}]"";");
            Line("}");
        }
    }
}
