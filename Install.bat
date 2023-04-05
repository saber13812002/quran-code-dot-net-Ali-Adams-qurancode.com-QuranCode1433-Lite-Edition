@echo off
IF EXIST Release.bat GOTO :END

XCOPY /Q /H /E /Y Source\DataAccess\Audio\*.*        .\Audio\
XCOPY /Q /H /E /Y Source\DataAccess\Data\*.*         .\Data\
XCOPY /Q /H /E /Y Source\DataAccess\Translations\*.* .\Translations\
XCOPY /Q /H /E /Y Source\Model\Data\*.*              .\Data\
XCOPY /Q /H /E /Y Source\QuranCode\Fonts\*.*         .\Fonts\
XCOPY /Q /H /E /Y Source\QuranCode\Help\*.*          .\Help\
XCOPY /Q /H /E /Y Source\QuranCode\Images\*.*        .\Images\
XCOPY /Q /H /E /Y Source\Server\Rules\*.*            .\Rules\
XCOPY /Q /H /E /Y Source\Server\Values\*.*           .\Values\
XCOPY /Q /H /E /Y Source\Utilities\Numbers\*.*       .\Numbers\
COPY /Y Source\LICENSE .
REM COPY /Y Source\Readme.txt .
REM COPY /Y Source\Features.txt .
COPY /Y Build\Release\*.* .
RD /S /Q Build
REN Source C#
DEL Install.bat

:END
