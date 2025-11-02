using Npgsql;

public class PostgresDocumentPartStore : IDocumentPartStore
{
    private readonly string _connectionString;

    public PostgresDocumentPartStore(string connectionString)
    {
        _connectionString = connectionString;
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS document_parts (
                name TEXT NOT NULL,
                part_key TEXT NOT NULL,
                PRIMARY KEY (name, part_key)
            );", conn);
        cmd.ExecuteNonQuery();
    }

    public async Task<bool> Store(string name, IEnumerable<string> partKeys)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        // Replace existing mapping
        await using (var del = new NpgsqlCommand("DELETE FROM document_parts WHERE name = @name", conn, (NpgsqlTransaction)tx))
        {
            del.Parameters.AddWithValue("@name", name);
            await del.ExecuteNonQueryAsync();
        }

        foreach (var pk in partKeys)
        {
            await using var ins = new NpgsqlCommand("INSERT INTO document_parts (name, part_key) VALUES (@name, @pk)", conn, (NpgsqlTransaction)tx);
            ins.Parameters.AddWithValue("@name", name);
            ins.Parameters.AddWithValue("@pk", pk);
            await ins.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return true;
    }

    public async Task<IEnumerable<string>> Get(string name)
    {
        var list = new List<string>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT part_key FROM document_parts WHERE name = @name ORDER BY part_key", conn);
        cmd.Parameters.AddWithValue("@name", name);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }
        return list;
    }

    public async Task<bool> Remove(string name, IEnumerable<string> partKeys)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        // Remove all parts for the document name
        await using var cmd = new NpgsqlCommand("DELETE FROM document_parts WHERE name = @name", conn);
        cmd.Parameters.AddWithValue("@name", name);
        await cmd.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<IEnumerable<string>> ListNames()
    {
        var list = new List<string>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT DISTINCT name FROM document_parts ORDER BY name", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }
        return list;
    }

    public async Task<string?> GetParentDocument(string chunkKey)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // First check if the chunk key itself is a document
        await using (var cmd = new NpgsqlCommand("SELECT name FROM document_parts WHERE name = @key LIMIT 1", conn))
        {
            cmd.Parameters.AddWithValue("@key", chunkKey);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null)
            {
                return chunkKey;
            }
        }

        // Otherwise, search for documents that contain this chunk
        await using (var cmd = new NpgsqlCommand("SELECT name FROM document_parts WHERE part_key = @key LIMIT 1", conn))
        {
            cmd.Parameters.AddWithValue("@key", chunkKey);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null)
            {
                return result.ToString();
            }
        }

        return null;
    }
}
