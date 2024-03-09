#!/bin/bash

# Kill current running version
echo "Killing Kosuru Bot"
killall -9 Kosuru

# install .NET sdk sudo apt-get install -y dotnet-sdk-<version>

# Get latest changes from master
echo "Pulling Latest Changes"
git pull origin master

# Delete unused files
echo "Deleting Unused Files"
rm KosuruIcon.jpg
rm BotDesc.md
rm README.md
rm .gitignore
rm Commands.txt
rm CurrentServers.txt
rm -r .vscode
rm -r .github

# Build new latest changes 
echo "Build Latest Release"
dotnet build Kosuru.sln -c Release
