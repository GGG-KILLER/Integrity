using System;
using CommandLine;

namespace Integrity
{
    [Verb ( "pretty", HelpText = "Prints out the file in a human-readable format." )]
    internal class PrettyPrintOptions
    {
        [Option ( 'v', "verbose", Default = false )]
        public Boolean Verbose { get; set; }

        [Value ( 0, HelpText = "The integrity file to format", Required = true, MetaName = "integrity file" )]
        public String IntegrityFile { get; set; }
    }
}