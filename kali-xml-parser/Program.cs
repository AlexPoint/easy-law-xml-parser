using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace kali_xml_parser
{
    class Program
    {
        static void Main(string[] args)
        {
            var applicationPath = AppDomain.CurrentDomain.BaseDirectory + "..\\..\\";
            var inputDirectory = applicationPath + "input\\";
            var outputDirectory = applicationPath + "output\\";

            var rootElt = new XElement("root");
            
            // Browse all xml files and append their content to the root element
            foreach (var file in Directory.GetFiles(inputDirectory, "*.xml", SearchOption.AllDirectories))
            {
                var doc = XDocument.Load(file);
                rootElt.Add(doc.Root);
            }

            // 
            var bundleDoc = new XDocument(rootElt);

            // Save the compiled xml file
            var outputFile = outputDirectory + "bundle.xml";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            bundleDoc.Save(outputFile);
            Console.WriteLine("Bundle xml file written at {0}", new Uri(outputFile).AbsolutePath);

            // Cleanup unescaped tags (p, sup, em, font, br)
            var tagsToEscapeRegex = new Regex(@"<(p|sup|em|font|br)([^>]*)>");
            var cleanOutputFile = outputDirectory + "bundle-clean.xml";
            using (var input = File.OpenText(outputFile))
            using (var output = new StreamWriter(cleanOutputFile))
            {
                string line;
                while (null != (line = input.ReadLine()))
                {
                    // optionally modify line.
                    var newLine = tagsToEscapeRegex.Replace(line, "<$1$2>");
                    output.WriteLine(newLine);
                }
            }
            Console.WriteLine("Cleanup bundle xml file written at {0}", new Uri(cleanOutputFile).AbsolutePath);

            Console.ReadKey();
        }
    }
}
