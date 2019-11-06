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
    class Generate
    {
        /// <summary>
        /// PostgresとC#の型の紐づけ
        /// </summary>
        /// <param name="dbDataType">データベースのデータ型</param>
        /// <param name="isNullable">NOT NULLの場合「NO」。そうでない場合「YES」</param>
        /// <returns>C#で使用する変数型</returns>
        private Type ConvertPgDataTypeToCsParameterType(string dbDataType, string isNullable)
        {
            switch (dbDataType)
            {
                case "character varying":
                    return typeof(string);
                case "text":
                    return typeof(string);
                case "boolean":
                    return typeof(bool?);
                case "smallint":
                    return typeof(short?);
                case "integer":
                    return typeof(int?);
                case "serial":
                    return typeof(int?);
                case "bigint":
                    return typeof(long?);
                case "bytea":
                    return typeof(byte[]);
                case "real":
                    return typeof(double?);
                case "double precision":
                    return typeof(double?);
                case "time without time zone":
                    return typeof(DateTime?);
                case "timestamp with time zone":
                    return typeof(DateTime?);
                case "timestamp without time zone":
                    return typeof(DateTime?);
                default:
                    throw new ArgumentException($"型が不明です。データベースのデータ型: { dbDataType }");
            }
        }

        /// <summary>
        /// ｢public int id { get; set; };｣のように末尾にセミコロンが付いてしまうので削除する
        /// </summary>
        /// <param name="fileName">fileName</param>
        private void DeletePropertysEndSemicolon(string fileName)
        {
            // ファイルを読込み､波括弧末尾のセミコロンを削除
            string fileDetail = File.ReadAllText(fileName).Replace("};", "}");

            // 再度ファイルに書き出します
            using (var writer = new StreamWriter(fileName))
            {
                writer.Write(fileDetail);
            }

        }

        /// <summary>
        /// テーブルに対応するEntityクラスを作成
        /// </summary>
        /// <param name="nameSpace">作成するクラスが所属するNameSpace名</param>
        /// <param name="tblName">テーブルの名前</param>
        /// <param name="tableInfo">データベースから取得したテーブルの情報</param>
        public void GenerateEntityClass(string nameSpace, string tblName, List<TableInfo> tableInfo)
        {
            // CodeCompileUnitのインスタンスを作成
            var codeCompileUnit = new CodeCompileUnit();
            // 名前空間を追加
            var name = new CodeNamespace(nameSpace);
            codeCompileUnit.Namespaces.Add(name);
            // クラスを追加
            var classType = new CodeTypeDeclaration(tblName);
            // クラスの属性(つまりテーブル名の属性)を定義。[Table("列名")]の定義
            CodeAttributeDeclaration customAttribute_Class = new CodeAttributeDeclaration(
                "System.ComponentModel.DataAnnotations.Schema.Table",
                new CodeAttributeArgument(new CodePrimitiveExpression(tblName)));
            // クラスの属性を追加
            classType.CustomAttributes.Add(customAttribute_Class);

            foreach (var t in tableInfo)
            {
                // ColumnNameの先頭1文字を大文字にし、変数名[ColumnName_Processed]を作成
                string ColumnName_Source = t.ColumnName;
                TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
                string ColumnName_Processed = textInfo.ToTitleCase(ColumnName_Source);
                // フィールドの属性(つまり列名の属性)を定義。[Column("列名")]の定義
                CodeAttributeDeclaration customAttribute_Field = new CodeAttributeDeclaration(
                    "System.ComponentModel.DataAnnotations.Schema.Column",
                    new CodeAttributeArgument(new CodePrimitiveExpression(ColumnName_Source)));
                // CodeMemberフィールドを作成し、プロパティを追加
                var field = new CodeMemberField
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = $"{ ColumnName_Processed } {{ get; set; }}",
                    Type = new CodeTypeReference(this.ConvertPgDataTypeToCsParameterType(t.DataType, t.IsNullable)),
                };
                field.CustomAttributes.Add(customAttribute_Field);
                classType.Members.Add(field);
            }

            name.Types.Add(classType);

            var provider = new CSharpCodeProvider();
            var fileName = $"{ classType.Name }.{ provider.FileExtension }";

            // Entityクラスを出力
            using (var writer = File.CreateText(fileName))
            {
                provider.GenerateCodeFromCompileUnit(codeCompileUnit, writer, new CodeGeneratorOptions());
            }

            // 各プロパティ末尾のセミコロン削除
            this.DeletePropertysEndSemicolon(fileName);
            // 画面にファイル名を表示
            Console.WriteLine($"{fileName}が生成されました。");
        }
    }

}
