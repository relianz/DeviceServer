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

using System;           // Array
using System.Text;      // StringBuilder

namespace Relianz.DeviceServer
{
    static class Helpers
    {
        public static T[] SubArray<T>( this T[] data, int index, int length )
        {
            T[] result = new T[ length ];
            Array.Copy( data, index, result, 0, length );

            return result;

        } // SubArray

        public static string ByteArrayToString( byte[] ba )
        {
            StringBuilder hex = new StringBuilder( ba.Length * 2 );

            foreach( byte b in ba )
                hex.AppendFormat( "{0:x2}", b );

            return hex.ToString();

        } // ByteArrayToString

        public static byte[] StringToByteArray( String hex )
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[ NumberChars / 2 ];

            for( int i = 0; i < NumberChars; i += 2 )
                bytes[ i / 2 ] = Convert.ToByte( hex.Substring( i, 2 ), 16 );

            return bytes;

        } // StringToByteArray

        public static long LongRandom( long min, long max, Random rand )
        {
            byte[] buf = new byte[ 8 ];
            rand.NextBytes( buf );
            long longRand = BitConverter.ToInt64( buf, 0 );

            return Math.Abs( longRand % (max - min) ) + min;

        } // LongRandom

        public static string ShrinkPath( string path, int maxLength )
        {
            var parts = path.Split( '\\' );
            var output = String.Join( "\\", parts, 0, parts.Length );

            var endIndex = (parts.Length - 1);
            var startIndex = endIndex / 2;
            var index = startIndex;

            var step = 0;

            while( output.Length >= maxLength && index != 0 && index != endIndex )
            {
                parts[ index ] = "...";
                output = String.Join( "\\", parts, 0, parts.Length );
                if( step >= 0 ) step++;
                step = (step * -1);
                index = startIndex + step;
            }

            return output;

        } // ShrinkPath

    } // class Helpers

} // namespace Authenticator
