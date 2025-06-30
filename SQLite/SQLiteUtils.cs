using System;
using System.Collections.Generic;
using SQLite;
using UnityEngine;

public static class SQLiteUtils
{
    public static bool DoesTableExist(string tableName, SQLiteConnection db)
    {
        var query = "SELECT name FROM sqlite_master WHERE type='table' AND name=?";

        var result = db.ExecuteScalar<string>(query, tableName);

        return !string.IsNullOrEmpty(result);
    }
    /// <summary>
    /// Update any SQLite tables values using this function
    /// </summary>
    public static void UpdateTableDynamically(SQLiteConnection db, string tableName, Dictionary<string, object> columnsToUpdate, string whereClause, object[] whereArgs)
    {
        var setClauses = new List<string>();
        var parameters = new List<object>();

        foreach (var pair in columnsToUpdate) {
        setClauses.Add($"{pair.Key} = ?");
        parameters.Add(pair.Value);
        }

        string setClause = string.Join(", ", setClauses);
        parameters.AddRange(whereArgs);
    
        var updateTableQuery = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";
        db.Execute(updateTableQuery, parameters.ToArray());
    }
}
