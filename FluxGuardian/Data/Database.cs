using FluxGuardian.Models;
using LiteDB;

namespace FluxGuardian.Data;

public static class Database
{
    private static readonly LiteDatabase _database;

    static Database()
    {
        _database = new LiteDatabase("data.db");
        _database.Checkpoint();
        _database.Rebuild();
    }

    public static ILiteCollection<User> Users => _database.GetCollection<User>("users");
}