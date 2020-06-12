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

using System.ComponentModel;            // INotifyPropertyChanged

namespace Relianz.DeviceServer
{
    public class ViewModel : INotifyPropertyChanged
    {
        #region public members
        public ViewModel()
        {
            DeviceServerApp.Logger.Information( "ViewModel created." );

        } // ctor 

        public string LogFileLocation
        {
            get => m_logFileLocation;
            set
            {
                if( value != m_logFileLocation )
                {
                    m_logFileLocation = value;
                    OnPropertyChanged( "LogFileLocation" );
                }

            } // set

        } // LogFileLocation

        public string DeviceServerUri
        {
            get => m_deviceServerUri;
            set
            {
                if( value != m_deviceServerUri )
                {
                    m_deviceServerUri = value;
                    OnPropertyChanged( "DeviceServerUri" );
                }

            } // set

        } // DeviceServerUri

        public string RootDirectory
        {
            get => m_rootDirectory;
            set
            {
                if( value != m_rootDirectory )
                {
                    m_rootDirectory = value;
                    OnPropertyChanged( "RootDirectory" );
                }

            } // set

        } // RootDirectory

        public string NfcReader
        {
            get => m_NfcReader;
            set
            {
                if( value != m_NfcReader )
                {
                    m_NfcReader = value;
                    OnPropertyChanged( "NfcReader" );
                }

            } // set

        } // NfcReader 

        public string NfcTagAtr 
        { 
            get => m_NfcTagAtr; 
            set
            {
                if( value != m_NfcTagAtr )
                {
                    m_NfcTagAtr = value;
                    OnPropertyChanged( "NfcTagAtr" );
                }

            } // set

        } // NfcTagAtr 

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string propertyName )
        {
            if( PropertyChanged != null )
                PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );

        } // OnPropertyChanged
        #endregion

        #region private members
        private string m_logFileLocation;

        private string m_deviceServerUri;
        private string m_rootDirectory;

        private string m_NfcReader;
        private string m_NfcTagAtr;
        #endregion

    } // class ViewModel

} // namespace Relianz.DeviceServer
