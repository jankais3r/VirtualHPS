# VirtualHPS
Looking Glass HoloPlayService replacement for virtual machines without direct access to the hardware.

<p align="center">
  <img src="https://github.com/jankais3r/VirtualHPS/raw/main/img/VirtualHPS.png" width="200">
</p>

### The problem
The official [HoloPlay Service](https://learn.lookingglassfactory.com/onboarding/4) relies on a direct, low-level connection to the screen in order pull its lenticular lens calibration values over USB/HDMI. When we run HoloPlay Service in a virtual machine, it cannot reach the hardware, as the Looking Glass display is presented to the guest OS as a `Generic PnP Monitor` without any special properties.

### The solution
VirtualHPS mirrors the API endpoints of HoloPlay Service, but with the option to use manually supplied calibration data. That means it works also in VMs, and it is currently the only way to play Windows-based holo apps on Apple Silicon Macs.

### HoloPlay API Variants

#### 1) Legacy ✅
This method is used by Unity apps built using the oldest HoloPlay SDK. Each app has means of direct communication with the display, something that was since then offloaded to HoloPlay Service. We cannot simulate the display itself, but luckily there is a built-in fallback (probably for debug reasons) where these apps check for the existence of a file `C:\LKG_calibration\visual.json` and, if found, use that instead. VirtualHPS makes sure that this file always matches the calibration values of the currently used display. You can identify apps using this method by the launch dialog that asks you to manually select the display and resolution.

#### 2) NNG ✅
This is the current way that modern Unity, UE and HoloPlayCore apps get access to calibration data. The [NNG](https://nng.nanomsg.org)-based [CBOR](https://cbor.io)-encoded inter-process endpoint is available at `ipc:///tmp/holoplay-driver.ipc`. Besides the lens calibration data this endpoint also provides render window location, which is why modern apps like the [HoloPlay Studio](https://learn.lookingglassfactory.com/onboarding/6) don't ask you to manually choose a display.

#### 3) Standard websocket ✅
This enpoint exists to enable [Holoplay.js/Three.js](https://docs.lookingglassfactory.com/developer-tools/three) integration in web browsers. I was debating whether to include this in VirtualHPS as web apps can be ran on any platform and therefore there is no obvious benefit to running them in a virtual machine. Eventually I included it because it didn't take much effort to implement and it might be useful to somebody. This API endpoint runs at the address `ws://127.0.0.1:11222/`.

#### 3) NNG websocket ☑️
I wasn't able to gather much about the inner workings or the purpose behind this special websocket, besided the fact that it runs at `ws://127.0.0.1:11222/driver` and uses the `rep.sp.nanomsg.org` websocket subprotocol. Currently not implemented in VirtualHPS.


### Setup
#### Host system setup
- Verify that you have the "Default for Display" scaling option selected for the Looking Glass display in macOS' Display Preferences
- VM hypervisor capable of using multiple monitors. Tried and tested using [Parallels Desktop](https://www.parallels.com).
- In the Parallels VM options enable:
  - Options > Full Screen > Use all displays in full screen
  - Hardware > Graphics > Best for external displays 
- For [non-Portait screens](https://lookingglassfactory.com/product/overview) you need to install [HoloPlay Service](https://lookingglassfactory.com/software) in order to be able to pull the calibration data (only needed for the initial setup).

#### Video guide
<a href="https://www.youtube.com/watch?v=ql9mcvMc3l8"><img src="https://github.com/jankais3r/VirtualHPS/raw/main/img/youtube.png" width="600"></a>

#### Portrait setup
1) Once your Windows VM is booted up, pass through the Portrait's mass storage device. It contains a calibration file from factory that VirtualHPS utilizes. No further setup needed.

#### Non-Portrait setup
1) Non-Portrait LG screens do not have a mass storage, therefore we have to manually supply calibration values on first run of VirtualHPS.
2) To get the calibration values, make sure that HoloPlay Service on your computer can see the display, and then paste the following code into your web browser's JavaScript console:
```javascript
var ws = new WebSocket('ws://localhost:11222/'); ws.onmessage = function(event){alert(event.data)}
```

_Note: Connection to local websocket doesn't work in Safari, so use Firefox, Brave or any other Chromium-based browser._

Alternatively, you can visit [https://eka.hn/calibration_test.html](https://eka.hn/calibration_test.html) and copy the provided JSON straight from the website.

3) On first launch of VirtualHPS enter the copied calibration data.

### Debugging
Launch VirtualHPS.exe from a command line to receive debug messages.

<img src="https://github.com/jankais3r/VirtualHPS/raw/main/img/debug.png" width="600">


### Windows-based holo apps
If you are looking for some cool Windows holo apps, check out the following links:
1) [Made With](https://madewith.lookingglassfactory.com/?filter=apps) website
2) [Library App](https://docs.lookingglassfactory.com/legacy-products/software/library) store
3) [Official Looking Glass Factory](https://discordapp.com/invite/ZW87Y4m) Discord server

### Disclaimer
This work is not affiliated with Looking Glass Factory, Inc.

HoloPlay, Looking Glass, Portait and other potential trademarks belong to their respective owners.
