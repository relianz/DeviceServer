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

using System.IO;                // Path
using System.Linq;              // Select
using System.Diagnostics;       // StackFrame

using Serilog;                  // LoggerConfiguration
using Serilog.Core;             // ILogEventEnricher
using Serilog.Configuration;    // LoggerMinimumLevelConfiguration, LoggerSinkConfiguration
using Serilog.Events;           // LogEvent

namespace Relianz.DeviceServer
{
    public class FileLogger
    {
        #region public members
        public static ILogger GetLogger( ref string path )
        {
            if( !isConfigured )
            {
                if( path == null )
                {
                    logFilePath = Path.Combine( DeviceServerApp.AppLocalPath, "logs" );
                    path = logFilePath;
                }
                else
                    logFilePath = path;

                ConfigureLogger();
                isConfigured = true;
            }
            else
                Log.Logger.Information( "Logger already configured!" );

            return Log.Logger;

        } // GetLogger

        public static string LogFilePath { get => logFilePath; }
        #endregion

        #region private members
        private static void ConfigureLogger()
        {
            string logFile = Path.Combine( logFilePath, "log.txt" );

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            LoggerMinimumLevelConfiguration loggerMinimumLevelConfiguration = loggerConfiguration.MinimumLevel;

            LoggerConfiguration debugConfiguration = loggerMinimumLevelConfiguration.Debug();
            LoggerSinkConfiguration sinkConfiguration = debugConfiguration.WriteTo;

            LoggerConfiguration fileConfiguration = sinkConfiguration.File( logFile,
                                                                            rollingInterval: RollingInterval.Day,
                                                                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} (at {Caller}){NewLine}{Exception}" );
            fileConfiguration.Enrich.WithCaller();

            Log.Logger = fileConfiguration.CreateLogger();
            Log.Information( "//// Serilog started!" );

        } // ConfigureLogger

        private static bool isConfigured = false;
        private static string logFilePath = "";

        #endregion

    } // class FileLogger

    class CallerEnricher : ILogEventEnricher
    {
        public void Enrich( LogEvent logEvent, ILogEventPropertyFactory propertyFactory )
        {
            var skip = 3;
            while( true )
            {
                var stack = new StackFrame( skip );
                if( !stack.HasMethod() )
                {
                    logEvent.AddPropertyIfAbsent( new LogEventProperty( "Caller", new ScalarValue( "<unknown method>" ) ) );
                    return;
                }

                var method = stack.GetMethod();
                if( method.DeclaringType.Assembly != typeof( Log ).Assembly )
                {
                    var caller = $"{method.DeclaringType.FullName}.{method.Name}({string.Join( ", ", method.GetParameters().Select( pi => pi.ParameterType.FullName ) )})";
                    logEvent.AddPropertyIfAbsent( new LogEventProperty( "Caller", new ScalarValue( caller ) ) );

                    return;
                }

                skip++;
            }

        } // method Enrich

    } // class CallerEnricher

    static class LoggerCallerEnrichmentConfiguration
    {
        public static LoggerConfiguration WithCaller( this LoggerEnrichmentConfiguration enrichmentConfiguration )
        {
            return enrichmentConfiguration.With<CallerEnricher>();

        } // WithCaller

    } // class LoggerCallerEnrichmentConfiguration

} // namespace Relianz.DeviceServer
