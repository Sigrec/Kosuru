#!/bin/bash

# Kill current running version
echo "Killing Kosuru Bot"
killall -9 Kosuru

# Ensure apps are updated
echo "Updating Apps"
sudo apt-get update && sudo apt-get upgrade

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
rm -r .vscode
rm -r .github

# Build new latest changes 
echo "Build Latest Release"
dotnet build Kosuru.sln -c Release

# Start discord bot
# echo "Running Kosuru Bot"
# dotnet run Kosuru.sln