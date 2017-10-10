using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
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
            var stopwatch = Stopwatch.StartNew();

            var applicationPath = AppDomain.CurrentDomain.BaseDirectory + "..\\..\\";
            var inputDirectory = applicationPath + "input\\";
            var outputDirectory = applicationPath + "output\\";

            // Cleanup output directory
            var di = new DirectoryInfo(outputDirectory);
            foreach (var file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            // Create raw output directory (xml files as parsed in the input data, but consolidated by collective agreement)
            var rawOutputDirectory = outputDirectory + "raw\\";
            if (!Directory.Exists(rawOutputDirectory))
            {
                Directory.CreateDirectory(rawOutputDirectory);
            }

            // Browse all xml files and store for each collective agreement id, the associated input file paths.
            Console.WriteLine("Start browsing input files");
            var idToFiles = new Dictionary<string, List<string>>();
            foreach (var file in Directory.GetFiles(inputDirectory, "*.xml", SearchOption.AllDirectories))
            {
                var doc = XDocument.Load(file);

                var collAgreeNumber = doc.Descendants("CONTENEUR").Attributes().FirstOrDefault(att => att.Name == "num");
                var id = collAgreeNumber == null ? "null" : collAgreeNumber.Value;
                if (!idToFiles.ContainsKey(id))
                {
                    idToFiles.Add(id, new List<string>());
                }
                idToFiles[id].Add(file);
            }

            // Write temp file
            Console.WriteLine("Start writing mapping file");
            var mappingFilePath = outputDirectory + "mapping.txt";
            foreach (var idToFile in idToFiles)
            {
                using (var writer = File.OpenWrite(mappingFilePath))
                {
                    using (var tw = new StreamWriter(writer))
                    {
                        tw.WriteLine(idToFile.Key);
                        tw.WriteLine("--");
                        foreach (var file in idToFile.Value)
                        {
                            tw.WriteLine(file); 
                        }
                    }
                }
            }
            Console.WriteLine("End writing mapping file");

            // Compile all collective agreement xml files
            Console.WriteLine("Start writing compiled collective agreement xml files ({0} files)", idToFiles.Count);
            foreach (var entry in idToFiles)
            {
                var outputFile = rawOutputDirectory + entry.Key + ".xml";

                // Create empty XML file with root element only
                var bundleDoc = new XDocument(new XElement("root"));
                
                // Add content of all XML files under the root element
                foreach (var file in entry.Value)
                {
                    var doc = XDocument.Load(file);
                    bundleDoc.Root.Add(doc.Root);
                }

                // Do two transformations

                // 1 - escape content of <CONTENU> node
                var contentNode = bundleDoc.Descendants("CONTENU").FirstOrDefault();
                if (contentNode != null)
                {
                    contentNode.ReplaceNodes(new XCData(contentNode.Value));
                }

                // 2 - delete <LIEN> tags; when several <LIEN> tags are present for the same article, it makes the conversion to csv more difficult
                // (and those tags do not contain anything valuable)
                var linkNodes = bundleDoc.Descendants("LIENS");
                foreach (var linkNode in linkNodes)
                {
                    linkNode.Remove();
                }

                // Save the compiled xml file
                bundleDoc.Save(outputFile);
            }

            /*Console.WriteLine("Start writing compiled collective agreement xml files");

            // Cleanup unescaped tags (p, sup, em, font, br)
            Console.WriteLine("Starts cleaning xml files");
            var cleanOutputDirectory = outputDirectory + "clean\\";
            if (!Directory.Exists(cleanOutputDirectory))
            {
                Directory.CreateDirectory(cleanOutputDirectory);
            }
            foreach (var file in Directory.GetFiles(rawOutputDirectory))
            {
                var tagsToEscapeRegex = new Regex(@"<(/)?(p|sup|em|font|br|div|table|tr|td|tbody|th)([^>]*)>");
                var cleanOutputFile = cleanOutputDirectory + Path.GetFileName(file);
                using (var input = File.OpenText(file))
                {
                    using (var output = new StreamWriter(cleanOutputFile))
                    {
                        string line;
                        while (null != (line = input.ReadLine()))
                        {
                            // optionally modify line.
                            var newLine = tagsToEscapeRegex.Replace(line, "&lt;$1$2$3&gt;");
                            output.WriteLine(newLine);
                        }
                    }
                }
            }*/
            
            stopwatch.Stop();
            Console.WriteLine("END (elapsed: {0})", stopwatch.Elapsed.ToString("g"));
            Console.ReadKey();
        }
    }
}
