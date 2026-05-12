using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SharedContracts.Import;

public enum BulkImportFormat
{
    Csv,
    Json
}

public sealed class BulkImportColumn<T>
{
    public BulkImportColumn(
        string name,
        string displayName,
        bool isRequired,
        Action<T, string> assignValue,
        params string[] aliases)
    {
        Name = name;
        DisplayName = displayName;
        IsRequired = isRequired;
        AssignValue = assignValue;
        Aliases = aliases;
    }

    public string Name { get; }
    public string DisplayName { get; }
    public bool IsRequired { get; }
    public Action<T, string> AssignValue { get; }
    public IReadOnlyList<string> Aliases { get; }

    internal IEnumerable<string> Keys()
    {
        yield return Name;
        yield return DisplayName;
        foreach (var alias in Aliases)
        {
            yield return alias;
        }
    }
}

public sealed record BulkImportRow<T>(
    int RowNumber,
    T Item,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed record BulkImportResult<T>(IReadOnlyList<BulkImportRow<T>> Rows)
{
    public int ValidCount => Rows.Count(row => row.IsValid);
    public int ErrorCount => Rows.Count - ValidCount;
}

public static class BulkImportParser
{
    public static BulkImportResult<T> Parse<T>(
        string rawText,
        BulkImportFormat format,
        Func<T> itemFactory,
        IReadOnlyList<BulkImportColumn<T>> columns)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new BulkImportResult<T>(Array.Empty<BulkImportRow<T>>());
        }

        return format == BulkImportFormat.Json
            ? ParseJson(rawText, itemFactory, columns)
            : ParseCsv(rawText, itemFactory, columns);
    }

    private static BulkImportResult<T> ParseCsv<T>(
        string rawText,
        Func<T> itemFactory,
        IReadOnlyList<BulkImportColumn<T>> columns)
    {
        var lines = rawText.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            .Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0)
        {
            return new BulkImportResult<T>(Array.Empty<BulkImportRow<T>>());
        }

        var delimiter = DetectDelimiter(lines[0]);
        var headers = ParseCsvLine(lines[0], delimiter).Select(NormalizeKey).ToList();
        var rows = new List<BulkImportRow<T>>();

        for (var index = 1; index < lines.Count; index++)
        {
            var values = ParseCsvLine(lines[index], delimiter);
            var data = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var valueIndex = 0; valueIndex < values.Count && valueIndex < headers.Count; valueIndex++)
            {
                data[headers[valueIndex]] = values[valueIndex];
            }

            rows.Add(CreateRow(index + 1, itemFactory, columns, data));
        }

        return new BulkImportResult<T>(rows);
    }

    private static BulkImportResult<T> ParseJson<T>(
        string rawText,
        Func<T> itemFactory,
        IReadOnlyList<BulkImportColumn<T>> columns)
    {
        using var document = JsonDocument.Parse(rawText, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("El JSON ha de ser una llista d'objectes.");
        }

        var rows = new List<BulkImportRow<T>>();
        var rowNumber = 1;
        foreach (var item in document.RootElement.EnumerateArray())
        {
            rowNumber++;
            if (item.ValueKind != JsonValueKind.Object)
            {
                rows.Add(new BulkImportRow<T>(rowNumber, itemFactory(), new[] { "Cada element JSON ha de ser un objecte." }));
                continue;
            }

            var data = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in item.EnumerateObject())
            {
                data[NormalizeKey(property.Name)] = ToText(property.Value);
            }

            rows.Add(CreateRow(rowNumber, itemFactory, columns, data));
        }

        return new BulkImportResult<T>(rows);
    }

    private static BulkImportRow<T> CreateRow<T>(
        int rowNumber,
        Func<T> itemFactory,
        IReadOnlyList<BulkImportColumn<T>> columns,
        IReadOnlyDictionary<string, string> data)
    {
        var item = itemFactory();
        var errors = new List<string>();

        foreach (var column in columns)
        {
            var key = column.Keys().Select(NormalizeKey).FirstOrDefault(data.ContainsKey);
            var value = key is null ? "" : data[key].Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                if (column.IsRequired)
                {
                    errors.Add($"{column.DisplayName} es obligatori.");
                }

                continue;
            }

            try
            {
                column.AssignValue(item, value);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
            {
                errors.Add($"{column.DisplayName}: {ex.Message}");
            }
        }

        return new BulkImportRow<T>(rowNumber, item, errors);
    }

    private static char DetectDelimiter(string headerLine)
    {
        var candidates = new[] { ';', '\t', ',' };
        return candidates
            .OrderByDescending(candidate => CountDelimiter(headerLine, candidate))
            .First();
    }

    private static int CountDelimiter(string text, char delimiter)
    {
        var count = 0;
        var inQuotes = false;
        foreach (var character in text)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && character == delimiter)
            {
                count++;
            }
        }

        return count;
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (!inQuotes && character == delimiter)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        values.Add(current.ToString());
        return values;
    }

    private static string ToText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => element.GetRawText()
        };
    }

    public static string NormalizeKey(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
