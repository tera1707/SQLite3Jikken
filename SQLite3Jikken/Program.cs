using Microsoft.EntityFrameworkCore;
using PipeJikken;
using System.Diagnostics;

Console.WriteLine("ReadWrite");

// SQLite3Jikken_Readonly.exeを起動する
LaunchReadonlyProgram();

// 別プロセス側が立ち上がるのを待つ
Console.WriteLine("別アプリ起動後10秒待って、名前付きパイプ通信でDB更新指示を出す");
Thread.Sleep(2000);

var connectionString = "Data Source=testdb.db;Cache=Shared;";

using var context = new AppDbContext(connectionString);
using var pipe = new PipeConnect();

Console.WriteLine("WALモードを有効化");
context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

Console.WriteLine("データベースから読み取り中...\n");

var users = context.UserNames.ToList();

foreach (var user in users)
{
    Console.WriteLine($"ID: {user.Id}, Name: {user.Name}");
}

//--------------------------------------------------

// 全ユーザーのIDを変更する
context.UserNames
    .OrderByDescending(u => u.Id).ToList()
    .ForEach(user => user.Name = "User" + DateTime.Now.ToString("HHmmss") + $":{user.Id}");

// 更新をコミット
int affectedRowsForIdUpdate = context.SaveChanges();

// WALチェックポイントを実行して、変更がディスクに完全に書き込まれることを保証
//Console.WriteLine("\n--- WALチェックポイントを実行中 ---");
//context.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(FULL);");
//Console.WriteLine("✓ WALチェックポイント完了: 変更がディスクに書き込まれました");

Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));

// 別プロセスに、DB読み込みを指示
await pipe.CreateClientAsync("TestPipe", "Data Updated");

Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));

// 更新後のデータ表示
//context.UserNames.ToList().ForEach(user => Console.WriteLine($"ID: {user.Id}, Name: {user.Name}"));

// 終了
Console.ReadLine();

// ----------------

static void LaunchReadonlyProgram()
{
    Console.WriteLine("\n--- SQLite3Jikken_Readonlyを起動します ---");
    try
    {
        var readonlyExePath = @"SQLite3Jikken_Readonly.exe";

        if (File.Exists(readonlyExePath))
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = readonlyExePath,
                UseShellExecute = true,
                CreateNoWindow = false
            };

            using var process = Process.Start(processInfo);           
        }
        else
        {
            Console.WriteLine($"エラー: Readonlyプログラムが見つかりません: {readonlyExePath}");
            Console.WriteLine("SQLite3Jikken_Readonlyプロジェクトをビルドしてください。");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"エラー: Readonlyプログラムの起動に失敗しました: {ex.Message}");
    }
}
