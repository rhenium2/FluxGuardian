# FluxGuardian
A tool to monitor the health of Flux (https://runonflux.io) nodes.

If is available on Telegram at @FluxGuardianBot (https://t.me/FluxGuardianBot). 
It regularly checks the health of Flux nodes and notifies you back in case they are not up and confirmed. 
Send a message to it on Telegram and use ‘/start’ and then use ‘/addnode’ command.
If at anytime if you are stuck, just type ‘/start’ and you can start all over fresh.

## Get Started
1. Build docker image
```shell
    docker build . -t flux-guardian
```

2. Modify *fluxconfig.json* file

3. Run docker container
```shell
  docker run -d --rm --volume $(pwd):/app flux-guardian 
```

## Telegram commands
* start - starts the conversation with the bot
* addnode - adds a new Flux node
* status - shows the last status of all Flux nodes
* mynodes - shows all nodes added to the bot
* removeallnodes - removes all nodes added to the bot