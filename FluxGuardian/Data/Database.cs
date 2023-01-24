using FluxGuardian.Helpers;
using FluxGuardian.Models;
using FluxGuardian.Services;
using LiteDB;

namespace FluxGuardian.Data;

public class Database : IDisposable
{
    private readonly LiteDatabase _database;

    public static readonly Database DefaultInstance;

    static Database()
    {
        DefaultInstance = new Database();
        DefaultInstance.Initialize();
        DefaultInstance.RunAllMigrations();
    }

    private Database()
    {
        _database = new LiteDatabase("data.db");
    }

    private void Initialize()
    {
        _database.UtcDate = true;
        _database.Rebuild();
        Checkpoint();
    }

    public ILiteCollection<User> Users => _database.GetCollection<User>("users");

    public void Dispose()
    {
        Checkpoint();
        _database.Dispose();
    }

    private void Checkpoint()
    {
        _database.Checkpoint();
    }

    private void RunAllMigrations()
    {
        // Data migration #1
        if (_database.UserVersion == 0)
        {
            var allUsers = Users.FindAll().ToList();

            _database.BeginTrans();
            foreach (var user in allUsers)
            {
                foreach (var node in user.Nodes)
                {
                    var portSet = NodeService.FindPortSet(node.Port);
                    node.Port = portSet.Key;
                }

                Users.Update(user);
            }

            _database.Commit();

            _database.UserVersion = 1;
        }
    }
}