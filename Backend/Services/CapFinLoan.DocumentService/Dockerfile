# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Services/CapFinLoan.DocumentService/CapFinLoan.DocumentService.csproj", "Services/CapFinLoan.DocumentService/"]
COPY ["SharedKernel/CapFinLoan.SharedKernel/CapFinLoan.SharedKernel.csproj", "SharedKernel/CapFinLoan.SharedKernel/"]

RUN dotnet restore "Services/CapFinLoan.DocumentService/CapFinLoan.DocumentService.csproj"

COPY . .

WORKDIR "/src/Services/CapFinLoan.DocumentService"
RUN dotnet publish "CapFinLoan.DocumentService.csproj" \
    -c Release -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Create logs and uploads directories
RUN mkdir -p logs wwwroot/uploads

EXPOSE 5003
ENTRYPOINT ["dotnet", "CapFinLoan.DocumentService.dll"]
