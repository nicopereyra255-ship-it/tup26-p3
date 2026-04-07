using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;

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

 
record SortField (string Name, bool Numeric, bool Descending);

record AppConfig(
string? InputFile,    //archivo de entrada
string? OutputFile,   // archivo de salida 
string Delimiter,     // separador de las columnas
bool NoHeader,
List<SortField> SortFields   // lista que contiene los criterios de ordenamiento
);





