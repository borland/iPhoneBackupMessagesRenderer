using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer;

public class AddressBookDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string? _myName;
    
    // we cache negative results via null here too
    private readonly Dictionary<string, string?> _cache = new();

    public AddressBookDatabase(string addressBookDbFilePath, string? myName = null)
    {
        _myName = myName;
        _connection = SqliteHelper.OpenDatabaseReadOnly(addressBookDbFilePath);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    // searchString is probably a phone number
    public string? GuessName(string searchString)
    {
        // special case things we shouldn't guess
        if (string.IsNullOrWhiteSpace(searchString)) return searchString;
        if (searchString.Equals("Me", StringComparison.CurrentCultureIgnoreCase)) return _myName ?? searchString;
        
        // We sometimes get SMS messages from short-codes like 422 which screw up the text search.
        // We only want to guess if it's a qualified phone number (e.g. starts with +64 or similar)
        // or an Email address
        if (!searchString.StartsWith("+") && !searchString.Contains("@")) return searchString;
        
        if (_cache.TryGetValue(searchString, out var value)) return value;
        
        var guessed = GuessNameInternal(searchString);
        _cache[searchString] = guessed;
        return guessed;
    }
    
    string? GuessNameInternal(string searchString)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = """
                          SELECT c0First, c1Last, c2Middle, c6Organization
                          FROM ABPersonFullTextSearch_content
                          WHERE c16Phone LIKE '%' || $searchString || '%' 
                          OR c17Email LIKE '%' || $searchString || '%'
                          """;
        cmd.Parameters.AddWithValue("$searchString", searchString);

        using var reader = cmd.ExecuteReader();
        string? firstName = null, lastName = null, middleName = null, organization = null;

        if (reader.Read()) // just read the first row, if there are multiple we have no way to rank them
        {
            var iter = new DbDataReaderIterator(reader);
            firstName = iter.NextNullableString();
            lastName = iter.NextNullableString();
            middleName = iter.NextNullableString();
            organization = iter.NextNullableString();
        }

        var guessedName = string.Join(" ",
            new[] { firstName, middleName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (!string.IsNullOrWhiteSpace(guessedName)) return guessedName;

        if (!string.IsNullOrWhiteSpace(organization)) return organization;

        return null; // couldn't match on this input
    }
}