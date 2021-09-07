FROM mcr.microsoft.com/dotnet/sdk:5.0

COPY .artifacts /frogbot

ENTRYPOINT ["dotnet", "exec", "/frogbot/FrogBot.dll"]