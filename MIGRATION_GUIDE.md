# ImageViewer 마이그레이션 가이드

## 개요

이 문서는 ImageViewer 프로젝트에서 Entity Framework Core 마이그레이션을 관리하는 방법을 안내합니다.

## 프로젝트 구조

```
ImageViewer/
├── src/
│   ├── Infrastructure/ImageViewer.Infrastructure/     # DbContext 및 마이그레이션
│   ├── Services/ImageViewer.AuthService/             # Auth 서비스 (Startup 프로젝트)
│   ├── Services/ImageViewer.ImageService/            # Image 서비스
│   └── Services/ImageViewer.ShareService/            # Share 서비스
```

## 사전 준비

### 1. .NET EF Core 도구 설치
```bash
# 전역 설치
dotnet tool install --global dotnet-ef

# 업데이트
dotnet tool update --global dotnet-ef

# 버전 확인
dotnet ef --version
```

### 2. 프로젝트 루트로 이동
```bash
cd D:\MyStudy\05.WebProject\07.ImageViewer
```

## Auth 서비스 마이그레이션

Auth 서비스는 별도의 `AuthContext`를 사용합니다.

### 새 마이그레이션 생성

```bash
# 기본 문법
dotnet ef migrations add [마이그레이션명] \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext

# 예시
dotnet ef migrations add InitialAuthCreate \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext
```

### 마이그레이션 적용

```bash
# 최신 마이그레이션 적용
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext

# 특정 마이그레이션으로 되돌리기
dotnet ef database update [마이그레이션명] \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext
```

### 마이그레이션 조회

```bash
# 마이그레이션 목록 확인
dotnet ef migrations list \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext

# 마이그레이션 스크립트 생성
dotnet ef migrations script \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext \
  --output auth_migration.sql
```

### 마이그레이션 제거

```bash
# 최신 마이그레이션 제거 (아직 적용하지 않은 경우만)
dotnet ef migrations remove \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext
```

## 메인 서비스 마이그레이션

Image, Share 서비스는 공통 `ApplicationDbContext`를 사용합니다.

### Image 서비스를 통한 마이그레이션

```bash
# 새 마이그레이션 생성
dotnet ef migrations add [마이그레이션명] \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.ImageService \
  --context ApplicationDbContext

# 마이그레이션 적용
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.ImageService \
  --context ApplicationDbContext
```

### Share 서비스를 통한 마이그레이션

```bash
# 새 마이그레이션 생성
dotnet ef migrations add [마이그레이션명] \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.ShareService \
  --context ApplicationDbContext

# 마이그레이션 적용
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.ShareService \
  --context ApplicationDbContext
```

## 데이터베이스별 설정

### PostgreSQL 마이그레이션

```bash
# appsettings.Development.json에서 PostgreSQL 설정 확인
{
  "Database": {
    "Type": "PostgreSQL",
    "ConnectionStrings": {
      "PostgreSQL": "Host=localhost;Port=5432;Database=ImageViewerDB;Username=postgres;Password=your_password;"
    }
  }
}

# 마이그레이션 실행
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext \
  --environment Development
```

### SQL Server 마이그레이션

```bash
# appsettings에서 SQL Server 설정
{
  "Database": {
    "Type": "SqlServer",
    "ConnectionStrings": {
      "SqlServer": "Server=(localdb)\\mssqllocaldb;Database=ImageViewerDB;Trusted_Connection=true;"
    }
  }
}
```

## Docker 환경에서 마이그레이션

### 1. 컨테이너 실행 중 마이그레이션

```bash
# Auth 서비스 컨테이너에서 마이그레이션 실행
docker exec -it imageviewer_authservice dotnet ef database update \
  --project /src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project /src/Services/ImageViewer.AuthService \
  --context AuthContext
```

### 2. 초기화 스크립트로 마이그레이션

`init-auth-db.sh` 파일 생성:
```bash
#!/bin/bash
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext
```

## 실용적인 마이그레이션 예시

### 1. 사용자 테이블에 새 컬럼 추가

```bash
# 1. 도메인 엔티티 수정 (User.cs에 새 속성 추가)
# 2. 마이그레이션 생성
dotnet ef migrations add AddUserProfilePicture \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext

# 3. 마이그레이션 적용
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext
```

### 2. 이미지 테이블 스키마 변경

