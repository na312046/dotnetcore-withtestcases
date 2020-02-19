#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src

COPY ["UnitTestProject/UnitTestProject.csproj", "UnitTestProject/"]
COPY ["cosmos-dotnet-core-todo-app-master/src/todo.csproj", "cosmos-dotnet-core-todo-app-master/src/"]
RUN dotnet restore "UnitTestProject/UnitTestProject.csproj"
RUN dotnet restore "cosmos-dotnet-core-todo-app-master/src/todo.csproj"

COPY . .
WORKDIR "/src/cosmos-dotnet-core-todo-app-master/src"
RUN dotnet build "todo.csproj" -c Release -o /app/build

RUN dotnet test "UnitTestProject/UnitTestProject.csproj" --logger "trx;LogFileName=UnitTestProject.trx" 

FROM build AS publish
RUN dotnet publish "todo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "todo.dll"]

