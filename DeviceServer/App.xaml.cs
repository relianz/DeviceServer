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

using System;                       // NotImplementedException
using System.Windows;               // Application
using System.Threading.Tasks;       // Task

using Windows.Devices.Enumeration;  // DeviceInformationCollection
using Windows.Devices.SmartCards;   // SmartCardReader

using Pcsc;                         // SmartCardReaderUtils
using Serilog;                      // ILogger

namespace Relianz.DeviceServer
{
    public partial class DeviceServerApp : Application
    {
        #region public members
        public static ILogger Logger { get => fileLogger; private set => fileLogger = value; }
        public static bool IsApplicationActive { get => isApplicationActive; private set => isApplicationActive = value; }
        public static HttpServer WebServer { get => webServer; private set => webServer = value; }
        public static ViewModel AllPagesViewModel { get => allPagesViewModel; private set => allPagesViewModel = value; }
        public static string AppLocalPath { get => appLocalPath; private set => appLocalPath = value; }
        public static SmartCardReader CardReader { get => m_cardReader; private set => m_cardReader = value; }

        public static void GetSmartcardReaders()
        {
            string selector = SmartCardReader.GetDeviceSelector();

            var t1 = Task.Run( () => SmartCardReaderUtils.GetAllSmartCardReaderInfo() );
            t1.Wait();
            DeviceInformationCollection deviceInfo = t1.Result;

            int numOfReaders;

            if( deviceInfo == null )
            {
                Logger.Fatal( "Couldn't query for smartcard readers" );
                numOfReaders = 0;
            }
            else
            {
                numOfReaders = deviceInfo.Count;
            }

            if( numOfReaders == 0 )
            {
                string msg = "No smartcard readers found";
                AllPagesViewModel.NfcReader = msg + " - double click here to rescan!";
                Logger.Error( msg );

                return;
            }

            m_cardReaders = new SmartCardReader[ numOfReaders ];
            int i = 0;
            foreach( var reader in deviceInfo )
            {
                try
                {
                    Task<SmartCardReader> t2 = SmartCardReader.FromIdAsync( reader.Id ).AsTask();
                    t2.Wait();
                    SmartCardReader r = t2.GetAwaiter().GetResult();

                    m_cardReaders[ i ] = r;

                    i++;
                }
                catch( Exception ex )
                {
                    Logger.Fatal( ex.Message );
                    return;

                } // catch

            } // foreach reader.

            // show NFC readers found:
            String readers = "";
            Logger.Information( $"Found {numOfReaders} reader(s)" );

            for( int r = 0; r < numOfReaders; r++ )
            {
                if( r > 0 )
                    readers += ", ";

                string name = m_cardReaders[ r ].Name;
                readers += name;

                Logger.Information( name );

            } // for all readers found

            // Show readers found on UI:
            AllPagesViewModel.NfcReader = readers;

            // Select first reader for operations:
            CardReader = m_cardReaders[ 0 ];

        } // GetSmartCardReaders
        #endregion
        #region protected members
        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );

            AppLocalPath = System.AppDomain.CurrentDomain.BaseDirectory;

            string path = null;

            Logger = FileLogger.GetLogger( ref path );
            Logger.Information( "App instance constructed." );

            AllPagesViewModel = new ViewModel();
            AllPagesViewModel.LogFileLocation = path;

            string domainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string hostName = System.Net.Dns.GetHostName();
            string serverDnsName = hostName + "." + domainName;
          
            Logger.Information( "Server DNS name = " + serverDnsName );

            WebServer = hostName.Equals( "SANTACLARA" ) ? new HttpServer( ipAddr: serverDnsName, port: 9090, rootDir: AppLocalPath )
                                                        : new HttpServer( ipAddr: "127.0.0.1",   port: 9090, rootDir: AppLocalPath );
            WebServer.Start();

            AllPagesViewModel.DeviceServerUri = WebServer.ServerUri;
            AllPagesViewModel.RootDirectory = AppLocalPath;

            GetSmartcardReaders();
            
        } // OnStartup

        void App_Activated( object sender, EventArgs e )
        {
            IsApplicationActive = true;
            Logger.Information( "App activated." );

        } // App_Activated

        void App_Deactivated( object sender, EventArgs e )
        {
            IsApplicationActive = false;
            Logger.Information( "App deactivated." );
        }
        #endregion
        #region private members
        private static SmartCardReader[] m_cardReaders;
        private static SmartCardReader   m_cardReader;

        // the FileLogger of this App:
        private static ILogger fileLogger;

        // Is our App active?
        private static bool isApplicationActive;

        // Our web server:
        private static HttpServer webServer;

        // the view model:
        private static ViewModel allPagesViewModel;

        // our root directory:
        private static string appLocalPath;
        #endregion

    } // class DeviceServerApp

} // namespace Relianz.DeviceServer
