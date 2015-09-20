SETLOCAL EnableExtensions

:logtimestamp
REM ***** Setup LogFile with timestamp *****
set timehour=%time:~0,2%
set timestamp=%date:~-4,4%%date:~-10,2%%date:~-7,2%-%timehour: =0%%time:~3,2%
set newreliclog=%PathToInstallLogs%newreliclog-%timestamp%.txt
echo Logfile generated at: %newreliclog% >> %newreliclog%

:: Path used for custom configuration and worker role environment varibles
SET NR_HOME=%ALLUSERSPROFILE%\New Relic\.NET Agent\

:: Must copy twice in case the files have been updated.
:: CUSTOM newrelic.config : Uncomment the line below if you want to copy a custom newrelic.config file into your instance
copy /Y newrelic.config %NR_HOME% >> %newreliclog%
:: CUSTOM INSTRUMENTATION : Uncomment the line below to copy custom instrumentation into the agent directory.
copy /y CustomInstrumentation.xml %NR_HOME%\extensions >> %newreliclog%

:: Quit if running on local PC.
if not %localappdata%=="" EXIT /B 0

for /F "usebackq tokens=1,2 delims==" %%i in (`wmic os get LocalDateTime /VALUE 2^>NUL`) do if '.%%i.'=='.LocalDateTime.' set ldt=%%j
set ldt=%ldt:~0,4%-%ldt:~4,2%-%ldt:~6,2% %ldt:~8,2%:%ldt:~10,2%:%ldt:~12,6%

SET NR_ERROR_LEVEL=0

CALL:INSTALL_NEWRELIC_AGENT
CALL:INSTALL_NEWRELIC_SERVER_MONITOR

IF %NR_ERROR_LEVEL% EQU 0 (
	EXIT /B 0
) ELSE (
	EXIT %NR_ERROR_LEVEL%
)

:: --------------
:: Functions
:: --------------
:INSTALL_NEWRELIC_AGENT
	ECHO %ldt% : Begin installing the New Relic .net Agent >> "%newreliclog%" 2>&1

	:: Current version of the installer
	SET NR_INSTALLER_NAME=NewRelicAgent_x64_5.5.52.0.msi
	:: Path used for custom configuration and worker role environment varibles
	SET NR_HOME=%ALLUSERSPROFILE%\New Relic\.NET Agent\

	ECHO Installing the New Relic .net Agent. >> "%newreliclog%" 2>&1

	IF "%IsWorkerRole%" EQU "true" (
	    msiexec.exe /i %NR_INSTALLER_NAME% /norestart /quiet NR_LICENSE_KEY=%LICENSE_KEY% INSTALLLEVEL=50 /lv* %newreliclog%2
	) ELSE (
	    msiexec.exe /i %NR_INSTALLER_NAME% /norestart /quiet NR_LICENSE_KEY=%LICENSE_KEY% /lv* %newreliclog%2
	)

	:: CUSTOM newrelic.config : Uncomment the line below if you want to copy a custom newrelic.config file into your instance
	copy /Y newrelic.config %NR_HOME% >> %newreliclog%

	:: CUSTOM INSTRUMENTATION : Uncomment the line below to copy custom instrumentation into the agent directory.
	copy /y CustomInstrumentation.xml %NR_HOME%\extensions >> %newreliclog%

	:: WEB ROLES : Restart the service to pick up the new environment variables
	:: 	if we are in a Worker Role then there is no need to restart W3SVC _or_
	:: 	if we are emulating locally then do not restart W3SVC
	IF "%IsWorkerRole%" EQU "false" IF "%EMULATED%" EQU "false" (
		ECHO Restarting IIS and W3SVC to pick up the new environment variables >> "%RoleRoot%\nr.log" 2>&1
		IISRESET
		NET START W3SVC
	)

	IF %ERRORLEVEL% EQU 0 (
	  REM  The New Relic .net Agent installed ok and does not need to be installed again.
	  ECHO New Relic .net Agent was installed successfully. >> "%newreliclog%" 2>&1

	) ELSE (
	  REM   An error occurred. Log the error to a separate log and exit with the error code.
	  ECHO  An error occurred installing the New Relic .net Agent 1. Errorlevel = %ERRORLEVEL%. >> "%newreliclog%" 2>&1

	  SET NR_ERROR_LEVEL=%ERRORLEVEL%
	)

GOTO:EOF

:INSTALL_NEWRELIC_SERVER_MONITOR
	ECHO %ldt% : Begin installing the New Relic Server Monitor >> "%newreliclog%" 2>&1

	:: Current version of the installer
	SET NR_INSTALLER_NAME=NewRelicServerMonitor_x64_3.3.3.0.msi

	ECHO Installing the New Relic Server Monitor. >> "%newreliclog%" 2>&1
	msiexec.exe /i %NR_INSTALLER_NAME% /norestart /quiet NR_LICENSE_KEY=%LICENSE_KEY% /lv* %newreliclog%2

	IF %ERRORLEVEL% EQU 0 (
	  REM  The New Relic Server Monitor installed ok and does not need to be installed again.
	  ECHO New Relic Server Monitor was installed successfully. >> "%newreliclog%" 2>&1

	  NET STOP "New Relic Server Monitor Service"
	  NET START "New Relic Server Monitor Service"

	) ELSE (
	  REM   An error occurred. Log the error to a separate log and exit with the error code.
	  ECHO  An error occurred installing the New Relic Server Monitor 1. Errorlevel = %ERRORLEVEL%. >> "%newreliclog%" 2>&1

	  SET NR_ERROR_LEVEL=%ERRORLEVEL%
	)

GOTO:EOF