```bash
# 1. Image 엔티티 수정
# 2. 마이그레이션 생성
dotnet ef migrations add UpdateImageSchema \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.ImageService \
  --context ApplicationDbContext

# 3. 마이그레이션 검토
dotnet ef migrations script UpdateImageSchema \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.ImageService \
  --context ApplicationDbContext

# 4. 마이그레이션 적용
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.ImageService \
  --context ApplicationDbContext
```

## 일괄 처리 스크립트

### Windows (migrate-all.bat)

```batch
@echo off
echo "=== ImageViewer 마이그레이션 실행 ==="

echo "1. Auth 서비스 마이그레이션..."
dotnet ef database update ^
  --project src/Infrastructure/ImageViewer.Infrastructure ^
  --startup-project src/Services/ImageViewer.AuthService ^
  --context AuthContext

if %ERRORLEVEL% neq 0 (
    echo "Auth 마이그레이션 실패"
    exit /b 1
)

echo "2. Image 서비스 마이그레이션..."
dotnet ef database update ^
  --project src/Infrastructure/ImageViewer.Infrastructure ^
  --startup-project src/Services/ImageViewer.ImageService ^
  --context ApplicationDbContext

if %ERRORLEVEL% neq 0 (
    echo "Image 마이그레이션 실패"
    exit /b 1
)

echo "마이그레이션 완료!"
```

### Linux/Mac (migrate-all.sh)

```bash
#!/bin/bash
set -e

echo "=== ImageViewer 마이그레이션 실행 ==="

echo "1. Auth 서비스 마이그레이션..."
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext

echo "2. Image 서비스 마이그레이션..."
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.ImageService \
  --context ApplicationDbContext

echo "마이그레이션 완료!"
```

## 문제 해결

### 1. 마이그레이션 충돌 해결

```bash
# 현재 상태 확인
dotnet ef migrations list \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext

# 문제가 있는 마이그레이션 제거
dotnet ef migrations remove \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext

# 새로운 마이그레이션 생성
dotnet ef migrations add FixedMigration \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext
```

### 2. 데이터베이스 초기화

```bash
# 데이터베이스 삭제
dotnet ef database drop \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext \
  --force

# 처음부터 다시 마이그레이션
dotnet ef database update \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext
```

### 3. 프로덕션 마이그레이션 스크립트 생성

```bash
# 프로덕션용 SQL 스크립트 생성
dotnet ef migrations script \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext \
  --idempotent \
  --output scripts/auth_production_migration.sql

# 특정 범위 마이그레이션 스크립트
dotnet ef migrations script FromMigration ToMigration \
  --project src/Infrastructure/ImageViewer.Infrastructure \
  --startup-project src/Services/ImageViewer.AuthService \
  --context AuthContext \
  --output scripts/specific_migration.sql
```

## 베스트 프랙티스

### 1. 마이그레이션 네이밍 컨벤션
- `Add[Entity][Field]`: 새 필드 추가 (`AddUserProfilePicture`)
- `Remove[Entity][Field]`: 필드 제거 (`RemoveUserAge`)
- `Update[Entity]Schema`: 스키마 변경 (`UpdateImageSchema`)
- `Create[Entity]Table`: 새 테이블 생성 (`CreateNotificationTable`)

### 2. 마이그레이션 전 체크리스트
- [ ] 모든 변경사항이 코드에 반영되었는가?
- [ ] 테스트 환경에서 마이그레이션을 검증했는가?
- [ ] 데이터 손실 위험은 없는가?
- [ ] 백업이 준비되어 있는가?

### 3. 프로덕션 배포 시 주의사항
- 항상 데이터베이스 백업 후 진행
- SQL 스크립트로 검토 후 적용
- 롤백 계획 준비
- 다운타임 최소화 전략 수립

## 자동화 스크립트

### GitHub Actions 예시

```yaml
name: Database Migration

on:
  push:
    branches: [main]
    paths: 
      - 'src/Infrastructure/**'
      - 'src/Core/**'

jobs:
  migrate:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Install EF Tools
      run: dotnet tool install --global dotnet-ef
      
    - name: Run Migrations
      run: |
        dotnet ef database update \
          --project src/Infrastructure/ImageViewer.Infrastructure \
          --startup-project src/Services/ImageViewer.AuthService \
          --context AuthContext
```

이 가이드를 참조하여 안전하고 효율적으로 데이터베이스 마이그레이션을 관리하세요!