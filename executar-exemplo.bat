@echo off
rem Sobe o app e abre no Chrome já com a loja de exemplo completa (6 pessoas, 3 turnos, histórico escalonado).
rem ATENÇÃO: substitui os dados que estiverem no navegador.
cd /d "%~dp0"
set URL=http://localhost:5194
set ASPNETCORE_ENVIRONMENT=Development

start "" /b cmd /c "for /l %%i in (1,1,120) do (curl -s -o nul %URL% && (start chrome %URL%/inicio?carregar=exemplo & exit) || timeout /t 1 >nul)"

dotnet run --project src\Folguinha.Web --no-launch-profile --urls %URL%
pause
