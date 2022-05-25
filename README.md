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
This is the current way that modern Unity, UE and HoloPlayCore apps get access to calibration data. The [NNG](https://nng.nanomsg.org)-based [CBOR](https://cbor.io)-encoded inter-process endpoint is available at `ipc:///tmp/holoplay-driver.ipc`. Besides the lens calibration data this endpoint also provides window location, which is why modern apps like the [HoloPlay Studio](https://learn.lookingglassfactory.com/onboarding/6) don't ask you to manually choose a display.

#### 3) Standard websocket ✅
This enpoint exists to enable [Holoplay.js/Three.js](https://docs.lookingglassfactory.com/developer-tools/three) integration in web browsers. I was debating whether to include this in VirtualHPS as web apps can be ran on any platform and therefore there is no obvious benefit to running them in a VM with no official HoloPlay Service support. Eventually I included it because it didn't take much effort to implement and might be useful to somebody. This API endpoint runs at the address `ws://127.0.0.1:11222/`.

#### 3) NNG websocket ☑️
I wasn't able to gather much about the inner workings or the purpose behind this special websocket, besided the fact that it runs ad the address on `ws://127.0.0.1:11222/driver` and uses the `rep.sp.nanomsg.org` subprotocol. Currently not implemented in VirtualHPS.


## Setup
### Requirements
- VM hypervisor capable of using multiple monitors. Tried and tested using [Parallels Desktop](https://www.parallels.com).
- For [non-Portait screens](https://lookingglassfactory.com/product/overview) you need to install [HoloPlay Service](https://lookingglassfactory.com/software) in order to be able to pull the calibration data on first use.

### Video guide
<a href="https://www.youtube.com/watch?v=ql9mcvMc3l8"><img src="https://github.com/jankais3r/VirtualHPS/raw/main/img/youtube.png" width="600"></a>



### Disclaimer
This work is not affiliated with Looking Glass Factory, Inc.
HoloPlay, Looking Glass, Portait and other potential trademarks belong to their respective owners.
