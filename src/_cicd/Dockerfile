FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
ARG BUILD_OUTPUT

WORKDIR /app

COPY ${BUILD_OUTPUT}/ .

ENTRYPOINT ["dotnet", "Identity.Business.Users.dll"]