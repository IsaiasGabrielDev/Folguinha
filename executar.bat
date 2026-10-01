@echo off
cd /d "%~dp0"
set URL=http://localhost:5194
set ASPNETCORE_ENVIRONMENT=Development

rem Abre o Chrome assim que o servidor responder.
start "" /b cmd /c "for /l %%i in (1,1,120) do (curl -s -o nul %URL% && (start chrome %URL% & exit) || timeout /t 1 >nul)"

dotnet run --project src\Folguinha.Web --no-launch-profile --urls %URL%
pause
