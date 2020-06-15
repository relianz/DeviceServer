﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System.Threading.Tasks;
using Windows.Devices.SmartCards;

using Pcsc;
using System;

using Relianz.DeviceServer.Etc;     // MifareUltralightEtcTag
using Relianz.DeviceServer;
using System.Reflection.Metadata.Ecma335;

namespace MifareUltralight
{
    /// <summary>
    /// Access handler class for MifareUL based ICC. It provides wrappers for different MifareUL 
    /// commands
    /// </summary>
    public class AccessHandler
    {
        /// <summary>
        /// connection object to smart card
        /// </summary>
        private SmartCardConnection connectionObject { set; get; }
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="ScConnection">
        /// connection object to a MifareUL ICC
        /// </param>
        public AccessHandler(SmartCardConnection ScConnection)
        {
            connectionObject = ScConnection;
        }
        /// <summary>
        /// Wrapper method to read 16 bytes (4 pages) starting at pageAddress
        /// </summary>
        /// <param name="pageAddress">
        /// start page to read
        /// </param>
        /// <returns>
        /// byte array of 16 bytes
        /// </returns>
        public async Task<byte[]> ReadAsyncOriginal( byte pageAddress )
        {
            var apduRes = await connectionObject.TransceiveAsync( new MifareUltralight.Read( pageAddress ) );

            if( !apduRes.Succeeded )
            {
                throw new Exception("Failure reading MIFARE Ultralight card, " + apduRes.ToString());
            }

            return apduRes.ResponseData;

        } // ReadAsyncOriginal

        public async Task<byte[]> ReadAsync( byte pageAddress )
        {
            Iso7816.ApduCommand cmd = new MifareUltralight.Read( pageAddress );

            DeviceServerApp.Logger.Information( "Transceiving <" + cmd.ToString() + ">" );
            var apduRes = await connectionObject.TransceiveAsync( cmd );

            DeviceServerApp.Logger.Information( "Got response <" + apduRes.ToString() + ">" );
            if( !apduRes.Succeeded )
            {
                throw new Exception( "Failure reading MIFARE Ultralight card, " + apduRes.ToString() );
            }

            return apduRes.ResponseData;

        } // ReadAsync

        /*
        public async Task ReadAsyncExtended( byte pageAddress )
        {
            Iso7816.ApduCommand cmd = new MifareUltralight.Read( pageAddress );

            DeviceServerApp.Logger.Information( "Transceiving <" + cmd.ToString() +">" );
            var apduRes = await connectionObject.TransceiveAsync( cmd );
         
            if( !apduRes.Succeeded )
            {
                throw new Exception( "Failure reading MIFARE Ultralight card, " + apduRes.ToString() );
            }

            int len = apduRes.ResponseData.Length;
            MifareUltralightEtcTag.responseBuffer[ 0 ] = (byte)len;
            apduRes.ResponseData.CopyTo( MifareUltralightEtcTag.responseBuffer, 1 );

            return;

        } // ReadAsyncExtended
        */

        /// <summary>
        /// Wrapper method write 4 bytes at the pageAddress
        /// </param name="pageAddress">
        /// page address to write
        /// </param>
        /// byte array of the data to write
        /// </returns>
        public async Task WriteAsyncOriginal( byte pageAddress, byte[] data )
        {
            if( data.Length != 4 )
            {
                throw new NotSupportedException();
            }

            var apduRes = await connectionObject.TransceiveAsync( new MifareUltralight.Write( pageAddress, ref data ) );

            if( !apduRes.Succeeded )
            {
                Exception exception = new Exception( "Failure writing MIFARE Ultralight card, " + apduRes.ToString() );
                throw exception;
            }

        } // WriteAsyncOriginal

        public async Task WriteAsync( byte pageAddress, byte[] data )
        {
            if( data.Length != 4 )
            {
                throw new NotSupportedException();
            }

            Iso7816.ApduCommand cmd = new MifareUltralight.Write( pageAddress, ref data );

            DeviceServerApp.Logger.Information( "Transceiving <" + cmd.ToString() + ">" );
            var apduRes = await connectionObject.TransceiveAsync( cmd );

            DeviceServerApp.Logger.Information( "Got response <" + apduRes.ToString() + ">" );
            if( !apduRes.Succeeded )
            {
                Exception exception = new Exception( "Failure writing MIFARE Ultralight card, " + apduRes.ToString() );
                throw exception;
            }

        } // WriteAsync

        /// <summary>
        /// Wrapper method to perform transparent transceive data to the MifareUL card
        /// </summary>
        /// <param name="commandData">
        /// The command to send to the MifareUL card
        /// </param>
        /// <returns>
        /// byte array of the read data
        /// </returns>
        public async Task<byte[]> TransparentExchangeAsync(byte[] commandData)
        {
            byte[] responseData = await connectionObject.TransparentExchangeAsync(commandData);

            return responseData;
        }
        /// <summary>
        /// Wrapper method get the MifareUL ICC UID
        /// </summary>
        /// <returns>
        /// byte array UID
        /// </returns>
        public async Task<byte[]> GetUidAsync()
        {
            var apduRes = await connectionObject.TransceiveAsync(new MifareUltralight.GetUid());

            if (!apduRes.Succeeded)
            {
                throw new Exception("Failure getting UID of MIFARE Ultralight card, " + apduRes.ToString());
            }

            return apduRes.ResponseData;
        }
    }
}
