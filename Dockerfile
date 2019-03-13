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
ARG CERT_PATH_ARG='./out/secret.pfx'
ENV CERTPATH $CERT_PATH_ARG

ARG CERT_SECRET='SUPERSECRET!'
ENV CERTSECRET $CERT_SECRET

RUN dotnet dev-certs https -ep $CERTPATH -p $CERTSECRET

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime

# Get/Set Enviornment variables based on provided Arguments
# Arguments can be overriden at commandline or docker-compose

ARG CERT_PATH_ARG='./secret.pfx'
ENV CERTPATH $CERT_PATH_ARG

ARG CERT_SECRET='SUPERSECRET!'
ENV CERTSECRET $CERT_SECRET

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "httpserver.dll"]