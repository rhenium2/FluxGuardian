# FluxGuardian 
[![Try it on Telegram](https://img.shields.io/badge/try%20it-on%20Telegram-0088cc.svg)](https://t.me/FluxGuardianBot)
[![Try it on Discord](https://img.shields.io/badge/try%20it-on%20Discord-7289d9.svg)](https://discord.gg/5H2qxcBk)

FluxGuardian is a tool to monitor the health of Web3 Flux nodes. 

Available on Telegram as @FluxGuardianBot (https://t.me/FluxGuardianBot)

Available on Discord as @FluxGuardian (https://discord.gg/5H2qxcBk)

It regularly checks the health of Flux nodes and notifies you back in case they are not up and confirmed. 
Send a message to it on Telegram and use ‘/start’ and then use ‘/addnode’ command.
at anytime if you are stuck, just type ‘/start’ and you can start all over fresh.

## Get Started
1. Build docker image
```shell
    docker build . -t flux-guardian
```

2. Modify *fluxconfig.json* file

3. Run docker container
```shell
  docker run -d --rm --volume $(pwd):/app --name fg flux-guardian 
```

## Telegram commands
* start - starts the conversation with the bot
* addnode - adds a new Flux node
* status - shows the last status of all your Flux nodes
* mynodes - shows all nodes added to the bot
* removeallnodes - removes all nodes added to the bot
