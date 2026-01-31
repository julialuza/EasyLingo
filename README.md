# EasyLingo

EasyLingo to aplikacja desktopowa do nauki języków obcych (angielski i niemiecki) w technologii WPF (.NET 8).  
Umożliwia rejestrację, logowanie, naukę słówek, quizy, śledzenie postępów oraz zdobywanie odznak.

---

## Wymagania

- Visual Studio 2022
- .NET 8 SDK
- Git
- Połączenie z Internetem do pobrania paczek NuGet

> Nie wymaga instalacji SQLite – jest wbudowane w EF Core.

---

## 1️⃣ Sklonowanie repozytorium

1. Otwórz Visual Studio 2022.
2. Wybierz **File → Open → Open from Git…** lub **Clone a repository**.
3. Wklej URL repozytorium:

https://github.com/julialuza09/EasyLingo


4. Wybierz lokalizację na dysku i kliknij **Clone**.

---

## 2️⃣ Przywrócenie paczek NuGet

1. W **Solution Explorer** kliknij prawym przy projekcie `EasyLingo`.
2. Wybierz **Restore NuGet Packages**.
3. Upewnij się, że masz zainstalowane:
   - Microsoft.EntityFrameworkCore
   - Microsoft.EntityFrameworkCore.Sqlite
   - Microsoft.EntityFrameworkCore.Tools

---

## 3️⃣ Instalacja narzędzi EF Core (tylko jeśli nie są zainstalowane)

W terminalu (Visual Studio → **Tools → Terminal** lub CMD/PowerShell) uruchom:

```powershell
dotnet tool restore
dotnet tool install --global dotnet-ef
```

## 4️⃣ Tworzenie migracji i bazy danych

Ponieważ migracje **nie są w repozytorium**, musisz je wygenerować lokalnie.

1. Otwórz terminal w Visual Studio (**Tools → Terminal**) lub PowerShell.
2. Przejdź do katalogu projektu (tam gdzie znajduje się `EasyLingo.csproj`):

```powershell
cd ścieżka/do/EasyLingo
dotnet ef migrations add InitialCreate
dotnet ef database update
```


