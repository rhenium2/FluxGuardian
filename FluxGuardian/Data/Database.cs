using FluxGuardian.Models;
using LiteDB;

namespace FluxGuardian.Data;

public class Database : IDisposable
{
    private readonly LiteDatabase _database;

    public static readonly Database DefaultInstance;
    
    public Database()
    {
        _database = new LiteDatabase("data.db");
        _database.Rebuild();
        Checkpoint();
    }

    static Database()
    {
        DefaultInstance = new Database();
    }
    
    public ILiteCollection<User> Users => _database.GetCollection<User>("users");

    public void Dispose()
    {
        _database.Checkpoint();
        _database.Dispose();
    }

    public void Checkpoint()
    {
        _database.Checkpoint();
    }
}