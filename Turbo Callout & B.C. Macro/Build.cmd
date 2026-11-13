@echo off
rem Consolidated release: build the single Turbo Callout application.
call "%~dp0..\Turbo Callout\Build.cmd"
exit /b %errorlevel%
