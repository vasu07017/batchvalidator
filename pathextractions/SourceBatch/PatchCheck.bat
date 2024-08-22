: ************************** MN_AC_E01 ************************** :
:                                                    2023/09/27   :
:   Report Shelter AccessLog Copy and format Check         :
:   Version 1.0                                                   :
:                                                     XYZUVW     :
: **************************************************************  :
@call common\header.bat

: ************** state environmental variables *****************:
call InitFile.bat

:*************** STEP10_INIT_PARAMETER CHECK ***********:
if not exist %AAAA_PATH%\%SAMPLE_FILE% @call print_err.bat STEP10_INIT_PARAMETER_CHECK 910
if not exist %BBBB_TOBEPROCESSED% @call print_err.bat STEP10_INIT_PARAMETER_CHECK 911
if not exist %TT_LOGS% @call print_err.bat STEP10_INIT_PARAMETER_CHECK 912
if not exist %KBC_LOGS% @call print_err.bat STEP10_INIT_PARAMETER_CHECK 912
*********** STEP20_BEFORE_DIR_CHECK *****:
@call common\stepname.bat STEP20_BEFORE_DIR_CHECK
dir /S %AAAA_PATHS%
dir /S %BBBB_TOBEPROCESSED%
dir /S %TT_LOGS%
	



if errorlevel 2 @call print_err.bat STEP20_BEFORE_DIR_CHECK 920

**************** STEP40_DEL_AND_FILE_COPY**********:
@call common stepname.bat STEP30_DEL_AND_FILE_COPY
if exist %BBBB_TOBEPROCESSED%\%SAMPLE_FILES% DEL /F /Q %BBBB_TOBEPROCESSED%\%SAMPLE_FILE%
dir /a-d %BBBB_TOBEPROCESSED%\%SAMPLE_FILES%  >NUL 2>MUL && (
cscript //NoLogo %vbsPath%sleep.vbs 60

if exist %BBBB_TOBEPROCESSED%\%SAMPLE_FILES% DEL /F /Q %BBBB_TOBEPROCESSED%\%SAMPLE_FILES%
)
if exist %BBBB_TOBEPROCESSED%\%SAMPLE_FILES% DEL /F /Q %BBBB_TOBEPROCESSED%\%SAMPLE_FILE%
dir /a-d %BBBB_TOBEPROCESSED%\%SAMPLE_FILES%  >NUL 2>MUL && (


if exist %BBBB_TOBEPROCESSED%\%SAMPLE_FILES% DEL /F /Q %BBBB_TOBEPROCESSED%\%SAMPLE_FILES%
)

dir /a-d %BBBB_TOBEPROCESSED%\%SAMPLE_FILES%  >NUL 2>NUL && @call print_err.bat STEP40_DEL_AND_FILE_COPY 940 
dir /S %BBBB_TOBEPROCESSED%

ROBOCOPY %AAAA_PATHS% %BBBB_TOBEPROCESSED%\%SAMPLE_FILES% /IS /R:3 /W:10 
if errorlevel 4 call print_err.bat STEP40_DEL_AND_FILE_COPY 941
dir /S %BBBB_TOBEPROCESSED%

:********* STEP50_EBANGO_USERID_FILE_FORMAT_CHECK ***************************:
@call stepname.bat STEP5O EBANGO_USERID_FILE_FORMAT_CHECK
FOR /f "tokens="%%P IN ('dir /A-d /b /O:N "%BBBB_TOBEPROCESSED%\%SAMPLE_FILES%"') do (
%CSV_PRECHECK_EXE% AL_EBANGO %BBBB_TOBEPROCESSED%\%%P %TT_LOGS%\%SAMPLE_FORMAT_CHECK_RESULT_LOG%
if errorlevel 1 @call print_err.bat STEP50_EBANGO_USERID_FILE_FORMAT_CHECK 950

*********** STEP60_AFTER_DIR_CHECK *****:
@call common\stepname.bat STEP6O AFTER_DIR_CHECK
dir /S %AAAA_PATHS%
dir /S %BBBB_TOBEPROCESSED%
dir /S %TT_LOGS%
1f errorlevel 2 @call print_err.bat STEP60_AFTER_DIR_CHECK 960
:************ End of Script *****:
@call batchend.bat
exit