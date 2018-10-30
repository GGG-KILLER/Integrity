using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CommandLine;
using GlobExpressions;
using GUtils.Timing;
using Integrity.Util;

namespace Integrity
{
    [Verb ( "gen", HelpText = "Generates a hash integrity file" )]
    internal class GenerateOptions
    {
        [Option ( 'h', "hash", Default = "sha256", HelpText = "The hash algorithm to use for file hashing when generating the integrity file. Supported algorithms: MD5, SHA1, SHA256, SHA384, SHA512" )]
        public String Hash { get; set; }

        [Option ( 't', "threads", Default = 8, HelpText = "The amount of threads to use when generating the integrity file" )]
        public Int32 Threads { get; set; }

        [Option ( 'r', "root", Default = ".", HelpText = "The directory to use as root" )]
        public String Root { get; set; }

        [Option ( 'v', "verbose", Default = false )]
        public Boolean Verbose { get; set; }

        [Option ( 'f', "file", HelpText = "The integrity file to write to", Required = true )]
        public String File { get; set; }

        // Arbitrary pattern limit? yes.
        [Value ( 0, HelpText = "The globs to search with", Min = 1, Max = 25, Required = true, MetaName = "globs" )]
        public IEnumerable<String> Globs { get; set; }
    }

    [Verb ( "check", HelpText = "Checks a hash integrity file" )]
    internal class CheckOptions
    {
        [Option ( 't', "threads", Default = 8, HelpText = "The amount of threads to use for file hashing when checking the integrity file" )]
        public Int32 ThreadCount { get; set; }

        [Option ( 'v', "verbose", Default = false )]
        public Boolean Verbose { get; set; }

        [Value ( 0, HelpText = "The integrity file to check", Required = true, MetaName = "integrity file" )]
        public String File { set; get; }

        [Value ( 1, HelpText = "The starting path", Default = ".", MetaName = "root directory", MetaValue = "The path to use as root" )]
        public String Root { get; set; }
    }

    [Verb ( "pretty", HelpText = "Prints out the file in a human-readable format." )]
    internal class PrettyPrintOptions
    {
        [Option ( 'v', "verbose", Default = false )]
        public Boolean Verbose { get; set; }

        [Value ( 0, HelpText = "The integrity file to format", Required = true, MetaName = "integrity file" )]
        public String IntegrityFile { get; set; }
    }

    internal class Program
    {
        public static readonly HashSet<String> Algorithms = new HashSet<String> { "MD5", "SHA1", "SHA256", "SHA384", "SHA512" };

        private static Int32 Main ( String[] args ) =>
            Parser.Default.ParseArguments<GenerateOptions, CheckOptions, PrettyPrintOptions> ( args )
                .MapResult (
                    ( GenerateOptions options ) => Generate ( options ),
                    ( CheckOptions options ) => Check ( options ),
                    ( PrettyPrintOptions options ) => PrettyPrint ( options ),
                    errors => -1 );

        private static Int32 Generate ( GenerateOptions options )
        {
            options.Hash = options.Hash.ToUpper ( );
            if ( !Algorithms.Contains ( options.Hash ) )
                throw new Exception ( "Invalid hash algorithm chosen. Supported ones are: MD5, SHA1, SHA256, SHA384, SHA512" );

            using ( var rootarea = new TimingArea ( "Hash file generation" ) )
            {
                var paths = new HashSet<String> ( );
                try
                {
                    using ( rootarea.TimeLine ( "Listing files for globs" ) )
                        foreach ( var pattern in options.Globs )
                            foreach ( var path in Glob.Files ( options.Root, pattern, GlobOptions.Compiled ) )
                                paths.Add ( path );
                }
                catch ( Exception ex )
                {
                    rootarea.Log ( $"Error matching globs:\n{ex}" );
                    return -1;
                }
                paths.TrimExcess ( );

                IntegrityFile.Entry[] results;
                using ( TimingArea area = options.Verbose ? new TimingArea ( "File hashing", rootarea ) : null )
                {
                    try
                    {
                        var delta = Math.Max ( paths.Count / 100, 1 );
                        var calculated = 0;
                        results = paths
                            .AsParallel ( )
                            .WithDegreeOfParallelism ( options.Threads )
                            .Select ( path =>
                            {
                                try
                                {
                                    using ( area?.TimeLine ( $"hashing {path}" ) )
                                    using ( FileStream stream = File.Open ( Path.Combine ( options.Root, path ), FileMode.Open, FileAccess.Read, FileShare.Read ) )
                                        return new IntegrityFile.Entry ( path, Hash.WithAlgorithm ( options.Hash, stream ) );
                                }
                                finally
                                {
                                    Interlocked.Increment ( ref calculated );
                                    if ( calculated % delta == 0 )
                                        lock ( area ?? rootarea )
                                            ( area ?? rootarea ).Log ( $"Progress: {calculated}/{paths.Count}" );
                                }
                            } )
                            .ToArray ( );
                    }
                    catch ( Exception ex )
                    {
                        ( area ?? rootarea ).Log ( $"Error while hashing files:\n{ex}" );
                        return -1;
                    }
                }

                using ( FileStream stream = File.Open ( options.File, FileMode.Create, FileAccess.Write, FileShare.None ) )
                    new IntegrityFile ( options.Hash, results ).WriteTo ( stream );
            }
            return 0;
        }

