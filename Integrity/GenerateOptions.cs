using System;
using System.Collections.Generic;
using CommandLine;

namespace Integrity
{
    [Verb ( "gen", HelpText = "Generates a hash integrity file" )]
    internal class GenerateOptions
    {
        [Option ( 'b', "buffer-size", Default = "16KiB" )]
        public String BufferSize { get; set; }

        [Option ( 'v', "verbose", Default = false )]
        public Boolean Verbose { get; set; }

        [Option ( 't', "threads", Default = -1, HelpText = "The amount of threads to use when generating the integrity file" )]
        public Int32 ThreadCount { get; set; }

        [Option ( 'h', "hash", Default = "sha256", HelpText = "The hash algorithm to use for file hashing when generating the integrity file. Supported algorithms: MD5, SHA1, SHA256, SHA384, SHA512" )]
        public String Hash { get; set; }

        [Option ( 'r', "root", Default = ".", HelpText = "The directory to use as root" )]
        public String Root { get; set; }

        [Option ( 'f', "file", HelpText = "The integrity file to write to", Required = true )]
        public String File { get; set; }

        // Arbitrary pattern limit? yes.
        [Value ( 0, HelpText = "The globs to search with", Min = 1, Max = 25, Required = true, MetaName = "globs" )]
        public IEnumerable<String> Globs { get; set; }
    }
}