# Demo Local Configuration to Avoid Warn in DevSecOps Security Scanner Tool

## Never Commit
- appsettings.Development.json
- appsettings.Local.json
- secrets.json
- real passwords / tokens / API keys

Add the following items in `.gitignore`
```
#---------------
# Local secrets
**/appsettings.Development.json
**/appsettings.Local.json
**/secrets.json
**/.env
**/.env.*
```

To run locally, create a file `appsettings.Development.json` with the following content:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SecureDb;User Id=dev_user;Password=dev_password;TrustServerCertificate=true"
  },
  "Jwt": {
    "Key": "local-super-secret-jwt-key"
  },
  "Email": {
    "Username": "dev-mail@company.local",
    "Password": "dev-mail-password"
  }
}

```

We can also demonstrate **Secret** concept.
To demonstrate secret concept, create a file `secrets.json` with the following content:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "[SECRET]-Server=localhost;Database=SecureDb;User Id=sa;Password=super-secret"
  },
  "Jwt": {
    "Key": "[SECRET]-my-local-jwt-secret"
  },
  "Email": {
    "Username": "[SECRET]-local@company.local",
    "Password": "[SECRET]-local-mail-password"
  }
}
```

The content of `secrets.json` will override `appsettings.Development.json` content since secret registration comes after `.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)`

```csharp
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)

    //“Load User Secrets untuk assembly milik Program, dan kalau tidak ada file secret-nya, jangan error.”
    .AddUserSecrets<Program>(optional: true) 
    .AddEnvironmentVariables();
```

Open API through:
https://localhost:7137/Swagger/index.html


---

# FAQ
- Q: Bagaimana mapping json ke environment variable?
<br/>A: 

    | JSON                                  | Environment Variable                   |
    | ------------------------------------- | -------------------------------------- |
    | `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
    | `Jwt:Issuer`                          | `Jwt__Issuer`                          |
    | `Jwt:Audience`                        | `Jwt__Audience`                        |
    | `Jwt:Key`                             | `Jwt__Key`                             |
    | `Email:Host`                          | `Email__Host`                          |
    | `Email:Port`                          | `Email__Port`                          |
    | `Email:Username`                      | `Email__Username`                      |
    | `Email:Password`                      | `Email__Password`                      |

- Q: Bagaimana cara menjalankan?<br/>
    A: 
    -  Set manual
        - Linux / macOS
            ```bash
            # cara 1: ASPNETCORE_ENVIRONMENT
            ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/SecureConfigApi

            # cara 2: DOTNET_ENVIRONMENT
            DOTNET_ENVIRONMENT=Development dotnet run --project src/SecureConfigApi
            ```

            Kedua cara diatas akan produce:
            `builder.Environment.EnvironmentName = Development`

        - Windows
            ```bash
            # CMD
            set ASPNETCORE_ENVIRONMENT=Development
            dotnet run --project src/SecureConfigApi

            # Powershell
            $env:ASPNETCORE_ENVIRONMENT="Development"
            dotnet run --project src/SecureConfigApi
            ```
    - Use Profile
        - Use Terminal
            - Dengan specify Profile
                ```bash
                dotnet run --launch-profile Dev
                ```
            - Tanpa specify Profile
                ```bash
                dotnet run --project src/SecureConfigApi
                ```
                maka `dotnet run` biasanya **tetap mencoba memakai launch profile default/pertama** (jika tersedia, dalam contoh ini **Dev**), tetapi behavior ini tergantung tooling / host. 
                <br/>

            - Disable launch profile (seperti **Production/Kubernates/Container**)
                ```bash 
                dotnet run --no-launch-profile
                ```

        - Use IDE (Visual Studio / Rider)
            aaa

- Q: Bagaimana kalau tanpa set `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT`?
    <br/>A: Yang terjadi adalah, nilai `builder.Environment.EnvironmentName` akan bernilai default `Production`. 
    
    Sehingga :

    ```csharp
    .AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true)
    ```

    karena `optional: true`, maka:
    - Jika file `appsettings.Production.json` **ada** → akan dibaca
    - Jika file `appsettings.Production.json` **tidak ada** → **tidak error**, lanjut jalan
<br/>

- Q: Apa bagian `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT` apa bisa di set di file `.env`?<br/>
    A: Bisa, tapi **.NET tidak otomatis membaca file `.env`** seperti Node.js / Python.
<br/>

- Q: Jika web api saya di deploy di kubernates. lalu, saya set nilai dari `appsettings.json` di set via environment variable kubernates. default value di `appsettings.json` ini akan di**override** dari environment variable ya? <br/>

    A: **Ya, benar** — kalau Anda deploy ke Kubernetes dan set config via environment variable, maka nilai di `appsettings.json` akan di**override** oleh environment variable **selama urutan config provider-nya benar**.

    ```csharp
    builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();
    ```

    provider yang dibaca terakhir menang (**last provider wins**). Karena `.AddEnvironmentVariables()` ada **paling akhir**, maka **env var dari Kubernetes akan override** nilai yang sama dari `appsettings.json`.
<br/>

- Q: Apakah dalam docker file harus ada `--no-launch-profile`?<br/>
A: Tidak perlu — dan memang **tidak seharusnya**.
Saat masuk Docker / Kubernetes, aplikasi biasanya dijalankan dengan:

    ```dockerfile
    ENTRYPOINT ["dotnet", "SecureConfigApi.dll"]
    ```

    Hal lain, parameter `--no-launch-profile` itu parameter dari 
    ```bash
    dotnet run
    ```
    ini **bukan** menjalankan app Anda langsung, tapi menjalankan **.NET CLI command (dotnet CLI)** yang tugasnya:
    - cari project (.csproj)
    - build project
    - baca launchSettings.json
    - apply launch profile
    - baru jalankan app

    Sementara itu, dalam docker file ada perintah
    ```bash
    dotnet SecureConfigApi.dll
    ```
    ini **bukan CLI run command**. Tapi di mode ini, `dotnet` bertindak sebagai **runtime host, bukan project runner**. 
    <br/>

    Disamping itu, `launchSettings.json` itu **fitur tooling**, bukan fitur runtime ASP.NET Core, maka `launchSettings.json` akan dibaca oleh:

    - .NET CLI (dotnet run)
    - Visual Studio
    - Rider

    `launchSettings.json` tidak dipakai oleh:
    - CLR runtime
    - Kestrel runtime
    - dotnet MyApp.dll
    - Docker container runtime

    `launchSettings.json` juga **tidak ikut ke hasil publish**
