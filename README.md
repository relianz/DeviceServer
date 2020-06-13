# DeviceServer
A WPF app with an integrated HTTP server that enables the web browser to access local smartcard devices that would otherwise not be accessible via JavaScript.

## Status
The Software is WIP, developnment started on June 11, 2020. 

What already runs is:
* Identification of Smartcard readers
* Identification of NFC tags
* Running the HTTP Server

## Softwarestack
DeviceServer is written in C# [Version 8.0](https://stackoverflow.com/questions/247621/what-are-the-correct-version-numbers-for-c). It uses the following technologies and software modules:
* [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
* [HttpListener](https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistener?view=netcore-3.1)
* [Windows Presentation Foundation](https://docs.microsoft.com/en-us/dotnet/desktop-wpf/overview/?view=vs-2019) 
* [Microsoft.Windows.SDK.Contracts](https://docs.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-enhance)
* [Serilog](https://github.com/serilog/serilog)
* [Json.NET](https://www.newtonsoft.com/json)

You do not have to obtain any packages manually, the project file [DeviceServer.csproj](./DeviceServer/DeviceServer.csproj) does this automatically. 

## Platform & Tools
* Microsoft Windows 10 
* [Microsoft Visual Studio Community 2019](https://visualstudio.microsoft.com/vs/community/)
