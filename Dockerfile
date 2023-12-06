FROM mcr.microsoft.com/dotnet/runtime:8.0

USER 51515

COPY .artifacts /frogbot

ENTRYPOINT ["dotnet", "/frogbot/FrogBot.dll"]