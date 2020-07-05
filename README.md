# DeviceServer
A WPF app with an integrated HTTP server that enables the web browser to access local smartcard devices that would otherwise not be accessible via JavaScript.

Table of Contents
=================

  * [Motivation](#Motivation)
    * [What is a thing?](#What-is-a-thing)
    * [Thing ID and serial number](#Thing-ID-and-serial-number)
  * [HTTP client examples](#HTTP-client-examples)
    * [Reading thing data from NFC tag](#Reading-thing-data-from-NFC-tag)
    * [Writing thing data to NFC tag](#Writing-thing-data-to-NFC-tag )
  * [Softwarestack](#Softwarestack)
    * [Why not building an UWP app?](#Why-not-building-an-UWP-app)
    * [Status](#Status)
  * [Platform and Tools](#Platform-and-Tools)
  * [How to run?](#How-to-run)
    * [Building the app](#Building-the-app)
    * [Emulation mode](#Emulation-mode)
  * [Hardware](#Hardware)
    * [Smardcard readers](#Smardcard-readers)
    * [NFC tags](#NFC-tags)
    
## Motivation
The client part of a Web application often needs to access local devices. One example is access to NFC hardware. Specifications like [Web NFC](https://w3c.github.io/web-nfc/) for direct support by the browser are not yet generally available, so I came up with the idea of a local program that allows access to local resources via an HTTP interface.

### What is a thing?
A thing is a physical asset modeled by a [digital twin](https://en.wikipedia.org/wiki/Digital_twin). In the DeviceServer a thing is identified by a _type_ and a unique _ID_. A [GUID](https://de.wikipedia.org/wiki/Globally_Unique_Identifier) is used as ID, see class definition [Thing.cs](./DeviceServer/Thing.cs). After the production of a thing, the type and ID are written to an NFC tag by the DeviceServer and can be read again afterwards. The ID then serves as a pointer to a complex data set, which is stored e.g. in the digital twin in the cloud or in a storage built on [distributed ledger technology](https://en.wikipedia.org/wiki/Distributed_ledger). 

### Thing ID and serial number
The thing ID is not related to the [serial number](https://en.wikipedia.org/wiki/Serial_number) of a physical asset. Unlike the ID, serial numbers are not necessarily unique; different manufacturers of products may use the same serial numbers. They are therefore stored as a normal attribute in the digital twin. 

## HTTP client examples
Following are a few simple examples that show how to use the HTTP server from within the [PowerShell](https://docs.microsoft.com/en-us/powershell/).

### Reading thing data from NFC tag
<pre>
PS C:\> $response = <a href="https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/invoke-webrequest">Invoke-WebRequest</a> -Uri http://SANTACLARA.muc.smarttrust.de:9090<b>/readthing</b> -UseBasicParsing
PS C:\> $response.StatusCode
200

PS C:\> $response.Content | <a href="https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/convertfrom-json">ConvertFrom-Json</a> | <a href="https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/convertto-json">ConvertTo-Json</a>
{
    "Type":  9000,
    "TypeAsString":  "Digger",
    "Id":  "fbc0ceff-ed5b-4e7e-8160-2862dfe5bf57",
    "CreatedWhen":  "0001-01-01T00:00:00"
}
</pre>

### Writing thing data to NFC tag 
<pre>
PS C:\> $thing = @{ Type = "80"; 
                    Id = [System.Guid]::NewGuid().toString() }

PS C:\> $json = $thing | <a href="https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/convertto-json">ConvertTo-Json</a> 
PS C:\> $json
{
    "Id":  "7b3b9920-225f-4f4e-b1e3-62b08413fdd7",
    "Type":  "80"
}

PS C:\> $response = <a href="https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/invoke-webrequest">Invoke-WebRequest</a> -Uri http://SANTACLARA.muc.smarttrust.de:9090<b>/writething</b> -Method POST -Body $json
PS C:\> $response.StatusCode
204
</pre>
See definition of `ThingType` in [Thing.cs](./DeviceServer/Thing.cs) for known thing types.

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

### Status
The Software is **WIP**, development started on June 11, 2020. 

What already runs _(Saturday, 20/07/04 - 13:20 CEST)_:
* Identification of Smartcard readers
* Identification of NFC tags
* Writing Thing data to / Reading Thing data from [MIFARE Ultralight](https://www.nxp.com/docs/en/data-sheet/MF0ICU1.pdf) NFC tag 
* Services of the HTTP Server:<br/>`GET /reader`, `GET /nfctag`, `GET /readthing` and `POST /writething`
* Simple file based emulation mode
* Setting [CORS](https://en.wikipedia.org/wiki/Cross-origin_resource_sharing) response headers

Because development takes place in my free time (and I love my family), progress is slow.

## Platform and Tools
* [Microsoft Windows 10](https://docs.microsoft.com/en-us/windows/release-information/) - the App has been tested with [OS Build 17763.1217](https://support.microsoft.com/en-us/help/4551853/windows-10-update-kb4551853), any newer version of the operating system should do the job. 
* [Microsoft Visual Studio Community 2019](https://visualstudio.microsoft.com/vs/community/)

## How to run?
### Building the app
Clone the repository, open [DeviceServer.sln](./DeviceServer.sln) in VS16 and build the solution.

![DeviceServer UI](./DeviceServer/media/200620%20DeviceServer%20UI.png)

Click on _Start Browser_ and your standard browser navigates to [index.html](./DeviceServer/media/index.html).

### Emulation mode
If a file `Thing.json` exist in the app's root directory, thing data will be read from / written to it. This simple _emulation mode_ facilitates integration tests without NFC hardware, it can be activated on the app's UI. 

![DeviceServer UI, Emulation](./DeviceServer/media/200620%20DeviceServer%20UI,%20Emulation.png)

The following is a file `Thing.json` resulting from a `POST /writething` in emulation mode:
<pre>
PS Microsoft.PowerShell.Core\FileSystem::\\sandboxes.muc.smarttrust.de\Sandboxes\markus\Git-Repositories\DeviceServer\DeviceServer\bin\Debug\netcoreapp3.1> dir

    Verzeichnis: \\sandboxes.muc.smarttrust.de\Sandboxes\markus\Git-Repositories\DeviceServer\DeviceServer\bin\Debug\netcoreapp3.1

Mode                LastWriteTime         Length Name                                                                                                                              
----                -------------         ------ ----                                                                                                                              
d-----       20.06.2020     11:27                logs                                                                                                                              
d-----       12.06.2020     16:21                media                                                                                                                             
-a----       15.06.2020     12:26           4463 DeviceServer.deps.json                                                                                                            
-a----       20.06.2020     11:27         116736 DeviceServer.dll                                                                                                                  
-a----       20.06.2020     11:27         174592 DeviceServer.exe                                                                                                                  
-a----       20.06.2020     11:27          35480 DeviceServer.pdb                                                                                                                  
-a----       15.06.2020     12:26            236 DeviceServer.runtimeconfig.dev.json                                                                                               
-a----       15.06.2020     12:26            161 DeviceServer.runtimeconfig.json                                                                                                   
-a----       09.11.2019     00:56         693680 Newtonsoft.Json.dll                                                                                                               
-a----       26.05.2020     00:17         127488 Serilog.dll                                                                                                                       
-a----       15.05.2020     21:31          29696 Serilog.Sinks.File.dll                                                                                                            
-a----       20.06.2020     12:13            143 <b>Thing.json</b>                                                                                                                        
-a----       20.06.2020     12:11              2 Thing.json.backup                                                                                                                 

PS Microsoft.PowerShell.Core\FileSystem::\\sandboxes.muc.smarttrust.de\Sandboxes\markus\Git-Repositories\DeviceServer\DeviceServer\bin\Debug\netcoreapp3.1> <a href="https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.management/get-content">Get-Content</a> -Path .\<b>Thing.json</b>
{
  "<b>Type</b>": 3,
  "TypeAsString": "ExhaustSystem",
  "<b>Id</b>": "00e5acbe-50d3-4563-ac93-94c6af6da61b",
  "CreatedWhen": "0001-01-01T00:00:00"
}
</pre>
## Hardware
The app was tested with the following hardware:
### Smardcard readers
* [ACR122U](https://www.acs.com.hk/en/products/3/acr122u-usb-nfc-reader/)
* [ACR1255U-J1](https://www.acs.com.hk/en/products/403/acr1255u-j1-acs-secure-bluetooth%C2%AE-nfc-reader/) Bluetooth®
* [HID® OMNIKEY® 5427 CK](https://www.hidglobal.com/products/readers/omnikey/5427)

### NFC tags
* Tags with [MIFARE Ultralight](https://www.nxp.com/docs/en/data-sheet/MF0ICU1.pdf) chip
* [MIDAS NFC Wet Inlay](https://www.smartrac-group.com/midas-nfc.html) with [NTAG® 213](https://www.nxp.com/products/rfid-nfc/nfc-hf/ntag/ntag-for-tags-labels/ntag-213-215-216-nfc-forum-type-2-tag-compliant-ic-with-144-504-888-bytes-user-memory:NTAG213_215_216) chip
