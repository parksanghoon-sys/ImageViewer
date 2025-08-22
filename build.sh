#!/usr/bin/env bash
dotnet build --configuration Release

# AuthService 시작
dotnet run --project src/Services/ImageViewer.AuthService &

# ImageService 시작  
dotnet run --project src/Services/ImageViewer.ImageService &

# ShareService 시작
dotnet run --project src/Services/ImageViewer.ShareService &

# 프론트엔드 실행
cd frontend
npm start &

# 모든 백그라운드 프로세스가 종료될 때까지 대기
wait