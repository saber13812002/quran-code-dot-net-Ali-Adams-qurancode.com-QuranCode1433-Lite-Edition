@echo off
:START

cd Tools
CALL Clean
cd ..

del /F    /Q *.zip
del /F /S /Q *.tmp
del /F /S /Q *.bak
del /F /S /Q *.ini
del /F /S /Q *.user
del /F /S /Q *.pdb
del /F /S /Q *.resources
del /F /S /Q *.vshost.exe
del /F /S /Q *.vshost.exe.manifest
del /F /S /Q *.exe.config

rd /S /Q Build\Debug

rd /S /Q Build\Release\Bookmarks
rd /S /Q Build\Release\History
rd /S /Q Build\Release\Drawings
rd /S /Q Build\Release\Statistics

rd /S /Q Globals\obj
rd /S /Q Utilities\obj
rd /S /Q Model\obj
rd /S /Q DataAccess\obj
rd /S /Q Server\obj
rd /S /Q Client\obj
rd /S /Q QuranCode\obj

:END
