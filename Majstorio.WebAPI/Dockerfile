ARG DOTNET_SDK_IMAGE
ARG DOTNET_RUNTIME_IMAGE

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src
COPY . .
RUN dotnet publish Majstorio.WebAPI/Majstorio.WebAPI.csproj --configuration Release --output /app/publish --no-self-contained /p:UseAppHost=false

FROM ${DOTNET_RUNTIME_IMAGE} AS final
WORKDIR /app
COPY --from=build /app/publish .
# Profile photos are written here; the folder belongs to the user the API runs as.
RUN mkdir -p /app/uploads/profile-images && chown -R $APP_UID /app/uploads
USER $APP_UID
ENTRYPOINT ["dotnet", "Majstorio.WebAPI.dll"]
