FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Get/Set Enviornment variables based on provided Arguments
# Arguments can be overriden at commandline or docker-compose
ENV FACTION_PFX_FILE="cert.pfx"
ENV FACTION_PFX_PASS="5MWRBeFxad2CFdy3VX"

# Generate a Dev cert if a .pfx is not already provided, will not generate if file already exists
RUN dotnet dev-certs https -ep ./out/$FACTION_PFX_FILE -p $FACTION_PFX_PASS -v

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
# Get/Set Enviornment variables based on provided Arguments
# Arguments can be overriden at commandline or docker-compose

ENV FACTION_PFX_FILE="cert.pfx"
ENV FACTION_PFX_PASS="5MWRBeFxad2CFdy3VX"


WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "httpserver.dll"]