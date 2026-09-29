using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using probkibiologiczne.Models;

namespace probkibiologiczne.Services;

public class RepozytoriumProbek
{
    private readonly string _dbPath;

    public RepozytoriumProbek(string? dbPath = null)
    {
        _dbPath = dbPath ?? "probki.db";
        Initialize();
    }

    private SqliteConnection GetConnection()
    {
        var cs = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
        return new SqliteConnection(cs);
    }

    public void Initialize()
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Probki (
            Id TEXT PRIMARY KEY,
            Nazwa TEXT NOT NULL,
            Typ INTEGER NOT NULL,
            DataPobrania TEXT NOT NULL,
            Opis TEXT
        );";
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<Probka> GetAll()
    {
        var list = new List<Probka>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Nazwa, Typ, DataPobrania, Opis FROM Probki ORDER BY DataPobrania DESC";
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            list.Add(new Probka
            {
                Id = rdr.GetString(0),
                Nazwa = rdr.GetString(1),
                Typ = (TypProbki)rdr.GetInt32(2),
                DataPobrania = DateTime.Parse(rdr.GetString(3)),
                Opis = rdr.IsDBNull(4) ? string.Empty : rdr.GetString(4)
            });
        }
        return list;
    }

    public void Add(Probka s)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Probki (Id, Nazwa, Typ, DataPobrania, Opis) VALUES ($id,$nazwa,$typ,$data,$opis)";
        cmd.Parameters.AddWithValue("$id", s.Id);
        cmd.Parameters.AddWithValue("$nazwa", s.Nazwa);
        cmd.Parameters.AddWithValue("$typ", (int)s.Typ);
        cmd.Parameters.AddWithValue("$data", s.DataPobrania.ToString("o"));
        cmd.Parameters.AddWithValue("$opis", s.Opis ?? string.Empty);
        cmd.ExecuteNonQuery();
    }

    public void Update(Probka s)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Probki SET Nazwa=$nazwa, Typ=$typ, DataPobrania=$data, Opis=$opis WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", s.Id);
        cmd.Parameters.AddWithValue("$nazwa", s.Nazwa);
        cmd.Parameters.AddWithValue("$typ", (int)s.Typ);
        cmd.Parameters.AddWithValue("$data", s.DataPobrania.ToString("o"));
        cmd.Parameters.AddWithValue("$opis", s.Opis ?? string.Empty);
        cmd.ExecuteNonQuery();
    }

    public void Delete(string id)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Probki WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<Probka> Search(string? query, TypProbki? typeFilter)
    {
        var list = new List<Probka>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        var where = "";
        if (!string.IsNullOrWhiteSpace(query))
        {
            where = "WHERE (Nazwa LIKE $q OR Opis LIKE $q)";
            cmd.Parameters.AddWithValue("$q", $"%{query}%");
        }
        if (typeFilter != null)
        {
            where += string.IsNullOrEmpty(where) ? "WHERE " : " AND ";
            where += "Typ = $typ";
            cmd.Parameters.AddWithValue("$typ", (int)typeFilter.Value);
        }
        cmd.CommandText = $"SELECT Id, Nazwa, Typ, DataPobrania, Opis FROM Probki {where} ORDER BY DataPobrania DESC";
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            list.Add(new Probka
            {
                Id = rdr.GetString(0),
                Nazwa = rdr.GetString(1),
                Typ = (TypProbki)rdr.GetInt32(2),
                DataPobrania = DateTime.Parse(rdr.GetString(3)),
                Opis = rdr.IsDBNull(4) ? string.Empty : rdr.GetString(4)
            });
        }
        return list;
    }
}
