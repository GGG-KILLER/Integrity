using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Integrity.Util;

namespace Integrity
{
    internal delegate void FileProcessedEventHandler (
        FileGenerator generator,
        String path,
        IntegrityFile.Entry entry,
        Int64 ticksElapsed );

    internal class FileGenerator
    {
        private readonly String _hashAlgorithm;
        private readonly String _root;
        private readonly Int32 _bufferSize;

        /// <summary>
        /// Called when a file is processed.
        /// </summary>
        public event FileProcessedEventHandler FileProcessed;

        public FileGenerator ( String hashAlgorithm, String root, Int32 bufferSize )
        {
            this._hashAlgorithm = hashAlgorithm;
            this._root = root;
            this._bufferSize = bufferSize;
        }

        public async Task<IntegrityFile> Generate (
            IEnumerable<String> paths,
            Int32 parallelismDegree,
            CancellationToken cancellationToken = default )
        {
            if ( parallelismDegree == -1 )
                parallelismDegree = Environment.ProcessorCount;

            IntegrityFile.Entry[] entries;
            if ( parallelismDegree == 0 )
            {
                entries = await Task.WhenAll ( paths.Select ( async path => await this.ProcessFile ( path, cancellationToken ).ConfigureAwait ( false ) ) ).ConfigureAwait ( false );
            }
            else
            {
                var tempEntries = new ConcurrentBag<IntegrityFile.Entry> ( );
                await AsyncParallel.ForEach ( paths, new ParallelOptions
                {
                    MaxDegreeOfParallelism = parallelismDegree,
                    CancellationToken = cancellationToken
                }, async ( path, token ) =>
                {
                    IntegrityFile.Entry entry = await this.ProcessFile ( path, token )
                                                          .ConfigureAwait ( false );
                    tempEntries.Add ( entry );
                } )
                    .ConfigureAwait ( false );
                entries = tempEntries.ToArray ( );
            }

            return new IntegrityFile ( this._hashAlgorithm, entries );
        }

        public async Task<IntegrityFile.Entry> ProcessFile ( String path, CancellationToken cancellationToken = default )
        {
            var start = Stopwatch.GetTimestamp ( );
            IntegrityFile.Entry entry;
            using ( FileStream stream = File.Open (
                Path.Combine ( this._root, path ),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read ) )
            {
                var hash = await Hash.WithAlgorithmAsync ( this._hashAlgorithm, stream, this._bufferSize, cancellationToken: cancellationToken )
                                     .ConfigureAwait ( false );
                entry = new IntegrityFile.Entry ( path, hash );
            }
            var end = Stopwatch.GetTimestamp ( );

            this.FileProcessed?.Invoke ( this, path, entry, end - start );
            return entry;
        }
    }
}
