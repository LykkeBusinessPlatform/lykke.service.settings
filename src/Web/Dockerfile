FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
ENTRYPOINT ["dotnet", "Web.dll"]
ARG source=.
WORKDIR /app
COPY $source .
