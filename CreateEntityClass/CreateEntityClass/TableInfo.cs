using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateEntityClass
{
    /// <summary>
    /// テーブル情報格納クラス
    /// </summary>
    class TableInfo
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string IsNullable { get; set; }
    }
}
