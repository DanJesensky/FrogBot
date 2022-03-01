FROM mcr.microsoft.com/dotnet/sdk:6.0

COPY .artifacts /frogbot

ENTRYPOINT ["dotnet", "/frogbot/FrogBot.dll"]