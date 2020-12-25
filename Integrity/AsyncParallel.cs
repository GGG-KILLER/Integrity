using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Integrity
{
    internal static class AsyncParallel
    {
        private static async Task ProcessPartition<T> ( IEnumerator<T> partition, Func<T, Task> func, CancellationToken cancellationToken = default )
        {
            using ( partition )
            {
                while ( partition.MoveNext ( ) )
                {
                    cancellationToken.ThrowIfCancellationRequested ( );
                    await Task.Yield ( );
                    await func ( partition.Current ).ConfigureAwait ( false );
                }
            }
        }

        private static async Task ProcessPartition<T> ( IEnumerator<T> partition, Func<T, CancellationToken, Task> func, CancellationToken cancellationToken = default )
        {
            using ( partition )
            {
                while ( partition.MoveNext ( ) )
                {
                    cancellationToken.ThrowIfCancellationRequested ( );
                    await Task.Yield ( );
                    await func ( partition.Current, cancellationToken ).ConfigureAwait ( false );
                }
            }
        }

        public static async Task ForEach<T> (
            IEnumerable<T> source,
            ParallelOptions options,
            Func<T, Task> func )
        {
            await Task.WhenAll ( Partitioner.Create ( source )
                .GetPartitions ( options.MaxDegreeOfParallelism )
                .Select ( partition => ProcessPartition ( partition, func, options.CancellationToken ) ) );
        }

        public static async Task ForEach<T> (
            IEnumerable<T> source,
            ParallelOptions options,
            Func<T, CancellationToken, Task> func )
        {
            await Task.WhenAll ( Partitioner.Create ( source )
                .GetPartitions ( options.MaxDegreeOfParallelism )
                .Select ( partition => ProcessPartition ( partition, func, options.CancellationToken ) ) );
        }
    }
}
