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
                case "numeric":
                    return (isNullable == "YES") ? typeof(double?) : typeof(double);
                case "character":
                    return typeof(string);
                case "character varying":
                    return typeof(string);
                case "text":
                    return typeof(string);
                case "boolean":
                    return (isNullable == "YES") ? typeof(bool?) : typeof(bool);
                case "smallint":
                    return (isNullable == "YES") ? typeof(short?) : typeof(short);
                case "integer":
                    return (isNullable == "YES") ? typeof(int?) : typeof(int);
                case "serial":
                    return (isNullable == "YES") ? typeof(int?) : typeof(int);
                case "bigint":
                    return (isNullable == "YES") ? typeof(long?) : typeof(long);
                case "bytea":
                    return typeof(byte[]);
                case "real":
                    return (isNullable == "YES") ? typeof(double?) : typeof(double);
                case "double precision":
                    return (isNullable == "YES") ? typeof(double?) : typeof(double);
                case "date":
                    return (isNullable == "YES") ? typeof(DateTime?) : typeof(DateTime);
                case "time without time zone":
                    return (isNullable == "YES") ? typeof(DateTime?) : typeof(DateTime);
                case "timestamp with time zone":
                    return (isNullable == "YES") ? typeof(DateTime?) : typeof(DateTime);
                case "timestamp without time zone":
                    return (isNullable == "YES") ? typeof(DateTime?) : typeof(DateTime);
                default:
                    throw new ArgumentException($"想定されていない型です。PostgreSQLのデータ型: {dbDataType}");
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
        public void CreateEntityClass(string nameSpace, string tblName, List<ColumnInfo> tableInfo)
        {
            var codeCompileUnit = new CodeCompileUnit();
            var name = new CodeNamespace(nameSpace);
            CodeNamespaceImport[] codeNamespaceImports = {
                new CodeNamespaceImport("System"),
                new CodeNamespaceImport("System.ComponentModel.DataAnnotations"),
                new CodeNamespaceImport("System.ComponentModel.DataAnnotations.Schema") };
            name.Imports.AddRange(codeNamespaceImports);
            codeCompileUnit.Namespaces.Add(name);

            var classType = new CodeTypeDeclaration(tblName);
            CodeAttributeDeclaration customAttribute_Class = new CodeAttributeDeclaration(
                "Table",
                new CodeAttributeArgument(new CodePrimitiveExpression(tblName)));
            classType.CustomAttributes.Add(customAttribute_Class);

            foreach (var column in tableInfo)
            {
                string ColumnNameSource = column.Name;
                TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
                // カラム名の先頭1文字を大文字にし、変数名にする
                string ColumnNameProcessed = textInfo.ToTitleCase(ColumnNameSource);
                // フィールドの属性を定義。[Column("列名")]の定義
                CodeAttributeDeclaration customAttributeField = new CodeAttributeDeclaration(
                    "Column",
                    new CodeAttributeArgument(new CodePrimitiveExpression(ColumnNameSource)));
                // CodeMemberフィールドを作成し、プロパティを追加
                var field = new CodeMemberField
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.ScopeMask,
                    Name = $"{ ColumnNameProcessed } {{ get; set; }}",
                    Type = new CodeTypeReference(this.ConvertPgDataTypeToCsParameterType(column.DataType, column.IsNullable)),
                };
                field.CustomAttributes.Add(customAttributeField);
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
