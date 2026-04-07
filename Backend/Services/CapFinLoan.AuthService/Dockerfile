# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first (layer caching optimization)
COPY ["Services/CapFinLoan.AuthService/CapFinLoan.AuthService.csproj", "Services/CapFinLoan.AuthService/"]
COPY ["SharedKernel/CapFinLoan.SharedKernel/CapFinLoan.SharedKernel.csproj", "SharedKernel/CapFinLoan.SharedKernel/"]

# Restore dependencies
RUN dotnet restore "Services/CapFinLoan.AuthService/CapFinLoan.AuthService.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/Services/CapFinLoan.AuthService"
RUN dotnet publish "CapFinLoan.AuthService.csproj" \
    -c Release -o /app/publish \
    --no-restore

# Stage 2: Runtime (much smaller image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p logs

EXPOSE 5001
ENTRYPOINT ["dotnet", "CapFinLoan.AuthService.dll"]
