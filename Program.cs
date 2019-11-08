using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;

namespace sqltomd
{
    class Program
    {
        private static Regex NameRegex = new Regex("\".*\"");

        static async Task Main(string[] args)
        {
            var opts = Parser.Default.ParseArguments<Options>(args).MapResult(opt => opt, null);
            await RunApplication(opts);
        }

        static async Task RunApplication(Options opts)
        {
            var fileName = opts.FileName.Trim();

            if (!File.Exists(fileName))
            {
                throw new Exception($"File {fileName} does not exist.");
            }

            using (var sqlfile = File.OpenText(fileName))
            {
                var mdfilename = fileName.Replace(".sql", ".md");
                if (File.Exists(mdfilename))
                {
                    File.Delete(mdfilename);
                }

                using (var mdwriter = new StreamWriter(mdfilename, false))
                {
                    try
                    {                        
                        await mdwriter.WriteLineAsync($"# Tables");
                        await mdwriter.WriteLineAsync();
                        var row = new StringBuilder();
                        while (!sqlfile.EndOfStream)
                        {
                            var line = await sqlfile.ReadLineAsync();
                            if (string.IsNullOrEmpty(line))
                            {
                                continue;
                            }

                            if (line.ToUpperInvariant().Trim().StartsWith("CREATE TABLE"))
                            {
                                var match = NameRegex.Match(line);
                                if (match != null)
                                {
                                    var name = match.Captures.FirstOrDefault();
                                    if (name == null)
                                    {
                                        continue;
                                    }

                                    var tablename = name.ToString().Replace("\"", "");
                                    await mdwriter.WriteLineAsync($"## {tablename}");
                                    await mdwriter.WriteLineAsync();
                                }
                                await mdwriter.WriteLineAsync($"|Name|Type|Title|Description|");
                                await mdwriter.WriteLineAsync($"|---|---|---|---|");
                            }

                            if (line.StartsWith("    \""))
                            {
                                try
                                {
                                    var splitted = line.Split(" ").Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                    var lineForWrite = $"|{splitted[0].Replace("\"", "")}|{splitted[1]}|{string.Join(' ', splitted.Skip(2).ToList())}||";
                                    await mdwriter.WriteLineAsync(lineForWrite);
                                }
                                catch { }
                            }

                            if (line.Trim() == ");")
                            {
                                await mdwriter.WriteLineAsync();
                            }

                            await mdwriter.FlushAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception: " + ex);
                    }
                }
                Console.WriteLine("File is ended");
            }
        }
    }
}
