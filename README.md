# NostreetEntities
###  Entity Framework Service Class For C#
I used a combination of Entities Framework's C# classes to make a lightweight ORM Class that acts as a layer that creates the tables and managed intenally. The object will undergo normalization and multiple tables will be created if the object properties are custom data types. ses the string given in the constructor to determine the key for the ConnectionString in WebConfig file. DefaultConnection is the key by default.

#### Example
```C#

using NostreetEntities;

EFDBService srv = new EFDBService("SomeKeyInWebConfig");
CustomClass obj = srv.Get(9);
srv.Insert(obj);
srv.Update(obj);
srv.Delete(7);

```
