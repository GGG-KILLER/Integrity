using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Crypto = System.Security.Cryptography;

namespace Integrity.Util
{
    public static class Hash
    {
        public static String MD5 ( String data, Encoding encoding = null )
        {
            using var md5 = Crypto.MD5.Create ( );
            return WithAlgorithm ( md5, data, encoding );
        }

        public static String MD5 ( Byte[] data )
        {
            using var md5 = Crypto.MD5.Create ( );
            return WithAlgorithm ( md5, data );
        }

        public static String MD5 ( Stream data )
        {
            using var md5 = Crypto.MD5.Create ( );
            return WithAlgorithm ( md5, data );
        }

        public static String SHA1 ( String data, Encoding encoding = null )
        {
            using var sha1 = Crypto.SHA1.Create ( );
            return WithAlgorithm ( sha1, data, encoding );
        }

        public static String SHA1 ( Byte[] data )
        {
            using var sha1 = Crypto.SHA1.Create ( );
            return WithAlgorithm ( sha1, data );
        }

        public static String SHA1 ( Stream data )
        {
            using var sha1 = Crypto.SHA1.Create ( );
            return WithAlgorithm ( sha1, data );
        }

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

        public static String WithAlgorithm ( String name, String data, Encoding encoding = null )
        {
            using var algo = HashAlgorithm.Create ( name.ToUpper ( ) );
            return WithAlgorithm ( algo, data, encoding );
        }

        public static String WithAlgorithm ( String name, Byte[] data )
        {
            using var algo = HashAlgorithm.Create ( name.ToUpper ( ) );
            return WithAlgorithm ( algo, data );
        }

        public static String WithAlgorithm ( String name, Stream data )
        {
            using var algo = HashAlgorithm.Create ( name.ToUpper ( ) );
            return WithAlgorithm ( algo, data );
        }

        private static String WithAlgorithm ( HashAlgorithm algorithm, String data, Encoding encoding = null )
        {
            encoding ??= Encoding.UTF8;
            return WithAlgorithm ( algorithm, encoding.GetBytes ( data ) );
        }

        private static String WithAlgorithm ( HashAlgorithm algorithm, Byte[] data ) =>
            String.Concat ( Array.ConvertAll ( algorithm.ComputeHash ( data ), b => $"{b:x2}" ) );

        private static String WithAlgorithm ( HashAlgorithm algorithm, Stream data ) =>
            String.Concat ( Array.ConvertAll ( algorithm.ComputeHash ( data ), b => $"{b:x2}" ) );
    }
}