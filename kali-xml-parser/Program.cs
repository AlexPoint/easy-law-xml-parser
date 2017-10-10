using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // Browse all xml files and append their content to the root element
            var rawOutputDirectory = outputDirectory + "raw\\";
            if (!Directory.Exists(rawOutputDirectory))
            {
                Directory.CreateDirectory(rawOutputDirectory);
            }
            var idToNodes = new Dictionary<string, List<XElement>>();
            foreach (var file in Directory.GetFiles(inputDirectory, "*.xml", SearchOption.AllDirectories))
            {
                var doc = XDocument.Load(file);

                var collAgreeNumber = doc.Descendants("CONTENEUR").Attributes().FirstOrDefault(att => att.Name == "num");
                if (collAgreeNumber != null && !string.IsNullOrEmpty(collAgreeNumber.Value))
                {
                    //Console.WriteLine("Found file with collective agreement: {0}", file);
                    var id = collAgreeNumber.Value;
                    if (!idToNodes.ContainsKey(id))
                    {
                        idToNodes.Add(id, new List<XElement>());
                    }
                    idToNodes[id].Add(doc.Root);

                    //
                    if (idToNodes.Count > 10)
                    {
                        // Write previous collective agreement and flush dictionary
                        // write all documents (one per collective agreement)
                        foreach (var entry in idToNodes)
                        {
                            var outputFile = rawOutputDirectory + entry.Key + ".xml";

                            var bundleDoc = new XDocument(new XElement("root"));
                            if (File.Exists(outputFile))
                            {
                                bundleDoc = XDocument.Load(outputFile);
                            }

                            foreach (var node in entry.Value)
                            {
                                bundleDoc.Root.Add(node);
                            }

                            // Save the compiled xml file

                            if (!Directory.Exists(outputDirectory))
                            {
                                Directory.CreateDirectory(outputDirectory);
                            }
                            bundleDoc.Save(outputFile);
                            Console.WriteLine("Bundle xml file written at {0}", new Uri(outputFile).AbsolutePath);
                        }

                        idToNodes.Clear();
                    }
                }

                // do nothing
            }

            // Write what's remaining in the dictionary
            foreach (var entry in idToNodes)
            {
                var outputFile = rawOutputDirectory + entry.Key + ".xml";

                var bundleDoc = new XDocument(new XElement("root"));
                if (File.Exists(outputFile))
                {
                    bundleDoc = XDocument.Load(outputFile);
                }

                foreach (var node in entry.Value)
                {
                    bundleDoc.Root.Add(node);
                }

                // Save the compiled xml file

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                bundleDoc.Save(outputFile);
                Console.WriteLine("Bundle xml file written at {0}", new Uri(outputFile).AbsolutePath);
            }
            //Console.WriteLine("Found {0} collective agreement ids", idToNodes.Count);

            // Cleanup unescaped tags (p, sup, em, font, br)
            Console.WriteLine("Starts cleaning xml files");
            var cleanOutputDirectory = outputDirectory + "clean\\";
            if (!Directory.Exists(cleanOutputDirectory))
            {
                Directory.CreateDirectory(cleanOutputDirectory);
            }
            foreach (var file in Directory.GetFiles(rawOutputDirectory))
            {
                var tagsToEscapeRegex = new Regex(@"<(/)?(p|sup|em|font|br)([^>]*)>");
                var cleanOutputFile = cleanOutputDirectory + Path.GetFileName(file);
                using (var input = File.OpenText(file))
                {
                    using (var output = new StreamWriter(cleanOutputFile))
                    {
                        string line;
                        while (null != (line = input.ReadLine()))
                        {
                            // optionally modify line.
                            var newLine = tagsToEscapeRegex.Replace(line, "<$1$2$3>");
                            output.WriteLine(newLine);
                        }
                    }
                }
            }
            
            stopwatch.Stop();
            Console.WriteLine("END (elapsed: {0})", stopwatch.Elapsed.ToString("g"));
            Console.ReadKey();
        }
    }
}
