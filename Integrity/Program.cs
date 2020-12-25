using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using GlobExpressions;
using Tsu.Numerics;
using Tsu.Timing;

namespace Integrity
{
    internal class Program
    {
        public static readonly HashSet<String> Algorithms = new ( ) { "SHA256", "SHA384", "SHA512" };

        private static async Task<Int32> Main ( String[] args ) =>
            await Parser.Default
                        .ParseArguments<GenerateOptions, CheckOptions, PrettyPrintOptions> ( args )
                        .MapResult (
                            ( GenerateOptions options ) => Generate ( options ),
                            ( CheckOptions options ) => Check ( options ),
                            ( PrettyPrintOptions options ) => PrettyPrint ( options ),
                            errors => Task.FromResult ( -1 ) )
                        .ConfigureAwait ( false );

        private static async Task<Int32> Generate ( GenerateOptions options )
        {
            options.Hash = options.Hash.ToUpper ( );
            if ( !Algorithms.Contains ( options.Hash ) )
                throw new Exception ( "Invalid hash algorithm chosen. Supported ones are: SHA256, SHA384, SHA512" );

            var logger = new ConsoleTimingLogger ( );
            using ( logger.BeginScope ( "Hash file generation" ) )
            {
                var paths = new HashSet<String> ( );
                try
                {
                    using ( logger.BeginOperation ( "Listing files for globs" ) )
                    {
                        foreach ( var pattern in options.Globs )
                        {
                            foreach ( var path in Glob.Files ( options.Root, pattern, GlobOptions.Compiled ) )
                                paths.Add ( path );
                        }
                    }
                }
                catch ( Exception ex )
                {
                    logger.LogError ( $"Error matching globs:\n{ex}" );
                    return -1;
                }
                paths.TrimExcess ( );

                IntegrityFile file;
                using ( options.Verbose ? logger.BeginScope ( "File hashing" ) : null )
                {
                    try
                    {
                        var delta = Math.Max ( paths.Count / 100, 1 );
                        var calculated = 0;

                        var generator = new FileGenerator ( options.Hash, options.Root );
                        generator.FileProcessed += ( _, path, entry, elapsed ) =>
                        {
                            if ( Interlocked.Increment ( ref calculated ) % delta == 0 )
                            {
                                logger.LogInformation ( $"Progress: {calculated}/{paths.Count}" );
                            }
                        };
                        file = await generator.Generate ( paths, options.Threads )
                                              .ConfigureAwait ( false );
                    }
                    catch ( Exception ex )
                    {
                        logger.LogError ( $"Error while hashing files:\n{ex}" );
                        return -1;
                    }
                }

                using FileStream stream = File.Open ( options.File, FileMode.Create, FileAccess.Write, FileShare.None );
                file.WriteTo ( stream );
            }
            return 0;
        }

        private static async Task<Int32> Check ( CheckOptions options )
        {
            var logger = new ConsoleTimingLogger ( );
            using ( logger.BeginScope ( "Integrity check" ) )
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
                    using ( logger.BeginScope ( "File checking" ) )
                    {
                        var checker = new FileChecker ( options.Root );
                        checker.FileCheckFailed += ( _, entry, actualDigest, elapsed ) =>
                        {
                            logger.LogWarning ( $"Integrity violation for '{entry.RelativePath}' failed in {Duration.Format ( elapsed )}." );
                            Interlocked.Increment ( ref violated );
                        };
                        checker.FileCheckFinished += ( _, entry, checkFailed, elapsed ) =>
                        {
                            if ( !checkFailed )
                                logger.LogDebug ( $"Integrity check passed for '{entry.RelativePath}' in {Duration.Format ( elapsed )}." );

                            if ( Interlocked.Increment ( ref @checked ) % delta == 0 )
                            {
                                logger.LogInformation ( $"Progress: {@checked}/{file.Entries.Length}" );
                            }
                        };

                        ImmutableArray<IntegrityFile.Entry> failed = await checker.Check ( file, options.ThreadCount );

                        logger.LogInformation ( $"Files checked:         {file.Entries.Length}" );
                        logger.LogInformation ( $"Files violated:        {violated}" );
                        logger.LogInformation ( $"Corruption percentage: { violated / ( Double ) file.Entries.Length * 100D:#0.##}%" );

                        return failed.Length > 0 ? -1 : 0;
                    }
                }
                catch ( Exception e )
                {
                    logger.LogError ( $"Error while checking the integrity of the files:\n{e}" );
                    return -1;
                }
            }
        }

        private static async Task<Int32> PrettyPrint ( PrettyPrintOptions options )
        {
            var logger = new ConsoleTimingLogger ( );
            using ( logger.BeginScope ( "Human Readable Printing" ) )
            {
                try
                {
                    IntegrityFile file;
                    using ( FileStream stream = File.Open ( options.IntegrityFile, FileMode.Open, FileAccess.Read, FileShare.Read ) )
                        file = IntegrityFile.ReadFrom ( stream );

                    logger.LogInformation ( $"Hash algorithm used: {file.HashAlgorithm}" );
                    logger.LogInformation ( $"Entry count: {file.Entries.Length}" );

                    using ( logger.BeginScope ( "Entries:" ) )
                    {
                        var mlen = file.Entries.Max ( entry => entry.RelativePath.Length );
                        foreach ( IntegrityFile.Entry entry in file.Entries )
                            logger.LogInformation ( $"{entry.RelativePath.PadRight ( mlen, ' ' )}: {entry.HexDigest}" );
                    }
                }
                catch ( Exception e )
                {
                    logger.LogError ( $"Error while printing:\n{e}" );
                }
            }
            return 0;
        }
    }
}