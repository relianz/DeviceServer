/*
    The MIT License
    Copyright 2020, Dr.-Ing. Markus A. Stulle, Munich (markus@stulle.zone)
 
    Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
    and associated documentation files (the "Software"), to deal in the Software without restriction, 
    including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
    and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
    subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all copies 
    or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
    INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;                           // Guid
using System.IO;                        // File
using System.Text;                      // ASCIIEncoding

using Newtonsoft.Json;                  // JsonConvert

namespace Relianz.DeviceServer.Etc
{
    public class Thing
    {
        #region public members
        public enum ThingType : ulong
        {
            Unknown = 0,
            ExhaustSystem = 3,
            Engine = 50,
            UnderCarriage = 80,
            Digger = 9000

        } // enum ThingType

        public enum ExternalFormat : byte
        {
            Unknown = 0,
            Standard = 1,
            Tiny = 2,
            Display = 3

        } // enum ExternalFormat

        [JsonConstructor]
        public Thing( ThingType type, Guid id, DateTime? createdWhen )
        {
            // No thing without type!
            if( type == ThingType.Unknown )
            {
                DeviceServerApp.Logger.Error( $"Cannot construct thing with unknown type!" );
                throw new InvalidOperationException();
            }
            Type = type;
            TypeAsString = Type.ToString();

            // No thing without ID:
            if( id == Guid.Empty )
            {
                DeviceServerApp.Logger.Error( $"Cannot construct thing with empty ID!" );
                throw new InvalidOperationException();
            }
            Id = id;

            if( createdWhen != null )
                CreatedWhen = (DateTime)createdWhen;

        } // ctor

        public Thing( ThingType type )
        {
            Type = type;
            TypeAsString = Type.ToString();
            Id = Guid.NewGuid();

        } // ctor

        public Thing( string csvString, char separator = ',' )
        {
            m_stringSeparatorChar = separator;
            string[] attributes = csvString.Split( m_stringSeparatorChar );

            ExternalFormat ef = ExternalFormat.Unknown;

            // Read format specifier:
            switch( attributes[ 0 ] )
            {
                case "T":
                    ef = ExternalFormat.Tiny;       // T,TypeAsNumber,Id
                    break;

                case "S":
                    ef = ExternalFormat.Standard;   // S,TypeAsString,Id,CreatedWhen
                    break;

                case "D":
                    ef = ExternalFormat.Display;    // D,TypeAsNumber,TypeAsString,Id,CreatedWhen
                    break;

                default:
                    DeviceServerApp.Logger.Error( $"Invalid format specifier <{attributes[ 0 ]}>" );
                    throw new FormatException( "Invalid CSV format" );

            } // determine format.

            try
            {
                // Deserialize type of the thing:
                if( ef == ExternalFormat.Tiny || ef == ExternalFormat.Display )
                {
                    // Type has integer format, e.g. "9000":
                    Type = (ThingType)Convert.ToUInt64( attributes[ 1 ] );
                }
                else
                if( ef == ExternalFormat.Standard )
                {
                    // Type has string format, e.g. "Digger":
                    Type = (ThingType)Enum.Parse( typeof( ThingType ), attributes[ 1 ] );
                }

                TypeAsString = Type.ToString();

                // Deserialize the GUID of the thing:
                if( ef == ExternalFormat.Tiny || ef == ExternalFormat.Standard )
                {
                    Id = Guid.Parse( attributes[ 2 ] );
                }
                else
                if( ef == ExternalFormat.Display )
                {
                    Id = Guid.Parse( attributes[ 3 ] );
                }

                // Deserialize the birthday, if present:
                if( ef == ExternalFormat.Standard )
                {
                    CreatedWhen = DateTime.Parse( attributes[ 3 ] );
                }
                else
                if( ef == ExternalFormat.Display )
                {
                    CreatedWhen = DateTime.Parse( attributes[ 4 ] );
                }
            }
            catch( Exception ex )
            {
                DeviceServerApp.Logger.Error( ex.Message );
                throw;
            }

        } // ctor

        public static Thing FromByteArray( byte[] data )
        {
            ASCIIEncoding enc = new ASCIIEncoding();
            string  csvString = enc.GetString( data );

            return new Thing( csvString );

        } // FromByteArray

        public static Thing FromJsonString( string jsonString )
        {
            Thing t = null;

            try
            {
                t = JsonConvert.DeserializeObject<Thing>( jsonString );
            }
            catch( Exception x )
            {
                DeviceServerApp.Logger.Error( x.Message );
            }

            return t;

        } // FromJsonString

        public static Thing FromJsonFile( string pathToFile )
        {
            string json;

            try
            {
                json = File.ReadAllText( pathToFile );
                if( json.Length == 0 )
                {
                    DeviceServerApp.Logger.Error( $"Zero length JSON file {pathToFile}" );
                    return null;
                }
            }
            catch( Exception x )
            {
                DeviceServerApp.Logger.Error( x.Message );
                return null;

            } // FromJsonFile

            Thing  t = Thing.FromJsonString( json );
            return t;

        } // FromJsonFile

        public string ToJsonString( bool indented = false )
        {
            string s = indented ? JsonConvert.SerializeObject( this, Formatting.Indented )
                                : JsonConvert.SerializeObject( this );
            return s;

        } // ToJsonString

        public int ToJsonFile( string pathToFile, bool overwrite = false )
        {
            try
            {
                if( !overwrite )
                {
                    // Check if file already exists:
                    if( File.Exists( pathToFile ) )
                    {
                        string? pathName = Path.GetDirectoryName( pathToFile );
                        string fileName = Path.GetFileName( pathToFile );

                        if( pathName == null )
                            pathName = ".";

                        // make a copy of the file:
                        string backupFileName = fileName + ".backup";
                        File.Copy( pathToFile, Path.Combine( pathName, backupFileName ) );

                    } // file exists

                } // !overwrite

                // serialize object to JSON string with pretty printing:
                string json = ToJsonString( indented: true );

                // write JSON string to file:
                using( StreamWriter sw = new StreamWriter( pathToFile, false ) )
                {
                    sw.Write( json );
                }
            }
            catch( Exception x )
            {
                DeviceServerApp.Logger.Error( x.Message );
                return -1;
            }

            return 0;

        } // ToJsonFile

        public string ToTinyString()
        {
            ulong typeAsNumber = (ulong)Type;

            // T indicates tiny format: 
            // store type as integer, do not store TypeAsString, nor date/time of thing creation:
            string s = $"T{m_stringSeparatorChar}{typeAsNumber}{m_stringSeparatorChar}{Id}";

            return s;

        } // ToTinyString

        public override string ToString()
        {
            ulong typeAsNumber = (ulong)Type;

            // S indicates standard format:
            // store type as integer, do not store TypeAsString:
            string s = $"S{m_stringSeparatorChar}{typeAsNumber}{m_stringSeparatorChar}{Id}{m_stringSeparatorChar}{CreatedWhen}";

            return s;

        } // ToString

        public string ToDisplayString()
        {
            ulong typeAsNumber = (ulong)Type;

            // D indicates display format: 
            // store type both as integer and string:
            string s = $"D{m_stringSeparatorChar}{typeAsNumber}{m_stringSeparatorChar}{Type}{m_stringSeparatorChar}{Id}{m_stringSeparatorChar}{CreatedWhen}";

            return s;

        } // ToDisplayString

        public byte[] ToByteArray( Boolean tiny = false )
        {
            string s = tiny ? this.ToTinyString() : this.ToString();

            ASCIIEncoding enc = new ASCIIEncoding();
            return enc.GetBytes( s );

        } // ToByteArray

        public byte[] ToTagBuffer()
        {
            byte[] data = null;

            if( TagBufferSize <= 48 )
            {
                data = ToByteArray( tiny: true );
            }
            else
            {
                data = ToByteArray();
            }

            int dataLength = data.Length;

            if( dataLength > TagBufferSize )
            {
                DeviceServerApp.Logger.Fatal( $"Size of thing {dataLength} exceeds tag buffer size {TagBufferSize}" );
                return null;
            }

            byte[] tagBuffer = new byte[ TagBufferSize ];
            data.CopyTo( tagBuffer, 0 );

            // pad the remaining buffer:
            for( int i = dataLength; i < TagBufferSize; i++ )
            {
                tagBuffer[ i ] = Convert.ToByte( m_paddingChar );
            }

            return tagBuffer;

        } // ToTagBuffer

        public static Thing FromTagBuffer( byte[] buffer )
        {
            // find index of first padding character:
            bool found = false;
            int indexFirstPad;

            for( indexFirstPad = 0; indexFirstPad < buffer.Length; indexFirstPad++ )
            {
                if( buffer[ indexFirstPad ] == Convert.ToByte( m_paddingChar ) )
                {
                    found = true;
                    break;
                }
            }

            if( !found )
            {
                DeviceServerApp.Logger.Warning( $"No padding character!" );
            }

            byte[] csvData = new byte[ indexFirstPad ];
            for( int i = 0; i < indexFirstPad; i++ )
            {
                csvData[ i ] = buffer[ i ];
            }

            Thing  thing = Thing.FromByteArray( csvData );
            return thing;

        } // FromTagBuffer

        public static int SizeOf()
        {
            return TagBufferSize;

        } // SizeOf

        public ThingType Type { get => m_type; set => m_type = value; }
        public string TypeAsString { get => m_typeAsString; set => m_typeAsString = value; }
        public Guid Id { get => m_Id; set => m_Id = value; }
        public DateTime CreatedWhen { get => m_createdWhen; set => m_createdWhen = value; }
        #endregion

        #region private members

        // the type of the thing:
        private ThingType m_type;

        // not stored on tag:
        private string? m_typeAsString;

        // the globally unique ID of the thing:
        private Guid m_Id;

        // date and time of thing creation:
        private DateTime m_createdWhen;

        // character for CSV token separation:
        private static char m_stringSeparatorChar = ',';

        // character for padding fixed size string representation:
        private static char m_paddingChar = '*';

        // buffer for tag storage.
        // MIFARE Ultralight (MF0ICU1) das 48 bytes user memory (pages 4 to 15), see https://www.nxp.com/docs/en/data-sheet/MF0ICU1.pdf
        private const int TagBufferSize = 48;
        #endregion

    } // class Thing

} // namespace Relianz.DeviceServer.Etc 
