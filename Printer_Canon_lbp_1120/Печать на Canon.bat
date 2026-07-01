@echo off
chcp 65001 >nul
title Печать на Canon LBP-1120
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Print-Canon.ps1"
