using CommandLine;

namespace sqltomd
{
    public class Options
    {
        [Option('f', "file", Required = true, HelpText = "Input file name.")]
        public string FileName { get; set; }
    }
}