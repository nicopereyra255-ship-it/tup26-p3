using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

try
{
    // si no hay argumentos
    if (args.Length == 0)
    {
        Console.WriteLine("Use --help para ver las opciones");
        return;
    }

    // 1. Parseo de argumentos
    var config = ParseArgs(args);

    // 2. Lectura de entrada
    var text = ReadInput(config);

    // 3. Parseo del archivo
    var data = ParseDelimited(text, config);

    // 4. Ordenamiento
    var sorted = SortRows(data, config);

    // 5. Serialización (IMPORTANTE: headers + rows)
    var output = Serialize(data.headers, sorted, config);

    // 6. Salida
    WriteOutput(output, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}

// ===================== PARSE ARGS =====================

AppConfig ParseArgs(string[] args)
{
    string? input = null;
    string? output = null;
    string delimiter = ",";
    bool noHeader = false;
    var sortFields = new List<SortField>();

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        if (arg == "-h" || arg == "--help")
        {
            Console.WriteLine("Uso: sortx [input] [output] -b campo[:tipo[:orden]]");
            Environment.Exit(0);
        }
        else if (arg == "-i" || arg == "--input")
            input = args[++i];

        else if (arg == "-o" || arg == "--output")
            output = args[++i];

        else if (arg == "-d" || arg == "--delimiter")
        {
            var d = args[++i];
            delimiter = d == "\\t" ? "\t" : d;
        }
        else if (arg == "-nh" || arg == "--no-header")
            noHeader = true;

        else if (arg == "-b" || arg == "--by")
        {
            var parts = args[++i].Split(':');

            string name = parts[0];
            bool numeric = parts.Length > 1 && parts[1] == "num";
            bool desc = parts.Length > 2 && parts[2] == "desc";

            sortFields.Add(new SortField(name, numeric, desc));
        }
        else if (!arg.StartsWith("-"))
        {
            if (input == null)
                input = arg;
            else if (output == null)
                output = arg;
        }
    }

    if (sortFields.Count == 0)
        throw new Exception("Debe especificar al menos un campo (-b)");

    return new AppConfig(input, output, delimiter, noHeader, sortFields);
}

// ===================== READ INPUT =====================

string ReadInput(AppConfig config)
{
    if (config.InputFile != null)
        return File.ReadAllText(config.InputFile);

    return Console.In.ReadToEnd();
}

// ===================== PARSE DELIMITED =====================

(List<string> headers, List<Dictionary<string, string>> rows) ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(l => l.Trim('\r'))
        .ToList();

    List<string> headers;

    if (!config.NoHeader)
    {
        headers = lines[0].Split(config.Delimiter).ToList();
        lines.RemoveAt(0);
    }
    else
    {
        var count = lines[0].Split(config.Delimiter).Length;
        headers = Enumerable.Range(0, count)
            .Select(i => i.ToString())
            .ToList();
    }

    var rows = new List<Dictionary<string, string>>();

    foreach (var line in lines)
    {
        var values = line.Split(config.Delimiter);
        var dict = new Dictionary<string, string>();

        for (int i = 0; i < headers.Count; i++)
        {
            dict[headers[i]] = i < values.Length ? values[i] : "";
        }

        rows.Add(dict);
    }

    return (headers, rows);
}



List<Dictionary<string, string>> SortRows(
    (List<string> headers, List<Dictionary<string, string>> rows) data,
    AppConfig config)
{
    var rows = data.rows;

    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    foreach (var field in config.SortFields)
    {
        // VALIDACIÓN DE CAMPO
        if (!data.headers.Contains(field.Name))
            throw new Exception($"Campo inexistente: {field.Name}");

        Func<Dictionary<string, string>, object> keySelector = row =>
        {
            var value = row.ContainsKey(field.Name) ? row[field.Name] : "";

            if (field.Numeric && double.TryParse(value, out var num))
                return num;

            return value;
        };

        if (ordered == null)
        {
            ordered = field.Descending
                ? rows.OrderByDescending(keySelector)
                : rows.OrderBy(keySelector);
        }
        else
        {
            ordered = field.Descending
                ? ordered.ThenByDescending(keySelector)
                : ordered.ThenBy(keySelector);
        }
    }

    return ordered?.ToList() ?? rows;
}



string Serialize(
    List<string> headers,
    List<Dictionary<string, string>> rows,
    AppConfig config)
{
    var lines = new List<string>();

    if (!config.NoHeader)
    {
        lines.Add(string.Join(config.Delimiter, headers));
    }

    foreach (var row in rows)
    {
        var values = headers.Select(h => row.ContainsKey(h) ? row[h] : "");
        lines.Add(string.Join(config.Delimiter, values));
    }

    return string.Join("\n", lines);
}



void WriteOutput(string output, AppConfig config)
{
    if (config.OutputFile != null)
    {
        File.WriteAllText(config.OutputFile, output);
    }
    else
    {
        Console.WriteLine(output);
    }
}



record SortField(string Name, bool Numeric, bool Descending);

record AppConfig(
    string? InputFile,
    string? OutputFile,
    string Delimiter,
    bool NoHeader,
    List<SortField> SortFields
);






