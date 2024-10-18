# See https://aka.ms/customizecontainer to learn how to customize your debug container
# and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Install CUDA 12.4 and cuDNN 9.3 dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget gnupg2 curl ca-certificates 

USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Install clang/zlib1g-dev dependencies for publishing to native
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev \
    wget gnupg2 curl ca-certificates 

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN echo ${PWD}
COPY ["GenAI.Server.csproj", "."]
RUN dotnet restore "GenAI.Server.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "./GenAI.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GenAI.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true -r linux-x64 --self-contained

FROM nvidia/cuda:12.4.1-cudnn-runtime-ubuntu22.04 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
VOLUME /models
ENTRYPOINT ["./GenAI.Server"]
