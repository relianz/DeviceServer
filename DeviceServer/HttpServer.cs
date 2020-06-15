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

using System.IO;                        // StreamReader
using System.Net;                       // HttpListener, HttpListenerContext
using System.Text;                      // Encoding
using System.Threading.Tasks;           // Task
using System.Collections.Generic;       // IDictionary

using Newtonsoft.Json;                  // JsonConvert

namespace Relianz.DeviceServer
{
    public class HttpServer
    {
        #region public
        public HttpServer( string ipAddr, int port, string rootDir )
        {
            if( !HttpListener.IsSupported )
            {
                string err = "Windows XP SP2 or Server 2003 is required to use the HttpListener class.";

                DeviceServerApp.Logger.Error( err );
                throw new Exception( err );
            }

            ServerUri = $"http://{ipAddr}:{port}/";
            listener = new HttpListener();
            listener.Prefixes.Add( ServerUri );

            myServerSettings = new ServerSettings( rootDir );

        } // ctor

        public void Start()
        {
            keepGoing = true;

            if( listenerTask != null && !listenerTask.IsCompleted )
            {
                DeviceServerApp.Logger.Warning( "Server already started." );
                return;
            }

            try
            {
                listener.Start();
            }
            catch( HttpListenerException ex )
            {
                DeviceServerApp.Logger.Error( "Cannot start HttpListener - " + ex.Message );
                return;
            }

            listenerTask = new Task( new Action( ServerLoop ) );
            listenerTask.Start();

            DeviceServerApp.Logger.Information( "Server started." );

        } // Start

        public void Stop()
        {
            keepGoing = false;

            lock( listener )
            {
                // Do not terminate a request that's currently being processed:
                listener.Stop();
            }
            try
            {
                listenerTask.Wait();
            }
            catch( Exception x )
            {
                DeviceServerApp.Logger.Fatal( x.Message );
            }

            DeviceServerApp.Logger.Information( "Server stopped." );

        } // Stop

        public void DisplayPrefixesAndState()
        {
            // List the prefixes to which the server listens:
            HttpListenerPrefixCollection prefixes = listener.Prefixes;
            if( prefixes.Count == 0 )
            {
                DeviceServerApp.Logger.Error( "There are no HttpListener prefixes." );
                return;
            }

            foreach( string prefix in prefixes )
            {
                DeviceServerApp.Logger.Information( prefix );
            }

            // Show the listening state:
            if( listener.IsListening )
            {
                DeviceServerApp.Logger.Information( "The HttpListener is listening." );
            }
            else
            {
                DeviceServerApp.Logger.Warning( "The HttpListener is not listening." );
            }


        } // DisplayPrefixesAndState

        public ServerSettings myServerSettings;
        public string ServerUri { get => serverUri; private set => serverUri = value; }

        #endregion

        #region private
        private HttpListener listener = null;
        private Task listenerTask = null;
        private string serverUri = null;
        private bool keepGoing = false;

        private async void ServerLoop()
        {
            DisplayPrefixesAndState();

            while( keepGoing )
            {
                try
                {
                    DeviceServerApp.Logger.Information( "Waiting for HTTP request." );
                    HttpListenerContext context = await listener.GetContextAsync();
                   
                    // Start new thread to handle HTTP request:
                    _ = Task.Factory.StartNew( () => ProcessRequest( context, myServerSettings ) );

                }
                catch( Exception e )
                {
                    DeviceServerApp.Logger.Error( e.Message );

                    if( e is HttpListenerException )
                        return; // this gets thrown when the listener is stopped

                } // Exception

            } // keepGoing

        } // ServerLoop
        private void ProcessRequest( HttpListenerContext context, ServerSettings settings )
        {
            DeviceServerApp.Logger.Information( "Processing request from <" + context.Request.Url.ToString() + ">" );

            using( var response = context.Response )
            {
                try
                {
                    var handled = false;

                    switch( context.Request.Url.AbsolutePath )
                    {
                        // This is where we do different things depending on the URL                        
                        case "/settings":
                        {
                            switch( context.Request.HttpMethod )
                            {
                                case "GET":
                                {
                                    // Get the current application settings:
                                    response.ContentType = "application/json";

                                    // This is what we want to send back:
                                    var responseBody = JsonConvert.SerializeObject( settings );

                                    // Write it to the response stream
                                    var buffer = Encoding.UTF8.GetBytes( responseBody );
                                    response.ContentLength64 = buffer.Length;
                                    response.OutputStream.Write( buffer, 0, buffer.Length );
                                    handled = true;

                                    break;

                                } // GET settings

                                case "PUT":
                                {
                                    // Update application settings:
                                    using( var body = context.Request.InputStream )
                                    using( var reader = new StreamReader( body, context.Request.ContentEncoding ) )
                                    {
                                        // Get the data that was sent to us
                                        var json = reader.ReadToEnd();

                                        // Use it to update our settings
                                        UpdateSettings( JsonConvert.DeserializeObject<ServerSettings>( json ) );

                                        // Return 204 No Content to say we did it successfully
                                        response.StatusCode = 204;
                                        handled = true;
                                    }
                                    break;

                                } // PUT settings

                                default:
                                {
                                    DeviceServerApp.Logger.Error( $"Invalid HTTP method {context.Request.HttpMethod} for /settings" );
                                    break;
                                }

                            } // switch method

                            break;

                        } // settings 

                        case "/index.html":
                        {
                            string httpMethod = context.Request.HttpMethod;
                            if( httpMethod.CompareTo( "GET" ) != 0 )
                            {
                                DeviceServerApp.Logger.Error( $"Invalid HTTP method {context.Request.HttpMethod} for /index.html" );
                                break;
                            }

                            DeliverStaticResource( context, "media/index.html" );
                            handled = true;

                            break;
                        }

                        default:
                        {
                            DeviceServerApp.Logger.Error( $"Cannot handle absolute path <{context.Request.Url.AbsolutePath }>" );
                            break;
                        }

                    } // switch absolute path 

                    if( !handled )
                    {
                        response.StatusCode = 404;
                    }
                }
                catch( Exception e )
                {
                    // Return the exception details the client - you may or may not want to do this.
                    response.StatusCode = 500;
                    response.ContentType = "application/json";

                    var buffer = Encoding.UTF8.GetBytes( JsonConvert.SerializeObject( e ) );
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write( buffer, 0, buffer.Length );

                    // TODO: Log the exception

                } // catch

            } // using response

        } // ProcessRequest

