using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;
using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;

namespace CreateEntityClass
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("全テーブルのEntityクラス作成プログラム(PostgreSQL版) 接続先DBの情報を入力して下さい。");

            Console.Write("Server: ");
            string server = Console.ReadLine();
            Console.Write("Port: ");
            string port = Console.ReadLine();
            Console.Write("Database: ");
            string database = Console.ReadLine();
            Console.Write("Schema: ");
            string schema = Console.ReadLine();
            Console.Write("User: ");
            string user = Console.ReadLine();
            Console.Write("Password: ");
            string password = Console.ReadLine();
            string connString = GetConnString(server, port, database, user, password);
            Console.WriteLine("生成するEntityクラスの名前空間を入力して下さい。");
            Console.Write("namespace: ");
            string nameSpace = Console.ReadLine();

            // 接続文字列を表示
            Console.WriteLine(connString);
            Console.WriteLine($"Schema:{schema}");
            Console.WriteLine($"namespace:{nameSpace}");

            // データベースからEntityクラスを作成
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                // 全テーブル名のリスト作成
                var tblNames = GetAllTablesNameList(conn);

                foreach (var tblName in tblNames)
                {
                    List<TableInfo> tableInfo;
                    // テーブル情報を取得
                    tableInfo = GetTableInfo(tblName, schema, conn);
                    // Entityクラスを作成し、ファイルに保存
                    new Generate().GenerateEntityClass(nameSpace, tblName, tableInfo);
                }

                conn.Close();
            }
        }

        /// <summary>
        /// 全テーブル名のリスト作成
        /// </summary>
        /// <param name="conn">コネクション</param>
        /// <returns>全テーブル名のリスト</returns>
        static List<string> GetAllTablesNameList(NpgsqlConnection conn)
        {
            var result = new List<string>();

            using (var command = conn.CreateCommand())
            {
                // テーブル一覧を取得します
                command.CommandText = $@"SELECT relname AS table_name FROM pg_stat_user_tables ORDER BY table_name;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        result.Add(reader["table_name"].ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// 接続文字列生成
        /// </summary>
        /// <returns>接続文字列</returns>
        static string GetConnString(string server, string port, string database, string user, string password)
        {
            var sb = new StringBuilder();
            sb.Append($"Server={server};")
            .Append($"Port={port};")
            .Append($"Database={database};")
            .Append($"User Id={user};")
            .Append($"Password={password};");
            return sb.ToString();
        }

        /// <summary>
        /// 指定したテーブル情報を取得
        /// </summary>
        /// <param name="tblName">情報を取得するテーブルの名前</param>
        /// <param name="schema">情報を取得するスキーマの名前</param>
        /// <param name="conn">コネクション</param>
        /// <returns>テーブル情報</returns>
        static List<TableInfo> GetTableInfo(string tblName, string schema, NpgsqlConnection conn)
        {
            using (var command = conn.CreateCommand())
            {
                // テーブル情報を取得
                command.CommandText = $@"SELECT * FROM information_schema.columns WHERE table_name = '{tblName}'AND table_schema='{schema}' ORDER BY ordinal_position;";
                var result = new List<TableInfo>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var tableInfo = new TableInfo
                        {
                            ColumnName = reader["column_name"].ToString(),
                            DataType = reader["data_type"].ToString()
                        };
                        result.Add(tableInfo);
                    }
                }
                return result;
            }
        }
    }

}
