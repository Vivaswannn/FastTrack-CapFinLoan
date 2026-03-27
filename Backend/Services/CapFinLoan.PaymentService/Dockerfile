# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Services/CapFinLoan.PaymentService/CapFinLoan.PaymentService.csproj", "Services/CapFinLoan.PaymentService/"]
COPY ["SharedKernel/CapFinLoan.SharedKernel/CapFinLoan.SharedKernel.csproj", "SharedKernel/CapFinLoan.SharedKernel/"]

RUN dotnet restore "Services/CapFinLoan.PaymentService/CapFinLoan.PaymentService.csproj"

COPY . .

WORKDIR "/src/Services/CapFinLoan.PaymentService"
RUN dotnet publish "CapFinLoan.PaymentService.csproj" \
    -c Release -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=build /app/publish .

RUN mkdir -p logs

EXPOSE 5006
ENTRYPOINT ["dotnet", "CapFinLoan.PaymentService.dll"]
