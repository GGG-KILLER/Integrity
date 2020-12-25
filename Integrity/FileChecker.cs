using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Integrity.Util;

namespace Integrity
{
    internal delegate void FileCheckerCheckFailedEventHandler (
        FileChecker checker,
        IntegrityFile.Entry entry,
        String actualDigest,
        Int64 elapsedTicks );
    internal delegate void FileCheckerCheckFinishedEventHandler (
        FileChecker checker,
        IntegrityFile.Entry entry,
        Boolean checkFailed,
        Int64 elapsedTicks );

    internal class FileChecker
    {
        private readonly String _root;

        public event FileCheckerCheckFailedEventHandler FileCheckFailed;
        public event FileCheckerCheckFinishedEventHandler FileCheckFinished;

        public FileChecker ( String root )
        {
            this._root = root;
        }

        public async Task<ImmutableArray<IntegrityFile.Entry>> Check (
            IntegrityFile integrityFile,
            Int32 parallelismDegree,
            CancellationToken cancellationToken = default )
        {
            if ( parallelismDegree == -1 )
                parallelismDegree = Environment.ProcessorCount;

            var failedChecks = new ConcurrentBag<IntegrityFile.Entry> ( );
            if ( parallelismDegree == 0 )
            {
                await Task.WhenAll ( integrityFile.Entries.Select ( async entry => await this.CheckEntry ( integrityFile.HashAlgorithm, entry, failedChecks, cancellationToken ).ConfigureAwait ( false ) ) );
            }
            else
            {
                await AsyncParallel.ForEach ( integrityFile.Entries, new ParallelOptions
                {
                    MaxDegreeOfParallelism = parallelismDegree,
                    CancellationToken = cancellationToken,
                }, async ( entry, token ) => await this.CheckEntry ( integrityFile.HashAlgorithm, entry, failedChecks, token ) );
            }
            return failedChecks.ToImmutableArray ( );
        }

        public async Task CheckEntry (
            String hashAlgorithm,
            IntegrityFile.Entry entry,
            ConcurrentBag<IntegrityFile.Entry> failedChecks,
            CancellationToken cancellationToken = default )
        {
            String digest;
            var start = Stopwatch.GetTimestamp ( );
            using ( FileStream stream = File.Open ( Path.Combine ( this._root, entry.RelativePath ), FileMode.Open, FileAccess.Read, FileShare.Read ) )
            {
                digest = await Hash.WithAlgorithmAsync ( hashAlgorithm, stream, cancellationToken: cancellationToken )
                                   .ConfigureAwait ( false );
            }
            var end = Stopwatch.GetTimestamp ( );
            var elapsed = end - start;

            if ( !String.Equals ( entry.HexDigest, digest, StringComparison.OrdinalIgnoreCase ) )
            {
                failedChecks.Add ( entry );
                this.FileCheckFailed?.Invoke ( this, entry, digest, elapsed );
            }
            this.FileCheckFinished?.Invoke ( this, entry, elapsed );
        }
    }
}
