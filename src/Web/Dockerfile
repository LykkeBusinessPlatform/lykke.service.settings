FROM mcr.microsoft.com/dotnet/core/aspnet:2.2
ENTRYPOINT ["dotnet", "Web.dll"]
ARG source=.
WORKDIR /app
COPY $source .
