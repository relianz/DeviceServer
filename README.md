# DeviceServer
A WPF app with an integrated HTTP server that enables the web browser to access local smartcard devices that would otherwise not be accessible via JavaScript.

## Status
The Software is **WIP**, development started on June 11, 2020. 

What already runs _(Tuesday, 20/06/16 - 21:51 CEST)_:
* Identification of Smartcard readers
* Identification of NFC tags
* Writing Thing data to / Reading Thing data from [MIFARE Ultralight](https://www.nxp.com/docs/en/data-sheet/MF0ICU1.pdf) NFC tag 
* Services of the HTTP Server:<br/>`GET /reader`, `GET /nfctag`, and `GET /readthing`

![DeviceServer /readthing](./DeviceServer/media/200616%20DeviceServer,%20readthing.jpg)

Because development takes place in my free time (and I love my family), progress is slow.

## Softwarestack
DeviceServer is written in C# [Version 8.0](https://stackoverflow.com/questions/247621/what-are-the-correct-version-numbers-for-c). It uses the following technologies and software modules:
* [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
* [HttpListener](https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistener?view=netcore-3.1)
* [Windows Presentation Foundation](https://docs.microsoft.com/en-us/dotnet/desktop-wpf/overview/?view=vs-2019) with [Data binding](https://docs.microsoft.com/en-us/dotnet/desktop-wpf/data/data-binding-overview)
* [Task Parallel Library](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl)
* [Microsoft.Windows.SDK.Contracts](https://docs.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-enhance)
* [Serilog](https://github.com/serilog/serilog)
* [Json.NET](https://www.newtonsoft.com/json)

You do not have to obtain any packages manually, the project file [DeviceServer.csproj](./DeviceServer/DeviceServer.csproj) does this automatically. 

### Why not building an UWP app?
When a [UWP](https://docs.microsoft.com/en-us/windows/uwp/) app provides a network service, only _its own code_ or an app _on another machine_ can access the service. Since the app and the web browser should run on the same machine, UWP is not an alternative.

## Platform & Tools
* [Microsoft Windows 10](https://docs.microsoft.com/en-us/windows/release-information/) - the App has been tested with [OS Build 17763.1217](https://support.microsoft.com/en-us/help/4551853/windows-10-update-kb4551853), any newer version of the operating system should do the job. 
* [Microsoft Visual Studio Community 2019](https://visualstudio.microsoft.com/vs/community/)

### How to build?
Clone the repository, open [DeviceServer.sln](./DeviceServer.sln) in VS16 and build the solution.

![DeviceServer UI](./DeviceServer/media/200615%20DeviceServer%20UI.jpg)

## Hardware
The app was tested with the following hardware:
### Smardcard readers
* [ACR122U](https://www.acs.com.hk/en/products/3/acr122u-usb-nfc-reader/)
* [HID® OMNIKEY® 5427 CK](https://www.hidglobal.com/products/readers/omnikey/5427)

### NFC tags
* [MIFARE Ultralight](https://www.nxp.com/docs/en/data-sheet/MF0ICU1.pdf)
* [MIDAS NFC Wet Inlay](https://www.smartrac-group.com/midas-nfc.html)
