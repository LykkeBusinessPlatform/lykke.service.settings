FROM microsoft/dotnet:latest

COPY src/Web/App /app

WORKDIR /app

EXPOSE 443/tcp

ENTRYPOINT ["dotnet", "Web.dll"]