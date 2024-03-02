#!/bin/bash

# Kill current running version
killall Kosuru -v

# Ensure apps are updated
sudo apt-get update && sudo apt-get upgrade

# install .NET sdk sudo apt-get install -y dotnet-sdk-<version>
# Go to correct directory
cd ~/Kosuru # /root/Kosuru/..

# Get latest changes from master
git pull origin master

# Delete unused files
rm KosuruIcon.jpg
rm BotDesc.md
rm README.md
rm release.ps1
rm .gitignore
rm -r .vscode
rm -r .github

# Get current ubuntu version
UBUNTU_VERSION=$(lsb_release -rs | head -2)

# Build new latest changes 
dotnet build Kosuru.sln --configuration Release --runtime ubuntu.$UBUNTU_VERSION-x64

# Delete obj folder after build
rm -r obj

# Start discord bot
dotnet run Kosuru.sln