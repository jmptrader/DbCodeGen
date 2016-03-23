using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.CodeGen
{
    internal class CodeBulderForTablefParameters : CodeBuilder
    {
        internal CodeBulderForTablefParameters(DbConnection connection, Config config) : base(connection, config)
        {

        }
        protected override void GenerateTables()
        {
            var schemas = GetSchemaDetails();
            IEnumerable<DataRow> columns = GetRowDetails();

            foreach (IGrouping<string, DataRow> schema in schemas)
            {
                Line($"public static class {schema.Key}");
                Line("{");

                foreach (DataRow table in schema)
                {
                    var tableName = (string)table[2];
                    
                    Line($@"public const string {tableName}_Table = ""[{schema.Key}].[{tableName}]"";");
                    GenearateClass(schema.Key, tableName, columns);
                }

                Line("}");
            }
        }

        protected override void GenearateClass(string schema, string table, IEnumerable<DataRow> columns)
        {
            var tableColumns = from column in columns
                               where column.Field<string>(1) == schema && column.Field<string>(2) == table
                               orderby column.Field<int>(4)
                               select column.Field<string>(3);
            if (!table.ToLower().Contains("sys"))
            {

                Line("");
                Line($"public static class {table}");
                Line("{");
                foreach (string tableColumn in tableColumns)
                    Line($@"public const string {tableColumn} = ""[{tableColumn}]"";");
                Line("}");
            }

        }
    }
}
