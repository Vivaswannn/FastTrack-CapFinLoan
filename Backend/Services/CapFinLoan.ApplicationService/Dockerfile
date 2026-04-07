# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Services/CapFinLoan.ApplicationService/CapFinLoan.ApplicationService.csproj", "Services/CapFinLoan.ApplicationService/"]
COPY ["SharedKernel/CapFinLoan.SharedKernel/CapFinLoan.SharedKernel.csproj", "SharedKernel/CapFinLoan.SharedKernel/"]

RUN dotnet restore "Services/CapFinLoan.ApplicationService/CapFinLoan.ApplicationService.csproj"

COPY . .

WORKDIR "/src/Services/CapFinLoan.ApplicationService"
RUN dotnet publish "CapFinLoan.ApplicationService.csproj" \
    -c Release -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p logs

EXPOSE 5002
ENTRYPOINT ["dotnet", "CapFinLoan.ApplicationService.dll"]
