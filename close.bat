@echo off
for %%p in (5294 5215 5125 3000) do (
    for /f "tokens=5" %%a in ('netstat -ano ^| findstr :%%p') do (
        echo 포트 %%p -> PID %%a 종료 중...
        taskkill /PID %%a /F
    )
)
