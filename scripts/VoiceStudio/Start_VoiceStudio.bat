@echo off
color 0B
set ELEVENLABS_API_KEY=sk_f5a5665bd581c997aa028729d2fd386e455995b0bd6f392c
echo ========================================================
echo       FLIGHT SUPERVISOR - VOICE STUDIO LAUNCHER
echo ========================================================
echo.
echo Lancement du serveur Python local...

:: Change dir to VoiceStudio
cd /d "%~dp0"

:: Start Flask app in background
start /B ..\..\.venv\Scripts\python.exe app.py

:: Wait 2 seconds for server to start
timeout /t 2 /nobreak >nul

:: Open default browser
start http://localhost:5050

echo.
echo Le serveur tourne sur http://localhost:5050
echo L'interface devrait s'ouvrir dans votre navigateur.
echo.
echo Gardez cette fenetre ouverte pendant que vous utilisez Voice Studio.
echo Pour quitter, fermez simplement cette fenetre.
echo.
pause
