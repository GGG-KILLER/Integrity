using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tsu.Numerics;
using Crypto = System.Security.Cryptography;

namespace Integrity.Util
{
    /// <summary>
    /// A hashing utility class.
    /// </summary>
    internal static class Hash
    {
        public static String SHA256 ( String data, Encoding encoding = null )
        {
            using var sha256 = Crypto.SHA256.Create ( );
            return WithAlgorithm ( sha256, data, encoding );
        }

        public static String SHA256 ( Byte[] data )
        {
            using var sha256 = Crypto.SHA256.Create ( );
            return WithAlgorithm ( sha256, data );
        }

        public static String SHA256 ( Stream data )
        {
            using var sha256 = Crypto.SHA256.Create ( );
            return WithAlgorithm ( sha256, data );
        }

        public static Task<String> SHA256Async ( Stream stream, Int32 bufferSize = 4 * FileSize.KiB, CancellationToken cancellationToken = default )
        {
            using var sha256 = Crypto.SHA256.Create ( );
            return WithAlgorithmAsync ( sha256, stream, bufferSize, cancellationToken );
        }

        public static String SHA384 ( String data, Encoding encoding = null )
        {
            using var sha384 = Crypto.SHA384.Create ( );
            return WithAlgorithm ( sha384, data, encoding );
        }

        public static String SHA384 ( Byte[] data )
        {
            using var sha384 = Crypto.SHA384.Create ( );
            return WithAlgorithm ( sha384, data );
        }

        public static String SHA384 ( Stream data )
        {
            using var sha384 = Crypto.SHA384.Create ( );
            return WithAlgorithm ( sha384, data );
        }

        public static Task<String> SHA384Async ( Stream stream, Int32 bufferSize = 4 * FileSize.KiB, CancellationToken cancellationToken = default )
        {
            using var sha384 = Crypto.SHA384.Create ( );
            return WithAlgorithmAsync ( sha384, stream, bufferSize, cancellationToken );
        }

        public static String SHA512 ( String data, Encoding encoding = null )
        {
            using var sha512 = Crypto.SHA512.Create ( );
            return WithAlgorithm ( sha512, data, encoding );
        }

        public static String SHA512 ( Byte[] data )
        {
            using var sha512 = Crypto.SHA512.Create ( );
            return WithAlgorithm ( sha512, data );
        }

        public static String SHA512 ( Stream data )
        {
            using var sha512 = Crypto.SHA512.Create ( );
            return WithAlgorithm ( sha512, data );
        }

        public static Task<String> SHA512Async ( Stream stream, Int32 bufferSize = 4 * FileSize.KiB, CancellationToken cancellationToken = default )
        {
            using var sha512 = Crypto.SHA512.Create ( );
            return WithAlgorithmAsync ( sha512, stream, bufferSize, cancellationToken );
        }

        public static String WithAlgorithm ( String name, String data, Encoding encoding = null )
        {
            if ( String.IsNullOrEmpty ( name ) )
                throw new ArgumentException ( $"'{nameof ( name )}' cannot be null or empty.", nameof ( name ) );
            if ( data is null )
                throw new ArgumentNullException ( nameof ( data ) );

            using var algo = HashAlgorithm.Create ( name.ToUpperInvariant ( ) );
            return WithAlgorithm ( algo, data, encoding );
        }

        public static String WithAlgorithm ( String name, Byte[] data )
        {
            if ( String.IsNullOrEmpty ( name ) )
                throw new ArgumentException ( $"'{nameof ( name )}' cannot be null or empty.", nameof ( name ) );
            if ( data is null )
                throw new ArgumentNullException ( nameof ( data ) );

            using var algo = HashAlgorithm.Create ( name.ToUpperInvariant ( ) );
            return WithAlgorithm ( algo, data );
        }

        public static String WithAlgorithm ( String name, Stream data )
        {
            if ( String.IsNullOrEmpty ( name ) )
                throw new ArgumentException ( $"'{nameof ( name )}' cannot be null or empty.", nameof ( name ) );
            if ( data is null )
                throw new ArgumentNullException ( nameof ( data ) );

            using var algo = HashAlgorithm.Create ( name.ToUpperInvariant ( ) );
            return WithAlgorithm ( algo, data );
        }

        public static Task<String> WithAlgorithmAsync ( String name, Stream stream, Int32 bufferSize = 4 * FileSize.KiB, CancellationToken cancellationToken = default )
        {
            if ( String.IsNullOrEmpty ( name ) )
                throw new ArgumentException ( $"'{nameof ( name )}' cannot be null or empty.", nameof ( name ) );
            if ( stream is null )
                throw new ArgumentNullException ( nameof ( stream ) );
            if ( bufferSize < 1 )
                throw new ArgumentOutOfRangeException ( nameof ( bufferSize ) );

            using var algo = HashAlgorithm.Create ( name.ToUpperInvariant ( ) );
            return WithAlgorithmAsync ( algo, stream, bufferSize, cancellationToken );
        }

        private static String WithAlgorithm ( HashAlgorithm algorithm, String data, Encoding encoding = null ) =>
            WithAlgorithm ( algorithm, ( encoding ?? Encoding.Default ).GetBytes ( data ) );

        private static String WithAlgorithm ( HashAlgorithm algorithm, Byte[] data ) =>
            ByteArrayToHex ( algorithm.ComputeHash ( data ) );

        private static String WithAlgorithm ( HashAlgorithm algorithm, Stream data ) =>
            ByteArrayToHex ( algorithm.ComputeHash ( data ) );

        [SuppressMessage ( "Performance", "CA1835:Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'", Justification = "Can't use Memory<T> with HashAlgorithm." )]
        private static async Task<String> WithAlgorithmAsync ( HashAlgorithm algorithm, Stream stream, Int32 bufferSize = 4 * FileSize.KiB, CancellationToken cancellationToken = default )
        {
            if ( stream is null )
                throw new ArgumentNullException ( nameof ( stream ) );

            var buffer = ArrayPool<Byte>.Shared.Rent ( bufferSize );
            try
            {
                var length = 0;
                while ( ( length = await stream.ReadAsync ( buffer, 0, bufferSize, cancellationToken ).ConfigureAwait ( false ) ) > 0 )
                {
                    algorithm.TransformBlock ( buffer, 0, length, null, 0 );
                }
                algorithm.TransformFinalBlock ( buffer, 0, 0 );

                return ByteArrayToHex ( algorithm.Hash );
            }
            finally
            {
                ArrayPool<Byte>.Shared.Return ( buffer );
            }
        }

        private static String ByteArrayToHex ( Byte[] array ) =>
            String.Concat ( Array.ConvertAll ( array, b => $"{b:x2}" ) );
    }
}