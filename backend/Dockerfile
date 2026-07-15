FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Ortakare.Api/Ortakare.Api.csproj", "src/Ortakare.Api/"]
RUN dotnet restore "src/Ortakare.Api/Ortakare.Api.csproj"

COPY . .
WORKDIR /src/src/Ortakare.Api
RUN dotnet publish "Ortakare.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --system ortakare && adduser --system --ingroup ortakare ortakare
COPY --from=build --chown=ortakare:ortakare /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080
USER ortakare
ENTRYPOINT ["dotnet", "Ortakare.Api.dll"]
