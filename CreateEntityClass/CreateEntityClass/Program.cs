﻿using System;
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
    /// <summary>
    /// カラム情報格納クラス
    /// </summary>
    class ColumnInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string IsNullable { get; set; }
        public bool Key { get; set; } = false;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("全テーブルのEntityクラス作成プログラム(EntityFramework6.Npgsql版) 接続先DBの情報を入力して下さい。");

            Console.Write("Server(localhost): ");
            string server = Console.ReadLine();
            if (string.Empty == server) server = "localhost";
            Console.Write("Port(5432): ");
            string port = Console.ReadLine();
            if (string.Empty == port) port = "5432";
            Console.Write("Database: ");
            string database = Console.ReadLine();
            Console.Write("Schema(dbo): ");
            string schema = Console.ReadLine();
            if (string.Empty == schema) schema = "dbo";
            Console.Write("User(postgres): ");
            string user = Console.ReadLine();
            if (string.Empty == user) user = "postgres";
            Console.Write("Password(postgres): ");
            string password = Console.ReadLine();
            if (string.Empty == password) password = "postgres";
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
                var tblNames = GetAllTablesNamesList(conn);

                foreach (var tblName in tblNames)
                {
                    List<ColumnInfo> tableInfo;
                    // テーブル情報を取得
                    tableInfo = GetColumnsInfo(database, tblName, schema, conn);
                    // Entityクラスを作成し、ファイルに保存
                    new Generate().CreateEntityClass(nameSpace, tblName, tableInfo);
                }

                conn.Close();
            }
        }

        /// <summary>
        /// 全テーブル名のリスト作成
        /// </summary>
        /// <param name="conn">コネクション</param>
        /// <returns>全テーブル名のリスト</returns>
        static List<string> GetAllTablesNamesList(NpgsqlConnection conn)
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
        /// <param name="database">情報を取得するデータベースの名前</param>
        /// <param name="tblName">情報を取得するテーブルの名前</param>
        /// <param name="schema">情報を取得するスキーマの名前</param>
        /// <param name="conn">コネクション</param>
        /// <returns>テーブル情報</returns>
        static List<ColumnInfo> GetColumnsInfo(string database, string tblName, string schema, NpgsqlConnection conn)
        {
            using (var command = conn.CreateCommand())
            {
                List<string> keyColumns = new List<string>();
                // 主キーのカラムのリストを取る
                command.CommandText = $@"
                    SELECT ccu.column_name AS COLUMN_NAME
                    FROM information_schema.table_constraints tc ,information_schema.constraint_column_usage ccu
                    WHERE tc.table_catalog='{database}' AND tc.table_name='{tblName}' AND
                          tc.constraint_type='PRIMARY KEY' AND tc.table_catalog=ccu.table_catalog AND
                          tc.table_schema=ccu.table_schema AND tc.table_name=ccu.table_name AND
                          tc.constraint_name=ccu.constraint_name";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        keyColumns.Add(reader["column_name"].ToString());
                    }
                }

                command.CommandText = $@"SELECT * FROM information_schema.columns WHERE table_name = '{tblName}' AND table_schema = '{schema}' ORDER BY ordinal_position;";
                var result = new List<ColumnInfo>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // カラム名、型、null許容 などの情報を格納
                        var columnInfo = new ColumnInfo
                        {
                            Name = reader["column_name"].ToString(),
                            DataType = reader["data_type"].ToString(),
                            IsNullable = reader["is_nullable"].ToString()
                        };
                        if (keyColumns.Contains(columnInfo.Name))
                        {
                            // 主キーの場合
                            columnInfo.Key = true;
                        }
                        result.Add(columnInfo);
                    }
                }
                return result;
            }
        }
    }

}
