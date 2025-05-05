@echo off
REM Build script para ZariVirusKiller Client
echo Compilando solução em Release...
msbuild ZariVirusKiller.sln /p:Configuration=Release
echo Build concluído.
pause
