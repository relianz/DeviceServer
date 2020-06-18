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

        public static MifareUltralightEtcTag NfcTag { get => m_NfcTag; private set => m_NfcTag = value; }
        #endregion

        #region private members
        private void Window_Closing( object sender, CancelEventArgs e )
        {
            e.Cancel = false;

            DeviceServerApp.WebServer.Stop();
            DeviceServerApp.Logger.Information( "Main window closed." );

        } // Window_Closing

        private void Start_Browser( object sender, RoutedEventArgs e )
        {
            string uri = DeviceServerApp.AllPagesViewModel.DeviceServerUri + "index.html";

            Process.Start( "cmd", "/C start " + uri );

            DeviceServerApp.Logger.Information( "URI = " + uri );

        } // Start_Browser

        private void Read_Identity( object sender, RoutedEventArgs e )
        {
            Task<Thing> t = Task.Run( () => NfcTag.ReadThingData() );

            int? tCurrentId = Task.CurrentId;
            int tId = (tCurrentId == null) ? -1 : (int)tCurrentId;
            DeviceServerApp.Logger.Information( $"In task {tId} - Waiting for task {t.Id} to complete" );
            t.Wait();

            Thing thing = t.Result;
            if( thing != null )
            {
                DeviceServerApp.Logger.Information( "Success" );
                DeviceServerApp.AllPagesViewModel.NfcTagData = thing.ToDisplayString();
            }
            else
                DeviceServerApp.Logger.Error( $"Reading Identity failed" );

        } // Read_Identity

        private void Write_Identity( object sender, RoutedEventArgs e )
        {
            // example thing:
            Thing thing = new Thing( Thing.ThingType.ExhaustSystem );
           
            Task<int> t = Task.Run( () => NfcTag.WriteThingData( thing ) );

            int? tCurrentId = Task.CurrentId;
            int tId = (tCurrentId == null) ? -1 : (int)tCurrentId;
            DeviceServerApp.Logger.Information( $"In task {tId} - Waiting for task {t.Id} to complete" );
            t.Wait();

            int err = t.Result;
            if( err == 0 )
            {
                DeviceServerApp.Logger.Information( "Success" );
                DeviceServerApp.AllPagesViewModel.NfcTagData = thing.ToDisplayString();
            }
            else
                DeviceServerApp.Logger.Error( $"Writing Identity failed {err}" );

        } // Write_Identity

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
                DeviceServerApp.AllPagesViewModel.NfcTagUid = "(no tag present)";

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

                DeviceServerApp.Logger.Information( $"Tag added to {DeviceServerApp.CardReader.Name}" );
                DeviceServerApp.AllPagesViewModel.TagOnReader = true;

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
                    DeviceServerApp.AllPagesViewModel.TagOnReader = false;

                    DeviceServerApp.AllPagesViewModel.NfcTagAtr  = "(no tag present)";
                    DeviceServerApp.AllPagesViewModel.NfcTagUid  = "(no tag present)";
                    DeviceServerApp.AllPagesViewModel.NfcTagData = "";
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
                // Connect to the tag:
                using( SmartCardConnection connection = await card.ConnectAsync() )
                {
                    // Try to identify what type of card it was
                    IccDetection cardIdentification = new IccDetection( card, connection );
                    await cardIdentification.DetectCardTypeAsync();

                    string tagClass = cardIdentification.PcscDeviceClass.ToString();
                    string tagName = cardIdentification.PcscCardName.ToString();
                    string tagAtr = BitConverter.ToString( cardIdentification.Atr );

                    DeviceServerApp.Logger.Information( $"Connected to tag - PC/SC device class {tagClass}, tag name {tagName}" );
                    DeviceServerApp.Logger.Information( $"ATR {tagAtr}" );
                    DeviceServerApp.AllPagesViewModel.NfcTagAtr = tagAtr;

                    if( (cardIdentification.PcscDeviceClass == Pcsc.Common.DeviceClass.StorageClass) &&
                        (cardIdentification.PcscCardName == Pcsc.CardName.MifareUltralightC
                        || cardIdentification.PcscCardName == Pcsc.CardName.MifareUltralight
                        || cardIdentification.PcscCardName == Pcsc.CardName.MifareUltralightEV1) )
                    {
                        // Handle MIFARE Ultralight:
                        NfcTag = new MifareUltralightEtcTag( card );
                        MifareUltralight.AccessHandler mifareULAccess = new MifareUltralight.AccessHandler( connection );

                        // Each read should get us 16 bytes/4 blocks, so doing
                        // 4 reads will get us all 64 bytes/16 blocks on the card:
                        for( byte i = 0; i < 4; i++ )
                        {
                            byte[] response = await mifareULAccess.ReadAsync( (byte)(4 * i) );
                            DeviceServerApp.Logger.Information( "Block " + (4 * i).ToString() + " to Block " + (4 * i + 3).ToString() + " " + BitConverter.ToString( response ) );
                        }

                        byte[] responseUid = await mifareULAccess.GetUidAsync();

                        string uidStr = BitConverter.ToString( responseUid );
                        DeviceServerApp.Logger.Information( "UID = " + uidStr );
                        DeviceServerApp.AllPagesViewModel.NfcTagUid = uidStr;
                    }
                    else if( cardIdentification.PcscDeviceClass == Pcsc.Common.DeviceClass.MifareDesfire )
                    {
                        // Handle MIFARE DESfire
                        Desfire.AccessHandler desfireAccess = new Desfire.AccessHandler( connection );
                        Desfire.CardDetails desfire = await desfireAccess.ReadCardDetailsAsync();

                        DeviceServerApp.Logger.Information( "DesFire Card Details:  " + Environment.NewLine + desfire.ToString() );
                    }
                    else if( cardIdentification.PcscDeviceClass == Pcsc.Common.DeviceClass.StorageClass
                        && cardIdentification.PcscCardName == Pcsc.CardName.FeliCa )
                    {
                        // Handle Felica
                        DeviceServerApp.Logger.Information( "Felica card detected" );

                        var felicaAccess = new Felica.AccessHandler( connection );
                        var uid = await felicaAccess.GetUidAsync();

                        DeviceServerApp.Logger.Information( "UID:  " + BitConverter.ToString( uid ) );
                    }
                    else if( cardIdentification.PcscDeviceClass == Pcsc.Common.DeviceClass.StorageClass
                        && (cardIdentification.PcscCardName == Pcsc.CardName.MifareStandard1K || cardIdentification.PcscCardName == Pcsc.CardName.MifareStandard4K) )
                    {
                        // Handle MIFARE Standard/Classic
                        DeviceServerApp.Logger.Information( "MIFARE Standard/Classic card detected" );

                        var mfStdAccess = new MifareStandard.AccessHandler( connection );
                        var uid = await mfStdAccess.GetUidAsync();
                        DeviceServerApp.Logger.Information( "UID:  " + BitConverter.ToString( uid ) );

                        ushort maxAddress = 0;
                        switch( cardIdentification.PcscCardName )
                        {
                            case Pcsc.CardName.MifareStandard1K:
                                maxAddress = 0x3f;
                                break;
                            case Pcsc.CardName.MifareStandard4K:
                                maxAddress = 0xff;
                                break;
                        }
                        await mfStdAccess.LoadKeyAsync( MifareStandard.DefaultKeys.FactoryDefault );

                        for( ushort address = 0; address <= maxAddress; address++ )
                        {
                            var response = await mfStdAccess.ReadAsync( address, Pcsc.GeneralAuthenticate.GeneralAuthenticateKeyType.MifareKeyA );
                            DeviceServerApp.Logger.Information( "Block " + address.ToString() + " " + BitConverter.ToString( response ) );
                        }
                    }
                    else if( cardIdentification.PcscDeviceClass == Pcsc.Common.DeviceClass.StorageClass
                        && (cardIdentification.PcscCardName == Pcsc.CardName.ICODE1 ||
                            cardIdentification.PcscCardName == Pcsc.CardName.ICODESLI ||
                            cardIdentification.PcscCardName == Pcsc.CardName.iCodeSL2) )
                    {
                        // Handle ISO15693
                        DeviceServerApp.Logger.Information( "ISO15693 card detected" );

                        var iso15693Access = new Iso15693.AccessHandler( connection );
                        var uid = await iso15693Access.GetUidAsync();
                        DeviceServerApp.Logger.Information( "UID:  " + BitConverter.ToString( uid ) );
                    }
                    else
                    {
                        // Unknown card type
                        // Note that when using the XDE emulator the card's ATR and type is not passed through, so we'll
                        // end up here even for known card types if using the XDE emulator

                        // Some cards might still let us query their UID with the PC/SC command, so let's try:
                        var apduRes = await connection.TransceiveAsync( new Pcsc.GetUid() );
                        if( !apduRes.Succeeded )
                        {
                            DeviceServerApp.Logger.Error( "Failure getting UID of card, " + apduRes.ToString() );
                        }
                        else
                        {
                            DeviceServerApp.Logger.Information( "UID:  " + BitConverter.ToString( apduRes.ResponseData ) );
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                DeviceServerApp.Logger.Fatal( "Exception handling card: " + ex.ToString() );
            }

        } // HandleTag

        private static MifareUltralightEtcTag m_NfcTag;
        #endregion

    } // class MainWindow

} // namespace Relianz.DeviceServer
