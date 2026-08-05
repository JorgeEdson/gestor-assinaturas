FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacao
WORKDIR /origem

COPY GestorAssinaturas.Dominio/GestorAssinaturas.Dominio.csproj GestorAssinaturas.Dominio/
COPY GestorAssinaturas.Aplicacao/GestorAssinaturas.Aplicacao.csproj GestorAssinaturas.Aplicacao/
COPY GestorAssinaturas.Infraestrutura/GestorAssinaturas.Infraestrutura.csproj GestorAssinaturas.Infraestrutura/
COPY GestorAssinaturas.Api/GestorAssinaturas.Api.csproj GestorAssinaturas.Api/

RUN dotnet restore GestorAssinaturas.Api/GestorAssinaturas.Api.csproj

COPY GestorAssinaturas.Dominio/ GestorAssinaturas.Dominio/
COPY GestorAssinaturas.Aplicacao/ GestorAssinaturas.Aplicacao/
COPY GestorAssinaturas.Infraestrutura/ GestorAssinaturas.Infraestrutura/
COPY GestorAssinaturas.Api/ GestorAssinaturas.Api/

RUN dotnet publish GestorAssinaturas.Api/GestorAssinaturas.Api.csproj -c Release -o /aplicacao/publicacao --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS execucao
WORKDIR /aplicacao

COPY --from=compilacao /aplicacao/publicacao .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GestorAssinaturas.Api.dll"]
