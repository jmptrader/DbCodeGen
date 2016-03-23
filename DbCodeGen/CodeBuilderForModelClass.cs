using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.CodeGen
{

    internal class CodeBuilderForModelClass : CodeBuilder
    {
        internal CodeBuilderForModelClass(DbConnection connection, Config config) : base(connection, config)
        {

        }
        protected override void GenerateTables()
        {
            var schemas = GetSchemaDetails();
            IEnumerable<DataRow> columns = GetRowDetails();
            foreach (IGrouping<string, DataRow> schema in schemas)
            {
                foreach (DataRow table in schema)
                {
                    var tableName = (string)table[2];
                    GenearateClass(schema.Key, tableName, columns);
                }
            }
        }

        protected override void GenearateClass(string schema, string table, IEnumerable<DataRow> columns)
        {
            var tableColumns = from columnDetail in
                                   (from column in columns
                                    where column.Field<string>(1) == schema && column.Field<string>(2) == table
                                    orderby column.Field<int>(4) ascending
                                    select new { Name = column.Field<string>(3), Type = column.Field<string>(7) })
                               group columnDetail by columnDetail.Name into tc
                               select tc;
            if (!table.ToLower().Contains("sys"))
            {
                Line("");
                Line($"public class {table}Entity");
                Line("{");
                foreach (var tableColumn in tableColumns)
                {
                    string dataType = DataColumnType(tableColumn.ElementAt(0).Type);

                    Line($@"public {dataType}  {tableColumn.ElementAt(0).Name}");
                    LineToProperty("{ get; set; }");
                }
                Line("}");
            }
        }

    }
}
