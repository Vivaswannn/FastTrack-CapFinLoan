# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ApiGateway/CapFinLoan.Gateway/CapFinLoan.Gateway.csproj", "ApiGateway/CapFinLoan.Gateway/"]
COPY ["SharedKernel/CapFinLoan.SharedKernel/CapFinLoan.SharedKernel.csproj", "SharedKernel/CapFinLoan.SharedKernel/"]

RUN dotnet restore "ApiGateway/CapFinLoan.Gateway/CapFinLoan.Gateway.csproj"

COPY . .

WORKDIR "/src/ApiGateway/CapFinLoan.Gateway"
RUN dotnet publish "CapFinLoan.Gateway.csproj" \
    -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos '' appuser
USER appuser

# Copy published output from build stage
# ocelot.json must be in output
COPY --from=build /app/publish .

RUN mkdir -p logs

EXPOSE 5000
ENTRYPOINT ["dotnet", "CapFinLoan.Gateway.dll"]
