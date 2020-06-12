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

using System;                       // Action
using System.Windows;               // Window
using System.ComponentModel;        // CancelEventArgs
using System.Diagnostics;           // Process
using System.Threading.Tasks;       // Task

using Windows.Devices.SmartCards;   // SmartCardReader

using Pcsc;                         // CardName
using Pcsc.Common;                  // IccDetection

using Relianz.DeviceServer.Etc;     // MifareUltralightEtcTag

namespace Relianz.DeviceServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region public members
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = DeviceServerApp.AllPagesViewModel;

            RegisterCardEventHandlers();

            DeviceServerApp.Logger.Information( "Main window constructed." );

        } // ctor

        public bool TagOnReader { get => m_tagOnReader; set => m_tagOnReader = value; }
        #endregion

        #region private members
        private void Window_Closing( object sender, CancelEventArgs e )
        {
            e.Cancel = false;

            DeviceServerApp.Logger.Information( "Main window closed." );

        } // Window_Closing

        private void Start_Browser( object sender, RoutedEventArgs e )
        {
            string uri = DeviceServerApp.AllPagesViewModel.DeviceServerUri + "index.html";

            Process.Start( "cmd", "/C start " + uri );

            DeviceServerApp.Logger.Information( "URI = " + uri );
        }

        private void Rescan_Readers( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            DeviceServerApp.GetSmartcardReaders();
            RegisterCardEventHandlers();

        } // Rescan_Readers

        private void RegisterCardEventHandlers()
        {
            if( DeviceServerApp.CardReader != null )
            {
                DeviceServerApp.AllPagesViewModel.NfcTagAtr = "(no tag present)";

                // Register card event handlers:
                DeviceServerApp.CardReader.CardAdded += CardAdded;
                DeviceServerApp.CardReader.CardRemoved += CardRemoved;

            } // CardReader exists

        } // RegisterCardEventHandlers

        private void CardAdded( SmartCardReader sender, CardAddedEventArgs args )
        {
            try
            {
                if( sender != DeviceServerApp.CardReader )
                {                    
                    return;
                }
                
                TagOnReader = true;

                Task t = HandleTag( args.SmartCard );
                t.Wait();
            }
            catch( Exception x )
            {
                ;
            }

        } // CardAdded

        private void CardRemoved( SmartCardReader sender, CardRemovedEventArgs args )
        {
            try
            {
                if( sender == DeviceServerApp.CardReader )
                {
                    DeviceServerApp.Logger.Information( $"Tag removed from {DeviceServerApp.CardReader.Name}" );
                    TagOnReader = false;

                    DeviceServerApp.AllPagesViewModel.NfcTagAtr = "(no tag present)";
                }
            }
            catch( Exception x )
            {
                ;
            }

        } // CardRemoved

        private async Task HandleTag( SmartCard card )
        {
            try
            {
                Product p;

                using( SmartCardConnection connection = await card.ConnectAsync() )
                {
                    // Try to identify what type of tag it is:
                    IccDetection cardIdentification = new IccDetection( card, connection );
                    await cardIdentification.DetectCardTypeAsync();

                    String tagName = cardIdentification.PcscCardName.ToString();
                    String tagAtr = BitConverter.ToString( cardIdentification.Atr );

                    DeviceServerApp.Logger.Information( $"Handling tag {tagName}" );
                    DeviceServerApp.AllPagesViewModel.NfcTagAtr = tagAtr;

                    // Assert validity of tag:
                    bool validTag = (cardIdentification.PcscDeviceClass == DeviceClass.StorageClass)
                                    && (cardIdentification.PcscCardName == CardName.MifareUltralight);
                    if( !validTag )
                    {
                        if( tagName.Equals( "Unknown" ) )
                        {
                            ;
                        }
                        else
                        {
                            ;
                        }

                        ;

                        return;

                    } // invalid tag

                    // Create instance that handles tag data:
                    MifareUltralightEtcTag tag = new MifareUltralightEtcTag( connection );

                    // Read data from tag                
                    p = await tag.ReadProductData();

                } // using

                if( p != null )
                {
                    int productType = p.ProductType;        // 1;
                    long productID = p.ProductID;           // 8083602783975920776;
                    byte[] supplierAddr = p.SupplierAddr;   // Helpers.StringToByteArray( "c218b5b7bc390cbb16dcd591f0dceeb24348ee72fcbdb6e6ed060ccd6eb4fef552e16021040b33a6" );

                    string hint = $"Confirmation from ETC regarding the product with ID {productID}";
                }

            }
            catch( Exception x )
            {
                string msg = x.Message;
                ;
            }

        } // HandleTag

        private bool m_tagOnReader;
        #endregion

    } // class MainWindow

} // namespace Relianz.DeviceServer
