using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


try
{
    // validamos que contenga parametros
    if(args.Length == 0)
    {
        Console.WriteLine("Debe especificar un parametro");
        return;
    }
    // lee y procesa los argumentos que el usuario ingreso por consola
     var config = ParseArgs(args); 
    //lee el contenido del archivo
     var text = ReadInput(config);
    // convierte el texto en una estructura de datos (headers y filas)
    
    var data = ParseDelimited(text, config);

     //ordena las filas segun los criterios indicados
     var sorted = SortRows(data, config);

     // convierte las filas ordenadas nuevamente a texto
     var output = Serialize(sorted, config);

     // muestra el resultado en consola o lo guarda en un archivo
     WriteOutput(output, config);

}
catch (Exception ex){
    //muestra mensaje de error en caso de que exista
    Console.Error.WriteLine(ex.Message);
}

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

        if (arg == "-i" || arg == "--input")
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
string ReadInput (AppConfig config)
{
    if (config.InputFile !=null)
    return File.ReadAllText(config.InputFile);

    return Console.In.ReadToEnd();
}
(List<string> headers, List<Dictionary<string, string>> rows) ParseDelimited(string text, AppConfig config)
{
    var lines = text.Split('\n',StringSplitOptions.RemoveEmptyEntries)
    .Select(i => i.Trim('\r'))  
    .ToList() ;
    
    List<string> headers;
    if (!config.NoHeader)
    {
        headers = lines [0].Split(config.Delimiter).ToList();
        lines.RemoveAt(0); 
    }
    else
    {
        var count = lines [0].Split(config.Delimiter).Length;
        headers=Enumerable.Range(0, count)
        .Select(i => i.ToString())
        .ToList();
    }
    var rows = new List<Dictionary<string,string>>();
    foreach(var line in lines)
    {
        var values = line.Split(config.Delimiter);
        var dict = new Dictionary<string,string>();
        for (int i=0;i < headers.Count; i++)
        {
             dict[headers[i]] = i < values.Length ? values[i] : "";
        }
        rows.Add(dict);

    }
    return(headers,rows);




}
List<Dictionary<string, string>> SortRows(
    (List<string> headers, List<Dictionary<string, string>> rows) data,
    AppConfig config)
{
    var rows = data.rows;

    IOrderedEnumerable<Dictionary<string, string>>? ordered = null;

    foreach (var field in config.SortFields)
    {
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
    List<Dictionary<string, string>> rows,
    AppConfig config)
{
    var lines = new List<string>();

    // si hay encabezado
    if (!config.NoHeader && rows.Count > 0)
    {
        var headers = rows[0].Keys.ToList();
        lines.Add(string.Join(config.Delimiter, headers));
    }

    foreach (var row in rows)
    {
        lines.Add(string.Join(config.Delimiter, row.Values));
    }

    return string.Join("\n", lines);
}



 
record SortField (string Name, bool Numeric, bool Descending);

record AppConfig(
string? InputFile,    //archivo de entrada
string? OutputFile,   // archivo de salida 
string Delimiter,     // separador de las columnas
bool NoHeader,
List<SortField> SortFields   // lista que contiene los criterios de ordenamiento
);





