# CreateEntityClass
Entityクラス作成プログラム (EntityFramework6.Npgsql対応)

## 使い方
CreateEntityClass.exeを実行すると、対話形式でPostgreSQLの接続先や、出力するEntityクラスの名前空間を聞かれます。全て入力するとクラスファイルの生成が行われます。EXEと同じ階層に.csが出力されます。

~~~
>CreateEntityClass.exe
全テーブルのEntityクラス作成プログラム(EntityFramework6.Npgsql版) 接続先DBの情報を入力して下さい。
Server: localhost
Port(5432):
Database: testdb
Schema(dbo):
User: postgres
Password: postgres
生成するEntityクラスの名前空間を入力して下さい。
namespace: Entities
Server=localhost;Port=5432;Database=testdb;User Id=postgres;Password=postgres;
Schema:dbo
namespace:Entities
~~~
