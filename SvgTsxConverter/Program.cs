//############################################################
//
//	Author: Corentin Sakwinski (WilfreGD)
//	https://github.com/wilfregd
//
//	MIT License
//
//  .svg to .tsx converter
//															
//############################################################

using System.Text.RegularExpressions;

class Program
{
    private const string TEMPLATE_PATH = "./config/template.tsx";

    private static string[] obsoleteAttributes = { "class" }; //These will be removed from the final object. Ex: the "class" attribute will be manually replaced by the "className" attribute

    private static string[] svgs { get; set; }
    private static string template { get; set; }

    private static List<string> filenames = new List<string>();
    
    private static void Main(string[] args)
    {
        //Make sure the directories exist
        Directory.CreateDirectory("input");
        Directory.CreateDirectory("output");

        //Load the template
        template = File.ReadAllText(TEMPLATE_PATH);

        //Get the svg files
        var files = Directory.GetFiles("./input", "*.svg");

        if (files.Length > 0)
        {
            foreach (var file in files)
            {
                CreateTSX(file);
            }

            if (filenames.Count > 0)
            {
                GenerateIndex();
            }
        }
        else
        {
            Console.WriteLine("No SVG file found");
        }

        Console.WriteLine("\nPress any key to close...");
        Console.ReadKey();
    }

    /// <summary>
    /// Creates the .tsx file for the given .svg file.
    /// </summary>
    private static void CreateTSX(string svg)
    {
        string svgContent = File.ReadAllText(svg);

        //Get the tags separately
        string[] tags = svgContent.Split(">");
        List<string> contentTags = new List<string>();

        for (int i = 0; i < tags.Length; i++)
        {
            string tag = tags[i];

            if (!string.IsNullOrEmpty(tags[i]) && !string.IsNullOrWhiteSpace(tags[i]))
            {
                tag = tag + ">";
                tag = tag.Replace("\n", "");

                if (!tag.Contains("svg"))
                {
                    contentTags.Add(tag);
                }
            }
        }

        if (tags.Length <= 2)
        {
            Console.WriteLine("No content found in the '<svg>' tag");
            return;
        }

        //Manage the default attributes
        Regex regex = new Regex(@"[-A-Za-z0-9]+=""[^""]*""");
        var matches = regex.Matches(tags[0]);

        string content = BuildSVGObj(matches, tags);

        //Add the new generated file to the list
        string filename = BuildTSXFileName(svg);
        filenames.Add(filename);

        content = content.Replace("[NAME]", filename);

        //Save the file
        //Console.WriteLine(content);
        File.WriteAllText("./output/" + filename + ".tsx", content);

        Console.WriteLine("Generated TSX: " + filename + ".tsx");
    }

    /// <summary>
    /// Creates the file body containing the object with the original attributes.
    /// </summary>
    private static string BuildSVGObj(MatchCollection attributes, string[] tags)
    {
        if (attributes == null || attributes.Count == 0)
        {
            return "";
        }

        string obj = template;

        //Build props
        string objProps = "";

        foreach (var attr in attributes)
        {
            string[] keyValue = attr.ToString().Split("="); //Separates the key and value

            if (obsoleteAttributes.Contains(keyValue[0]))
            {
                continue;
            }

            objProps += $"\n\t\"{keyValue[0]}\"";

            if (keyValue.Length > 0) //The key has a value
            {
                objProps += $":{keyValue[1]}";
            }

            objProps += ",";
        }

        //Add the SVG content
        string objContent = "";
        objContent = "\tcontent: [\n";
        foreach (var tag in tags)
        {
            if (tag.Contains("svg") || string.IsNullOrEmpty(tag) || string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            string fTag = tag.Replace('"', '\'').Replace("\n", "").Replace("\r", "").Replace("\t", "");
            fTag = fTag.Replace("  ", "");

            objContent += $"\t\t\"{fTag}>\",\n";
        }
        objContent += "\n\t]";

        //Write on the template
        obj = obj.Replace("[PROPS]", objProps);
        obj = obj.Replace("[CONTENT]", objContent);

        return obj;
    }

    /// <summary>
    /// Creates the [timestamp]_index.js file for a custom implementation in React.
    /// </summary>
    private static void GenerateIndex()
    {
        string indexContent = "";

        //Timestamp
        var date = DateTime.Now;
        string timestamp = $"{date.Month}/{date.Day}/{date.Year}:{date.Hour}:{date.Minute}:{date.Second}";
        indexContent += "//File created at: " + timestamp + "\n";

        //Imports
        indexContent += "\n//Imports";
        foreach (var file in filenames)
        {
            indexContent += $"\nimport {file} from \"./{file}\"";
        }

        //Exports
        indexContent += "\n\n//Export";
        indexContent += "\nexport{";
        foreach (var file in filenames)
        {
            indexContent += $"\n    {file},";
        }
        indexContent += "\n}";

        string indexFilename = $"{date.Month}-{date.Day}-{date.Year}_{date.Hour}-{date.Minute}-{date.Second}_index.js";
        Console.WriteLine("\nGenerated index file: " + indexFilename);
        File.WriteAllText("./output/" + indexFilename, indexContent);
    }

    /// <summary>
    /// Takes the .svg file name to generate a .tsx (PascalCase) filename.
    /// </summary>
    private static string BuildTSXFileName(string svg)
    {
        //Generate the filename
        svg = svg.Replace(" ", "");
        svg = Path.GetFileName(svg).Replace(".svg", "");
        List<string> subs = new List<string>();
        subs.AddRange(svg.Split(new char[] { '_', '-' }));

        //Build the new filename
        string tsx = "";
        foreach (var sub in subs)
        {
            string subU = char.ToUpper(sub[0]) + sub.Substring(1);
            tsx += subU;
        }

        tsx += "Icon";

        return tsx;
    }
}