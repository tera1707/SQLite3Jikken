using Microsoft.EntityFrameworkCore;
using PipeJikken;

Console.WriteLine("Read");

var connectionString = "Data Source=testdb.db;Cache=Shared;";
using var context = new AppDbContext(connectionString);
using var pipe = new PipeConnect();

//WALモードを有効化
context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

pipe.CreateServerAsync("TestPipe", (recvString) =>
{
    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));

    //DB読み込み
    foreach (var user in context.UserNames.ToList())
    {
        Console.WriteLine($"ID: {user.Id}, Name: {user.Name}");
    }
}).Wait();

Console.ReadLine();
