ARG DOTNET_SDK_IMAGE
ARG DOTNET_RUNTIME_IMAGE

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src
COPY . .
RUN dotnet publish Majstorio.Worker/Majstorio.Worker.csproj --configuration Release --output /app/publish --no-self-contained /p:UseAppHost=false

FROM ${DOTNET_RUNTIME_IMAGE} AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Majstorio.Worker.dll"]
