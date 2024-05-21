FROM mcr.microsoft.com/dotnet/sdk:7.0.407-alpine3.19-amd64 # Use the latest version available
ARG servicename
WORKDIR /app
COPY out/$servicename .