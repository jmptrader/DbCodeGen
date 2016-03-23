using System;

namespace Database.CodeGen
{
    internal sealed class Table
    {
        internal string Schema { get; set; }
        internal string Name { get; set; }
    }

    internal class Column
    {
        internal string Schema { get; set; }
        internal string Table { get; set; }
        internal string Name { get; set; }
        internal int Ordinal { get; set; }
        internal bool IsNullable { get; set; }
        internal Type Type { get; set; }
        internal string DbType { get; set; }
    }

    internal sealed class IndexedColumn : Column
    {
        internal IndexKind IndexKind { get; set; }
    }
}

namespace Database.CodeGen
{
    internal enum IndexKind
    {
        Unique,
        PrimaryKey
    }
}