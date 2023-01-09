# FluxGuardian
a tool to monitor the health of Flux (https://runonflux.io) nodes

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