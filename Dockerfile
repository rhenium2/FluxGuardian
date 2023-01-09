FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
ENTRYPOINT ["dotnet", "FluxGuardian.dll"]