        private static Int32 Check ( CheckOptions options )
        {
            using ( var area = new TimingArea ( "Integrity check" ) )
            {
                try
                {
                    if ( !Directory.Exists ( options.Root ) )
                        throw new Exception ( "Root directory could not be found" );

                    IntegrityFile file;
                    using ( FileStream stream = File.Open ( options.File, FileMode.Open, FileAccess.Read, FileShare.Read ) )
                        file = IntegrityFile.ReadFrom ( stream );

                    var @checked = 0;
                    var violated = 0;
                    var delta = Math.Max ( file.Entries.Length / 100, 1 );
                    using ( var sub = new TimingArea ( "File checking", area ) )
                    {
                        file.Entries
                            .AsParallel ( )
                            .WithDegreeOfParallelism ( options.ThreadCount )
                            .ForAll ( entry =>
                            {
                                using ( FileStream stream = File.Open ( Path.Combine ( options.Root, entry.RelativePath ), FileMode.Open, FileAccess.Read, FileShare.Read ) )
                                {
                                    if ( Hash.WithAlgorithm ( file.HashAlgorithm, stream ) != entry.HexDigest )
                                    {
                                        sub.Log ( $"Integrity violation: {entry.RelativePath}" );
                                        Interlocked.Increment ( ref violated );
                                    }
                                    else if ( options.Verbose )
                                        sub.Log ( $"Integrity valid: {entry.RelativePath}" );
                                }
                                Interlocked.Increment ( ref @checked );
                                if ( @checked % delta == 0 )
                                    lock ( sub )
                                        sub.Log ( $"Progress: {@checked}/{file.Entries.Length}" );
                            } );
                    }

                    area.Log ( $"Files checked:         {file.Entries.Length}" );
                    area.Log ( $"Files violated:        {violated}" );
                    area.Log ( $"Corruption percentage: {( violated / ( Double ) file.Entries.Length ) * 100D:#0.##}%" );
                }
                catch ( Exception e )
                {
                    area.Log ( $"Error while checking the integrity of the files:\n{e}" );
                    return -1;
                }
            }
            return 0;
        }

        private static Int32 PrettyPrint ( PrettyPrintOptions options )
        {
            using ( var area = new TimingArea ( "Human Readable Printing" ) )
            {
                try
                {
                    IntegrityFile file;
                    using ( FileStream stream = File.Open ( options.IntegrityFile, FileMode.Open, FileAccess.Read, FileShare.Read ) )
                        file = IntegrityFile.ReadFrom ( stream );

                    area.Log ( $"Hash algorithm used: {file.HashAlgorithm}" );
                    area.Log ( $"Entry count: {file.Entries.Length}" );
                    using ( var sub = new TimingArea ( "Entries:", area ) )
                    {
                        var mlen = file.Entries.Max ( entry => entry.RelativePath.Length );
                        foreach ( IntegrityFile.Entry entry in file.Entries )
                            sub.Log ( $"{entry.RelativePath.PadRight ( mlen, ' ' )}: {entry.HexDigest}" );
                    }
                }
                catch ( Exception e )
                {
                    area.Log ( $"Error while printing:\n{e}" );
                }
            }
            return 0;
        }
    }
}
