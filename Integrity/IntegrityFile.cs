using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace Integrity
{
    internal readonly struct IntegrityFile
    {
        public readonly struct Entry
        {
            public readonly String RelativePath;
            public readonly String HexDigest;

            public Entry ( String path, String digest )
            {
                this.RelativePath = path;
                this.HexDigest = digest;
            }
        }

        private static readonly Byte[] MagicNumber =  new[]
        {
            ( Byte ) 'I',
            ( Byte ) 'N',
            ( Byte ) 'T',
            ( Byte ) 'E',
            ( Byte ) 'G',
            ( Byte ) 'R',
            ( Byte ) 'I',
            ( Byte ) 'T',
            ( Byte ) 'Y'
        };

        public const Int32 Version = 2;

        public readonly String HashAlgorithm;
        public readonly ImmutableArray<Entry> Entries;

        public IntegrityFile ( String algorithm, IEnumerable<Entry> entries )
        {
            this.HashAlgorithm = algorithm;
            this.Entries = entries.ToImmutableArray ( );
        }

        public void WriteTo ( Stream stream )
        {
            using ( var writer = new BinaryWriter ( stream, Encoding.UTF8, true ) )
            {
                writer.Write ( MagicNumber );
                writer.Write ( Version );
                writer.Write ( this.HashAlgorithm );
                writer.Write ( this.Entries.Length );
                foreach ( Entry entry in this.Entries )
                {
                    writer.Write ( entry.RelativePath );
                    writer.Write ( entry.HexDigest );
                }
            }
        }

        public static IntegrityFile ReadFrom ( Stream stream )
        {
            using ( var reader = new BinaryReader ( stream, Encoding.UTF8, true ) )
            {
                var buff = new Byte[MagicNumber.Length];
                reader.Read ( buff );
                if ( !MagicNumber.SequenceEqual ( buff ) )
                    throw new FormatException ( "Invalid file header" );

                var version = reader.ReadInt32 ( );
                if ( Version < version )
                    throw new FormatException ( "Unsupported file version" );

                var alg = reader.ReadString ( );

                // Backwards compatibility with v1
                if ( version == 1 )
                {
                    var globsLen = reader.ReadInt32 ( );
                    if ( globsLen >= 0 && globsLen <= 25 )
                        for ( var i = 0; i < globsLen; i++ )
                            reader.ReadString ( );
                }

                var fileLen = reader.ReadInt32 ( );
                if ( fileLen < 0 )
                    throw new FormatException ( "Invalid file count" );
                var files = new Entry[fileLen];
                for ( var i = 0; i < fileLen; i++ )
                    files[i] = new Entry ( reader.ReadString ( ), reader.ReadString ( ) );

                return new IntegrityFile ( alg, files );
            }
        }
    }
}
