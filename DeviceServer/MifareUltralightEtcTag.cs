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
        public async Task<int> WriteProductData( Product p )
        {
            byte[] supplierAddress = p.SupplierAddr;
            int productType = p.ProductType;
            long productID = p.ProductID;

            await CreateHandler();

            // Assert arguments:
            if( supplierAddress != null )
            {
                if( supplierAddress.Length != Product.AddressLength )
                {
                    return ErrorInvalidAddressLength;
                }

            } // supplierAddress != null
            else
            {
                return ErrorSupplierAddressNull;
            }

            // Create byte array from product data passed:
            int dataSize = SizeOfProductData;
            if( dataSize > EtcDataMaxNumOfBytes )
            {
                // Tag user memory too small to store data:
                return ErrorTagUserMemoryTooSmall;
            }

            // Allocate write buffer:
            byte[] data = new byte[ dataSize ];
            int i, offset = 0;


            // Serialize type of product: 
            if( m_useBitConverter )
            {
                byte[] byteArray = BitConverter.GetBytes( productType );
                byteArray.CopyTo( data, offset );

                offset += byteArray.Length;
            }
            else
            {
                for( i = 0; i < sizeof( int ); i++ )
                {
                    data[ offset + i ] = (byte)(productType >> i * 8);

                } // forall bytes in productType
                offset += i;
            }

            // Serialize ID of product: 
            if( m_useBitConverter )
            {
                byte[] byteArray = BitConverter.GetBytes( productID );
                byteArray.CopyTo( data, offset );

                offset += byteArray.Length;
            }
            else
            {
                for( i = 0; i < sizeof( long ); i++ )
                {
                    data[ offset + i ] = (byte)(productID >> i * 8);

                } // forall bytes in productType
                offset += i;
            }

            // Copy supplier address:
            supplierAddress.CopyTo( data, offset );

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

        } // IEtcTag.WriteProductData

        public async Task<Product> ReadProductData()
        {
            // at the time being, every call to the handler reads four pages:
            const int pagesPerReadAsync = 4;
            int bytesPerReadAsync = pagesPerReadAsync * BytesPerPage;

            // Calculate read buffer size:
            int readBufferSize = 0;
            while( readBufferSize <= EtcDataMaxNumOfBytes )
            {
                readBufferSize += bytesPerReadAsync;
                if( readBufferSize >= SizeOfProductData )
                {
                    break;
                }

            } // readBufferSize <= EtcDataMaxNumOfBytes

            Debug.Assert( readBufferSize >= SizeOfProductData );
            byte[] readBuffer = new byte[ readBufferSize ];

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

            // Process data read from tag:            
            int i, offset = 0;

            // Deserialize type of product: 
            int productType = 0;

            if( m_useBitConverter )
            {
                productType = BitConverter.ToInt32( readBuffer, offset );
                offset += sizeof( int );
            }
            else
            {
                for( i = 0; i < sizeof( int ); i++ )
                {
                    int k = readBuffer[ offset + i ] << (i * 8);
                    productType |= k;

                } // forall bytes in productType
                offset += i;
            }

            // Deserialize ID of product:
            long productID = 0;

            if( m_useBitConverter )
            {
                productID = BitConverter.ToInt64( readBuffer, offset );
                offset += sizeof( long );
            }
            else
            {
                for( i = 0; i < sizeof( long ); i++ )
                {
                    long k = readBuffer[ offset + i ] << (i * 8);
                    productID |= k;

                } // forall bytes in productType
                offset += i;
            }

            byte[] supplierAddress = new byte[ Product.AddressLength ];

            // Copy supplier address:
            Array.Copy( readBuffer, offset,
                        supplierAddress, 0,
                        Product.AddressLength );

            Product p = new Product( productType, productID, supplierAddress );
            return p;

        } // IEtcTag.ReadProductData
        #endregion

        #region Getter/setter
        public int SizeOfProductData
        {
            get => sizeof( int ) + sizeof( long ) + Product.AddressLength;

        } // SizeOfProductData

        public static int EtcDataMaxNumOfBytes => m_etcDataMaxNumOfBytes;
        public static byte FirstPageOfUserData => m_firstPageOfUserData;
        public static byte BytesPerPage => m_bytesPerPage;
        public static bool DryRun => m_dryRun;

        public int ErrorSupplierAddressNull => m_errorSupplierAddressNull;
        public int ErrorInvalidAddressLength => m_errorInvalidAddressLength;
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

        private const int m_etcDataMaxNumOfBytes = 144;
        private const byte m_firstPageOfUserData = 4;
        private const byte m_bytesPerPage = 4;

        private const bool m_useBitConverter = true;
        private const bool m_dryRun = false;

        private const int m_errorSupplierAddressNull = -1;
        private const int m_errorInvalidAddressLength = -2;
        private const int m_errorTagUserMemoryTooSmall = -3;
        private const int m_errorExceptionWhileWritingData = -4;

        #endregion

    } // class MifareUltralightEtcTag

} // namespace Authenticator.Etc
