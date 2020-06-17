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

using Newtonsoft.Json;      // JsonConvert

namespace Relianz.DeviceServer
{
    public class Device
    {
		#region public members
		public enum DeviceType : ushort
		{
			NoDevice = 0,
			SmartCardReader = 10,
			NfcTag = 200,

		} // enum DeviceType

		public Device( DeviceType type, string model = "Unknown model", string identity = "Unknown identity" )
        {
			Type = type;
			TypeAsString = type.ToString();
			Model = model;
			Identity = identity;

        } // ctor

		public string ToJsonString()
		{
			string s = JsonConvert.SerializeObject( this );

			return s;

		} // ToJsonString

		public DeviceType Type { get => m_type; private set => m_type = value; }
		public string TypeAsString { get => m_typeAsString; private set => m_typeAsString = value; }
		public string Model { get => m_model; private set => m_model = value; }
        public string Identity { get => m_identity; private set => m_identity = value; }
        #endregion

        #region private members
        private DeviceType m_type;
		private string m_typeAsString;
		private string m_model;
		private string m_identity;
        #endregion

    } // class Device

} // namespace Relianz.DeviceServer
