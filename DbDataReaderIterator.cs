using System.Data.Common;

namespace iPhoneBackupMessagesRenderer;

// Allows us to pick up columns from a DbDataReader without having to keep track of row numbers
ref struct DbDataReaderIterator(DbDataReader reader)
{
    private readonly DbDataReader reader = reader;
    private int col = 0;
    
    public int NextInt32() => reader.GetInt32(col++);
    public long NextInt64() => reader.GetInt64(col++);
    public bool NextBool() => reader.GetInt32(col++) == 1;
    public string NextString() => reader.GetString(col++);

    public long? NextNullableInt64()
    {
        var c = col++;
        return reader.IsDBNull(c) ? null : reader.GetInt64(c);   
    }
    
    public bool? NextNullableBool()
    {
        var c = col++;
        return reader.IsDBNull(c) ? null : reader.GetInt32(c) == 1;   
    }
    
    public string? NextNullableString()
    {
        var c = col++;
        return reader.IsDBNull(c) ? null : reader.GetString(c);   
    }
}