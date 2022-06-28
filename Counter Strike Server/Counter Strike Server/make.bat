@echo off
echo Loading mono
SET PATH=%PATH%;C:\Program Files\Mono\bin

echo Compiling project
call csc -out:cs_server.exe *.cs
echo.
echo Done
echo.
pause