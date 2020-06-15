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

using System;                           // Exception
using System.Diagnostics;               // Debug
using System.Threading.Tasks;           // Task

using Windows.Devices.SmartCards;       // SmartCardConnection

using MifareUltralight;                 // AccessHandler

namespace Relianz.DeviceServer.Etc
{
    public class MifareUltralightEtcTag : IEtcTag
    {
        #region Public members

        public MifareUltralightEtcTag( SmartCard card )
        {
            m_card = card;

        } // ctor

        #region Implementation of IEtcTag

        public async Task<int> WriteThingData( Thing t )
        {
            byte[]  data = t.ToByteArray();

            int dataSize = data.Length;
            if( dataSize > EtcDataMaxNumOfBytes )
            {
                // Tag user memory too small to store data:
                return ErrorTagUserMemoryTooSmall;
            }

            await CreateHandler();

            // at the time being, every call to the handler writes a single page:
            const int pagesPerWriteAsync = 1;

            // TODO: Create padded write buffer!

            int bytesWritten = 0;

            bool DryRun = false;
            if( !DryRun )
            {
                int numOfBytes = data.Length;
                int numOfPages = numOfBytes / BytesPerPage;
                int numOfCalls = numOfPages / pagesPerWriteAsync;

                byte pageAddress = 0;

                try
                {
                    for( byte j = 0; j < numOfCalls; j++ )
                    {
                        pageAddress = (byte)(FirstPageOfUserData + j * pagesPerWriteAsync);

                        // Compute data source index:
                        int index = j * pagesPerWriteAsync * BytesPerPage;
                        byte[] bytesForThisCall = data.SubArray( index, pagesPerWriteAsync * BytesPerPage );
#if DEBUG
                        if( j == (numOfCalls - 1) )
                        {
                            ;

                        } // last write 
#endif
                        // Write data to tag:
                        await m_handler.WriteAsync( pageAddress, bytesForThisCall );
                        bytesWritten += bytesForThisCall.Length;

                    } // for all write ops 
                }
                catch( Exception x )
                {
                    string msg = x.Message;

                    // Log error:
                    DeviceServerApp.Logger.Error( $"At page addr {pageAddress} - {msg}" );

                    // Report error:
                    return ErrorExceptionWhileWritingData;

                } // Exception 

            } // !dryRun )

            // Report success:
            DeviceServerApp.Logger.Information( $"{bytesWritten} bytes written" );
            return 0;

        } // IEtcTag.WriteThingData

        public async Task<Thing> ReadThingData()
        {
            // at the time being, every call to the handler reads four pages:
            const int pagesPerReadAsync = 4;
            int bytesPerReadAsync = pagesPerReadAsync * BytesPerPage;

            // Calculate read buffer size:
            int thingSize = Thing.SizeOf();
            int readBufferSize = 0;
            while( readBufferSize <= EtcDataMaxNumOfBytes )
            {
                readBufferSize += bytesPerReadAsync;
                if( readBufferSize >= thingSize )
                {
                    break;
                }

            } // readBufferSize <= EtcDataMaxNumOfBytes

            Debug.Assert( readBufferSize >= thingSize );
            byte[] readBuffer = new byte[ readBufferSize ];

            DeviceServerApp.Logger.Information( $"Thing size = {thingSize}" );
            DeviceServerApp.Logger.Information( $"Read buffer size = {readBufferSize}" );

            int numOfBytes;
            int numOfPages;
            int numOfCalls;

            byte[] response = null;

            bool DryRun = false;
            if( !DryRun )
            {
                await CreateHandler();

                try
                {
                    numOfBytes = readBuffer.Length;
                    numOfPages = numOfBytes / BytesPerPage;
                    numOfCalls = numOfPages / pagesPerReadAsync;

                    for( byte j = 0; j < numOfCalls; j++ )
                    {
                        // Read data from tag:
                        byte pageAddress = (byte)(FirstPageOfUserData + j * pagesPerReadAsync);

                        response = await m_handler.ReadAsync( pageAddress );

                        // copy tag response to read buffer:
                        int readBufferIndex = j * bytesPerReadAsync;
                        response.CopyTo( readBuffer, readBufferIndex );

                    } // for all read ops
                }
                catch( Exception x )
                {
                    string msg = x.Message;

                    // Log error:
                    DeviceServerApp.Logger.Error( msg );

                    // Report error:
                    return null;

                } // Exception 

            } // !DryRun

            Thing t = Thing.FromByteArray( readBuffer );

            return t;

        } // IEtcTag.ReadThingData

        #endregion

        #region Getter/setter
        public static int EtcDataMaxNumOfBytes => m_etcDataMaxNumOfBytes;
        public static byte FirstPageOfUserData => m_firstPageOfUserData;
        public static byte BytesPerPage => m_bytesPerPage;
        public static bool DryRun => m_dryRun;

        public int ErrorTagUserMemoryTooSmall => m_errorTagUserMemoryTooSmall;
        public int ErrorExceptionWhileWritingData => m_errorExceptionWhileWritingData;
        #endregion

        #endregion

        #region Private members

        private async Task CreateHandler()
        {
            SmartCardConnection connection = await m_card.ConnectAsync();
            m_handler = new AccessHandler( connection );

        } // CreateHandler

        private SmartCard m_card;
        private AccessHandler m_handler;

        private const byte m_firstPageOfUserData = 4;
        private const byte m_bytesPerPage = 4;
        private const bool m_dryRun = false;

        private const int  m_etcDataMaxNumOfBytes = 144;
        private const int  m_errorTagUserMemoryTooSmall = -3;
        private const int  m_errorExceptionWhileWritingData = -4;

        #endregion

    } // class MifareUltralightEtcTag

} // namespace Authenticator.Etc
