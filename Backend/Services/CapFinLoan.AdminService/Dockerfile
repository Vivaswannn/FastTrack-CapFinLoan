# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Services/CapFinLoan.AdminService/CapFinLoan.AdminService.csproj", "Services/CapFinLoan.AdminService/"]
COPY ["SharedKernel/CapFinLoan.SharedKernel/CapFinLoan.SharedKernel.csproj", "SharedKernel/CapFinLoan.SharedKernel/"]

RUN dotnet restore "Services/CapFinLoan.AdminService/CapFinLoan.AdminService.csproj"

COPY . .

WORKDIR "/src/Services/CapFinLoan.AdminService"
RUN dotnet publish "CapFinLoan.AdminService.csproj" \
    -c Release -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=build /app/publish .

RUN mkdir -p logs

EXPOSE 5004
ENTRYPOINT ["dotnet", "CapFinLoan.AdminService.dll"]
