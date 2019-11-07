# CreateEntityClass
Entityクラス作成プログラム (EntityFramework6.Npgsql対応)

## 使い方
実行すると、対話形式でPostgreSQLの接続先や、出力するcsの名前空間を聞かれます。入力していき最後にEnterでクラスファイルの作成が始まります。クラスファイルは、EXEと同じパスに出力されます。

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
