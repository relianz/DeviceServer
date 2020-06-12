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

using System;                           // DateTime
using System.ComponentModel;            // INotifyPropertyChanged

namespace Relianz.DeviceServer
{
    public class Product
    {
        #region Public members

        #region Getters/setters
        public int ProductType { get => m_productType; }
        public long ProductID { get => m_productID; }

        public DateTime? DateTimeCreated { get => m_dateTimeCreated; }
        public DateTime? DateTimeDelivered { get => m_dateTimeDelivered; set => m_dateTimeDelivered = value; }
        public DateTime? DateTimeAssembled { get => m_dateTimeAssembled; set => m_dateTimeAssembled = value; }

        public byte[] SupplierAddr { get => m_supplierAddress; set => m_supplierAddress = value; }
        public byte[] CustomerAddr { get => m_customerAddress; set => m_customerAddress = value; }

        public char StringSeparatorChar { get => m_stringSeparatorChar; }

        // Number of bytes for supplier's or manufacturer's address:
        public static int AddressLength => m_addressLength;
        #endregion

        public Product( int productType, long productID, byte[] supplierAddress )
        {
            m_productType = productType;
            m_productID = productID;
            m_supplierAddress = supplierAddress;

        } // ctor

        public Product( string csvString, char separator = ',' )
        {
            m_stringSeparatorChar = separator;
            string[] attributes = csvString.Split( m_stringSeparatorChar );

            try
            {
                m_productID = Int64.Parse( attributes[ 0 ] );
                m_productType = Int32.Parse( attributes[ 1 ] );

                // creation timestamp and supplier address:
                if( !string.IsNullOrEmpty( attributes[ 2 ] ) )
                    m_dateTimeCreated = DateTime.Parse( attributes[ 2 ] );

                if( !string.IsNullOrEmpty( attributes[ 3 ] ) )
                    m_supplierAddress = Helpers.StringToByteArray( attributes[ 3 ] );
                else
                    throw new FormatException( "Missing address of product creator" );

                // delivery timestamp and customer address:
                if( !string.IsNullOrEmpty( attributes[ 4 ] ) )
                {
                    m_dateTimeDelivered = DateTime.Parse( attributes[ 4 ] );

                    if( !string.IsNullOrEmpty( attributes[ 5 ] ) )
                        m_customerAddress = Helpers.StringToByteArray( attributes[ 5 ] );
                    else
                        throw new FormatException( "Missing address of product customer" );

                } // product delivered

                // assembly timestamp:
                if( !string.IsNullOrEmpty( attributes[ 6 ] ) )
                    m_dateTimeAssembled = DateTime.Parse( attributes[ 6 ] );
            }
            catch( Exception ex )
            {
                if( ex is FormatException || ex is OverflowException )
                {
                    ;
                }

                throw;
            }

        } // ctor Product  

        public override string ToString()
        {
            string addr = Helpers.ByteArrayToString( m_supplierAddress );
            string s = $"{m_productID}{m_stringSeparatorChar}{m_productType}{m_stringSeparatorChar}{m_dateTimeCreated}{m_stringSeparatorChar}{addr}";

            // Product delivered?
            if( m_dateTimeDelivered != null )
            {
                addr = Helpers.ByteArrayToString( m_customerAddress );
                s += $"{m_stringSeparatorChar}{m_dateTimeDelivered}{m_stringSeparatorChar}{addr}";
            }
            else
                s += $"{m_stringSeparatorChar}{m_stringSeparatorChar}";

            // Product assembled?
            if( m_dateTimeAssembled != null )
            {
                s += $"{m_stringSeparatorChar}{m_dateTimeAssembled}";
            }
            else
                s += $"{m_stringSeparatorChar}";

            return s;

        } // ToCsvString
        #endregion

        #region private members
        private int m_productType;
        private long m_productID;

        private DateTime? m_dateTimeCreated;
        private DateTime? m_dateTimeDelivered;
        private DateTime? m_dateTimeAssembled;

        private const int m_addressLength = 40;

        private byte[] m_supplierAddress;
        private byte[] m_customerAddress;

        private char m_stringSeparatorChar;
        #endregion

    } // class Product

    public class ProductDescription : INotifyPropertyChanged
    {
        #region Public members

        public ProductDescription( Product product )
        {
            m_product = product;

            switch( m_product.ProductType )
            {
                case 1:
                    m_productName = "Radial ball bearing (S/N 90903-63014)";
                    m_productImage = "Ball_bearing.png";
                    break;

                case 2:
                    m_productName = "Li-ion battery cell (Type 18650)"; // "Brake disc F (S/N SU003-00586)";
                    m_productImage = "Battery.png"; // "Brake_disc.png";
                    break;

                case 3:
                    m_productName = "Gear assembly, front planetary (S/N 3572073020)";
                    m_productImage = "Planetary_gear_train.png";
                    break;

                default:
                    m_productName = "Unknown product type";
                    m_productImage = "ProductInventory";
                    break;

            } // switch( product.ProductType )

            m_productID = $"Unique ID: " + m_product.ProductID;
            m_productCreatedState = "  Created: " + m_product.DateTimeCreated;

            if( m_product.DateTimeDelivered.HasValue )
            {
                string addr = Helpers.ByteArrayToString( m_product.CustomerAddr );
                m_productDeliveredState = $"Delivered: {m_product.DateTimeDelivered} to {addr}";
            }

            if( m_product.DateTimeAssembled.HasValue )
            {
                m_productAssembledState = $"Assembled: {m_product.DateTimeAssembled}";
            }

        } // ProductDescription

        #region Getter/setter
        public Product Product { get => m_product; }

        public string ProductName { get => m_productName; set => m_productName = value; }
        public string ProductID { get => m_productID; set => m_productID = value; }
        public string ProductImage { get => m_productImage; set => m_productImage = value; }
        public string ProductCreatedState { get => m_productCreatedState; set => m_productCreatedState = value; }
        public string ProductDeliveredState { get => m_productDeliveredState; set => m_productDeliveredState = value; }
        public string ProductAssembledState { get => m_productAssembledState; set => m_productAssembledState = value; }
        #endregion

        #endregion

        #region private
        private Product m_product;
        private string m_productName;
        private string m_productID;
        private string m_productCreatedState;
        private string m_productDeliveredState;
        private string m_productAssembledState;
        private string m_productImage;

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion private

    } // ProductDescription

} // namespace Relianz.DeviceServer 
