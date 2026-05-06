@echo off
echo Lanzando MotorDsl.Integrated.MultaApp en dispositivo Android...

call ./update-packages.bat
call ./run-MotorDsl.Integrated.MultaApp.bat
call ./run-MotorDsl.MultaApp.bat
call ./run-MotorDsl.Nuget.Integrated.MultaApp.bat
call ./run-MotorDsl.Nuget.MultaApp.bat
call ./run-MotorDsl.SampleApp.bat

