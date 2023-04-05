@echo off

CALL Clean.bat
CALL Version.bat

REM -- Move source code to Source folder:
IF NOT EXIST Source MD Source
MOVE /Y LICENSE Source
MOVE /Y README.md Source
MOVE /Y Clean.bat Source
MOVE /Y *.txt Source
MOVE /Y *.sln Source
MOVE /Y Tools Source
MOVE /Y Globals Source
MOVE /Y Utilities Source
MOVE /Y Model Source
MOVE /Y DataAccess Source
MOVE /Y Server Source
MOVE /Y Client Source
MOVE /Y QuranCode Source
COPY /Y Version.bat Source
COPY /Y Release.bat Source
COPY /Y Install.bat Source
XCOPY /H *.suo Source

REM -- Archive the Source folder:
"%PROGRAMFILES%\7-Zip\7z.exe" a -tzip -mx5 QuranCode1433.Lite.zip Source
"%PROGRAMFILES%\7-Zip\7z.exe" a -tzip -mx5 QuranCode1433.Lite.zip Install.bat

REM -- Move back the contents of Source folder to their original location:
MOVE /Y Source\LICENSE .
MOVE /Y Source\README.md .
MOVE /Y Source\Clean.bat .
MOVE /Y Source\Install.bat .
MOVE /Y Source\*.txt .
MOVE /Y Source\*.sln .
MOVE /Y Source\Tools .
MOVE /Y Source\Globals .
MOVE /Y Source\Utilities .
MOVE /Y Source\Model .
MOVE /Y Source\DataAccess .
MOVE /Y Source\Server .
MOVE /Y Source\Client .
MOVE /Y Source\QuranCode .

REM // delete Source folder.
RD /S /Q Source

REM // add the contents of the Build\Release folder to the archive.
"%PROGRAMFILES%\7-Zip\7z.exe" a -tzip  QuranCode1433.Lite.zip Build\Release\*.bat
"%PROGRAMFILES%\7-Zip\7z.exe" a -tzip  QuranCode1433.Lite.zip Build\Release\*.txt
"%PROGRAMFILES%\7-Zip\7z.exe" a -tzip  QuranCode1433.Lite.zip Build\Release\*.dll
"%PROGRAMFILES%\7-Zip\7z.exe" a -tzip  QuranCode1433.Lite.zip Build\Release\*.exe

CALL Version.bat
