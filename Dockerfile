FROM mcr.microsoft.com/dotnet/sdk:7.0

USER 51515

COPY .artifacts /frogbot

ENTRYPOINT ["dotnet", "/frogbot/FrogBot.dll"]