# Open Media Transport (OMT) Encoder for Raspberry Pi 5

**omtcapture** is an encoder for Raspberry Pi 5 which converts USB 3.0 based video capture devices including Webcams
to an Open Media Transport (OMT) source on the network.

## Requirements

* Raspberry Pi 5 with default OS installed. 2GB memory option is fine.
* USB 3.0 UVC capture device that supports uncompressed video in either UYVY, YUY2 or NV12 formats
(examples include Elgato Cam Link, Magewell USB Capture, Elgato Facecam series etc)
* dotnet 8
* Clang
* libomtnet
* libvmx

## Performance

A base model 2GB Raspberry Pi 5 can comfortably encode at up to 1080p60

## Instructions

1. Install dotnet 8 on to device.

Instructions can be found here:
https://learn.microsoft.com/en-us/dotnet/iot/deployment

**Important:** The --channel parameter should be set to 8.0

2. Install Clang

```
sudo apt install clang
```

3. Copy source code for the following repositories into the home directory in a structure similar to the following:

```
~/libvmx
~/libomtnet
~/omtplayer
```

The easiest way to do this is to git clone these repositories to the home directory using the commands below:

```
cd ~/
git clone https://github.com/openmediatransport/libvmx
git clone https://github.com/openmediatransport/libomtnet
git clone https://github.com/openmediatransport/omtplayer
```

4. Build libvmx 

```
cd ~/libvmx/build
chmod 755 buildlinuxarm64.sh
./buildlinuxarm64.sh
```

5. Build libomtnet 

```
cd ~/libomtnet/build
chmod 755 buildall.sh
./buildall.sh
```

6. Build omtcapture

```
cd ~/omtcapture/build
chmod 755 buildlinuxarm64.sh
./buildlinuxarm64.sh
```

7. All files needed will now be in ~/omtcapture/build/arm64

8. Edit the config file in ~/omtcapture/build/arm64/config.xml to match the desired capture format.

```
nano ~/omtcapture/build/arm64/config.xml
```

You may need to check the capture device documentation to confirm the maximum format that supports uncompressed video.
(Many devices may restrict high frame rates to MJPG only for example)

An example that finds the first device on the system and encodes at 1080p59.94 is below.

If the device does not support the selected frame rate, the nearest will be used instead.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<settings>
  <name>Video</name>
  <!--V4L2 Device Path-->
  <devicePath>/dev/video0</devicePath>
  <width>1920</width>
  <height>1080</height>
  <frameRateN>60000</frameRateN>
  <frameRateD>1001</frameRateD>
  <!--Supported Formats: UYVY, YUY2, NV12-->
  <codec>UYVY</codec>
</settings>

```

9. Run ~/omtcapture/build/arm64/omtcapture to start the encoding. The source should now be on the network.

```
~/omtcapture/build/arm64/omtcapture
```

## Install as a service (optional)

This configures the app to run automatically when the device starts up.

1. Copy the omtcapture files from ~/omtcapture/build/arm64 into a folder called /opt/omtcapture on the system.

```
sudo mkdir /opt/omtcapture
sudo cp ~/omtcapture/build/arm64/* /opt/omtcapture/
```

2. Copy the omtcapture.service template into the /etc/systemd/system/ folder.

```
sudo cp ~/omtcapture/omtcapture.service /etc/systemd/system/
```

3. Reload systemctl and enable the service

```
sudo systemctl daemon-reload
sudo systemctl enable omtcapture
```

4. Start the service and check its status

```
sudo systemctl start omtcapture
sudo systemctl status omtcapture
```

If successful, the output should show a log entry every 60 frames to confirm encoding is in process.

