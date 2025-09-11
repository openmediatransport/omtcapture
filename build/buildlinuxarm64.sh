dotnet publish ../omtcapture.sln -r linux-arm64 -c Release
mkdir arm64
cp ../bin/Release/net8.0/linux-arm64/native/omtcapture ./arm64/omtcapture
cp ../../libvmx/build/libvmx.so ./arm64/libvmx.so
cp ../config.xml ./arm64/config.xml