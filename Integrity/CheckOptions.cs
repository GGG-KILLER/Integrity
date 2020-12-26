using System;
using CommandLine;

namespace Integrity
{
    [Verb ( "check", HelpText = "Checks a hash integrity file" )]
    internal class CheckOptions
    {
        [Option ( 'b', "buffer-size", Default = "16KiB" )]
        public String BufferSize { get; set; }

        [Option ( 'v', "verbose", Default = false )]
        public Boolean Verbose { get; set; }

        [Option ( 't', "threads", Default = -1, HelpText = "The amount of threads to use when generating the integrity file" )]
        public Int32 ThreadCount { get; set; }

        [Value ( 0, HelpText = "The integrity file to check", Required = true, MetaName = "integrity file" )]
        public String File { set; get; }

        [Value ( 1, HelpText = "The starting path", Default = ".", MetaName = "root directory", MetaValue = "The path to use as root" )]
        public String Root { get; set; }
    }
}