        private static void UpdateSettings( ServerSettings newSettings )
        {
            // TODO: Update application settings
        }

        private void DeliverStaticResource( HttpListenerContext context, string fileName )
        {
            string path = Path.Combine( myServerSettings.RootDir, fileName );
            if( !File.Exists( path ) )
            {
                DeviceServerApp.Logger.Fatal( $"file <{path}> does not exist" );
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

                return;

            } // missing file

            try
            {
                Stream input = new FileStream( fileName, FileMode.Open );

                // Adding permanent HTTP response headers:
                string mime;
                context.Response.ContentType = _mimeTypeMappings.TryGetValue( Path.GetExtension( fileName ), out mime ) ? mime : "application/octet-stream";
                context.Response.ContentLength64 = input.Length;
                context.Response.AddHeader( "Date", DateTime.Now.ToString( "r" ) );
                context.Response.AddHeader( "Last-Modified", System.IO.File.GetLastWriteTime( fileName ).ToString( "r" ) );

                byte[] buffer = new byte[ 1024 * 16 ];

                int nbytes;
                while( (nbytes = input.Read( buffer, 0, buffer.Length )) > 0 )
                {
                    context.Response.OutputStream.Write( buffer, 0, nbytes );
                    myServerSettings.BytesServed += nbytes;
                }

                input.Close();

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Flush();
            }
            catch( Exception ex )
            {
                DeviceServerApp.Logger.Fatal( ex.Message );
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

        } // DeliverStaticResource

        private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>( StringComparer.InvariantCultureIgnoreCase ) 
        {
        {".asf", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".cco", "application/x-cocoa"},
        {".crt", "application/x-x509-ca-cert"},
        {".css", "text/css"},
        {".deb", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dll", "application/octet-stream"},
        {".dmg", "application/octet-stream"},
        {".ear", "application/java-archive"},
        {".eot", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".flv", "video/x-flv"},
        {".gif", "image/gif"},
        {".hqx", "application/mac-binhex40"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".iso", "application/octet-stream"},
        {".jar", "application/java-archive"},
        {".jardiff", "application/x-java-archive-diff"},
        {".jng", "image/x-jng"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".mml", "text/mathml"},
        {".mng", "video/x-mng"},
        {".mov", "video/quicktime"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".msi", "application/octet-stream"},
        {".msm", "application/octet-stream"},
        {".msp", "application/octet-stream"},
        {".pdb", "application/x-pilot"},
        {".pdf", "application/pdf"},
        {".pem", "application/x-x509-ca-cert"},
        {".pl", "application/x-perl"},
        {".pm", "application/x-perl"},
        {".png", "image/png"},
        {".prc", "application/x-pilot"},
        {".ra", "audio/x-realaudio"},
        {".rar", "application/x-rar-compressed"},
        {".rpm", "application/x-redhat-package-manager"},
        {".rss", "text/xml"},
        {".run", "application/x-makeself"},
        {".sea", "application/x-sea"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".swf", "application/x-shockwave-flash"},
        {".tcl", "application/x-tcl"},
        {".tk", "application/x-tcl"},
        {".txt", "text/plain"},
        {".war", "application/java-archive"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wmv", "video/x-ms-wmv"},
        {".xml", "text/xml"},
        {".xpi", "application/x-xpinstall"},
        {".zip", "application/zip"},
        };
        #endregion

    } // class HttpServer

    public class ServerSettings
    {
        public ServerSettings( string rootDir )
        {
            this.RootDir = rootDir;

        } // ctor

        public string RootDir { get => rootDir; set => rootDir = value; }
        public string BytesServed { get => bytesServed; set => bytesServed = value; }

        private string rootDir;
        private string bytesServed;

    } // ServerSettings

} // namespace Relianz.DeviceServer
