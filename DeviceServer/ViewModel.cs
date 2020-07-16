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
using System.Reflection;                // Assembly
using System.Windows;                   // MessageBox
using System.IO;                        // Path

namespace Relianz.DeviceServer
{
    public class ViewModel : INotifyPropertyChanged
    {
        #region public members
        public ViewModel()
        {
            m_productName = "DeviceServer";

            Assembly assembly = typeof( DeviceServerApp ).Assembly;
            foreach( var attr in assembly.GetCustomAttributes() )
            {
                switch( attr )
                {
                    case AssemblyProductAttribute a:
                        m_productName = a.Product;
                        break;

                    default:
                        break;
                }

            } // for each custom attribute

            m_assyVersion = assembly.GetName().Version.ToString();
            m_coreVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.ToString();

            WindowTitle = ProductName + ", Version " + AssyVersion;

            DeviceServerApp.Logger.Information( $"ViewModel created." );
            DeviceServerApp.Logger.Information( $"Product = {ProductName}" );
            DeviceServerApp.Logger.Information( $"App version = {AssyVersion}, Core version = {CoreVersion}" );

        } // ctor 

        public string ProductName
        {
            get => m_productName;
            set
            {
                if( value != m_productName )
                {
                    m_productName = value;
                    OnPropertyChanged( "ProductName" );
                }

            } // set

        } // LogFileLocation

        public string WindowTitle
        {
            get => m_windowTitle;
            set
            {
                if( value != m_windowTitle )
                {
                    m_windowTitle = value;
                    OnPropertyChanged( "WindowTitle" );
                }

            } // set

        } // LogFileLocation

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

        public bool TagOnReader 
        { 
            get => m_tagOnReader; 
            set 
            {
                if( value != m_tagOnReader )
                {
                    m_tagOnReader = value;
                    OnPropertyChanged( "TagOnReader" );
                }

            } // set

        } // TagOnReader 

        public string NfcTagData 
        { 
            get => m_NfcTagData;
            set
            {
                if( value != m_NfcTagData )
                {
                    m_NfcTagData = value;
                    OnPropertyChanged( "NfcTagData" );
                }

            } // set

        } // NfcTagData 

        public string NfcTagUid 
        { 
            get => m_NfcTagUid;
            set
            {
                if( value != m_NfcTagUid )
                {
                    m_NfcTagUid = value;
                    OnPropertyChanged( "NfcTagUid" );
                }

            } // set

        } // NfcTagUid

        public bool EmulationMode 
        { 
            get => m_emulationMode;
            set
            {
                if( value != m_emulationMode )
                {
                    if( !m_emulationMode )
                    {
                        // try to switch to emulation mode:
                        if( SwitchToEmulationMode() )
                        {
                            m_emulationMode = true;
                            OnPropertyChanged( "EmulationMode" );
                        }
                    }
                    else
                    {
                        // try to switch to NFC tag mode:
                        if( SwitchToNfcMode() )
                        {
                            m_emulationMode = false;
                            OnPropertyChanged( "EmulationMode" );
                        }
                    }

                } // value changed

            } // set

        } // EmulationMode

        public string EmulationFile { get => m_emulationFile; private set => m_emulationFile = value; }
        public string AssyVersion { get => m_assyVersion; }
        public string CoreVersion { get => m_coreVersion; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string propertyName )
        {
            if( PropertyChanged != null )
                PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );

        } // OnPropertyChanged
        #endregion

        #region private members
        private bool SwitchToEmulationMode()
        {
            EmulationFile = Path.Combine( DeviceServerApp.AllPagesViewModel.RootDirectory, "Thing.json" );
            if( File.Exists( EmulationFile ) )
            {
                TagOnReader = true;

                NfcTagAtr = "[using file emulation]";
                NfcTagUid = "[using file emulation]";

                DeviceServerApp.Logger.Information( $"Susscess, using file {EmulationFile}" );

                return true;

            } // emulation file exists.
            else
            {
                string msg = $"Missing emulation file\n{EmulationFile}";

                DeviceServerApp.Logger.Error( msg );
                MessageBox.Show( msg, "DeviceServer emulation mode", MessageBoxButton.OK );

                return false;

            } // emulation file missing.

        } // SwitchToEmulationMode

        private bool SwitchToNfcMode()
        {
            // TODO: Check availability of tag!

            NfcTagAtr = "(no tag present)";
            NfcTagUid = "(no tag present)";

            TagOnReader = false;

            return true;

        } // SwitchToNfcMode

        private string m_logFileLocation;

        private string m_deviceServerUri;
        private string m_rootDirectory;
        private string m_emulationFile;

        private string m_NfcReader;
        private string m_NfcTagAtr;
        private string m_NfcTagUid;
        private string m_NfcTagData;

        private bool m_tagOnReader;
        private bool m_emulationMode;

        private string m_assyVersion;
        private string m_coreVersion;
        private string m_productName;
        private string m_windowTitle;
        #endregion

    } // class ViewModel

} // namespace Relianz.DeviceServer
