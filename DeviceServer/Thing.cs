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
using System.Text;                      // ASCIIEncoding

using Newtonsoft.Json;                  // JsonConvert

namespace Relianz.DeviceServer.Etc
{
    public class Thing
    {
        #region public members
        public enum ThingType : ulong
        {
            ExhaustSystem = 3,
            Engine = 50,
            UnderCarriage = 80,
            Digger = 9000

        } // enum ThingType

        public Thing( ThingType type, Guid Id )
        {
            Type = type;
            m_Id = Id;

            m_createdWhen = DateTime.Now;
        
        } // ctor

        public Thing( ThingType type )
        {
            Type = type;
            m_Id = Guid.NewGuid();

            m_createdWhen = DateTime.Now;

        } // ctor

        public Thing( string csvString, char separator = ',' )
        {
            m_stringSeparatorChar = separator;
            string[] attributes = csvString.Split( m_stringSeparatorChar );

            try
            {
                Type = (ThingType)Enum.Parse( typeof( ThingType ), attributes[ 0 ] );
                m_Id = Guid.Parse( attributes[ 1 ] );
                m_createdWhen = DateTime.Parse( attributes[ 2 ] );
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
            Thing t = JsonConvert.DeserializeObject<Thing>( jsonString );

            return t;

        } // FromJsonString

        public string ToJsonString()
        {
            string s = JsonConvert.SerializeObject( this );

            return s;

        } // ToJsonString

        public override string ToString()
        {
            string s = $"{Type.ToString()}{m_stringSeparatorChar}{m_Id.ToString()}{m_stringSeparatorChar}{m_createdWhen.ToString()}";

            return s;

        } // ToString

        public byte[] ToByteArray()
        {
            string s = this.ToString();

            ASCIIEncoding enc = new ASCIIEncoding();
            return enc.GetBytes( s );

        } // ToByteArray

        public static int SizeOf()
        {
            Thing dummy = new Thing( ThingType.Digger );
            byte[] data = dummy.ToByteArray();

            return data.Length;

        } // SizeOf

        public ThingType Type { get => m_type; set => m_type = value; }
        #endregion

        #region private members

        // the type of the thing:
        private ThingType m_type;

        // the globally unique ID of the thing:
        private Guid m_Id;

        // date and time of thing creation:
        private DateTime m_createdWhen;

        // character for CSV token separation:
        private static char m_stringSeparatorChar = ',';
        #endregion

    } // class Thing

} // namespace Relianz.DeviceServer.Etc 
