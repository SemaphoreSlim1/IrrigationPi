# Irrigation Pi
[![Build Status](https://dev.azure.com/matthewethomas/Public%20Projects/_apis/build/status/matthewethomas.IrrigationPi?branchName=master)](https://dev.azure.com/matthewethomas/Public%20Projects/_build/latest?definitionId=16&branchName=master)
![Code Coverage](https://img.shields.io/azure-devops/coverage/matthewethomas/Public%20Projects/16)
![License](https://img.shields.io/github/license/matthewethomas/IrrigationPi)

A .Net Core 3.1 irrigation controller for the RaspberryPi running in Docker


# Running on Raspberry Pi
If you already have docker and docker compose installed and configured on your Pi, then running is a one-liner:
```sh
docker run -d -p 80:80 --priviliged --restart unless-stopped matthewthomas/irrigationcontroller:latest
```
Once running, point your browser to http://raspberrypi.local to access the irrigation controller interface.

# Installing Docker and Docker Compose on the Raspberry Pi
If you don't have docker installed on your pi, execute the following script:
```sh
sudo apt update
sudo apt upgrade

curl -sSL https://get.docker.com | sh
sudo usermod -aG docker pi

sudo apt-get install -y libffi-dev libssl-dev
sudo apt-get install -y python3 python3-pip
sudo pip3 -v install docker-compose

logout
```
Credit to [Raspberry Pi Blog](https://www.raspberrypi.org/blog/docker-comes-to-raspberry-pi/) and [Rohan Sawant](https://dev.to/rohansawant/installing-docker-and-docker-compose-on-the-raspberry-pi-in-5-simple-steps-3mgl)


# A little background
Up until very recently, interacting with the GPIO on the Raspberry Pi with .Net meant 1 of 3 things:
1. Installing Windows 10 IOT on your Pi, and developing a UWP app and using [Windows.Devices.Gpio](https://docs.microsoft.com/en-us/uwp/api/windows.devices.gpio) :nauseated_face:
2. Interacting with the GPIO by compiling source to the WiringPi library and then creating a managed wrapper
 (See one of my early attempts [here](https://github.com/matthewethomas/RaspberryPi-NetCore-Blink/tree/master/BlinkGpioWiringPi/)) 
3. Interacting with the GPIO via the file system, and creating a wrapper to interact with it. (See one of my attempts [here](https://github.com/matthewethomas/RaspberryPi-NetCore-Blink/tree/master/BlinkGpioFS))

.Net Core 3.0 introduced cross-platoform [IOT support](https://github.com/dotnet/iot), giving us [System.Devices.Gpio](https://www.nuget.org/packages/System.Device.Gpio) and [Iot.Device.Bindings](https://www.nuget.org/packages/Iot.Device.Bindings). Now, we can develop in .Net Core on our Mac, build and deploy in Azure DevOps, use [Raspbian](https://www.raspberrypi.org/downloads/raspbian/) as our operating system, and everything \*should\* be good to go. :smile: