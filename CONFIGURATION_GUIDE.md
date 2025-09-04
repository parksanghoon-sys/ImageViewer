# ImageViewer 데이터베이스 구성 가이드

## 개요

ImageViewer Auth Service는 설정을 통해 다양한 데이터베이스 타입을 지원합니다.

## 지원 데이터베이스

1. **InMemory** - 개발 및 테스트용 (기본값)
2. **PostgreSQL** - 프로덕션 권장
3. **SQL Server** - 엔터프라이즈 환경

## 설정 방법

### appsettings.json 구성

```json
{
  "Database": {
    "Type": "PostgreSQL",  // InMemory, PostgreSQL, SqlServer 중 선택
    "ConnectionStrings": {
      "InMemory": "ImageViewer_AuthService_InMemory",
      "PostgreSQL": "Host=postgres;Port=5432;Database=ImageViewerDB;Username=postgres;Password=your_password_here;",
      "SqlServer": "Server=(localdb)\\mssqllocaldb;Database=ImageViewerDB;Trusted_Connection=true;MultipleActiveResultSets=true;"
    }
  }
}
```

### 환경별 설정

#### 개발 환경 (appsettings.Development.json)
```json
{
  "Database": {
    "Type": "PostgreSQL",
    "ConnectionStrings": {
      "PostgreSQL": "Host=postgres;Port=5432;Database=ImageViewerDB;Username=postgres;Password=your_password_here;"
    }
  }
}
```

#### 프로덕션 환경 (appsettings.Production.json)
```json
{
  "Database": {
    "Type": "PostgreSQL",
    "ConnectionStrings": {
      "PostgreSQL": "Host=your-prod-server;Port=5432;Database=ImageViewerDB;Username=prod_user;Password=secure_password;"
    }
  }
}
```

## Docker Compose에서 사용

Docker Compose 환경에서는 환경 변수를 통해 설정할 수 있습니다:

```yaml
auth-service:
  environment:
    - Database__Type=PostgreSQL
    - Database__ConnectionStrings__PostgreSQL=Host=postgres;Port=5432;Database=ImageViewerDB;Username=postgres;Password=your_password_here;
```

## 데이터베이스별 특징

### InMemory
- **장점**: 빠른 시작, 테스트에 적합
- **단점**: 재시작 시 데이터 손실
- **용도**: 개발, 테스트

### PostgreSQL
- **장점**: 높은 성능, 확장성, 오픈소스
- **단점**: 별도 서버 필요
- **용도**: 프로덕션 권장

### SQL Server
- **장점**: Microsoft 생태계 통합
- **단점**: 라이센스 비용
- **용도**: 엔터프라이즈 환경

## 마이그레이션

### 새 마이그레이션 생성
```bash
dotnet ef migrations add MigrationName --project src/Infrastructure/ImageViewer.Infrastructure --startup-project src/Services/ImageViewer.AuthService
```

### 마이그레이션 적용
```bash
dotnet ef database update --project src/Infrastructure/ImageViewer.Infrastructure --startup-project src/Services/ImageViewer.AuthService
```

### 마이그레이션 삭제
```bash
dotnet ef migrations remove --project src/Infrastructure/ImageViewer.Infrastructure --startup-project src/Services/ImageViewer.AuthService
```

## 문제 해결

### 연결 실패 시
1. 연결 문자열 확인
2. 데이터베이스 서버 상태 확인
3. 방화벽 설정 확인
4. 로그 파일 확인 (`logs/auth-service-.txt`)

### 마이그레이션 오류 시
1. 마이그레이션 히스토리 확인
2. 데이터베이스 백업 후 재시도
3. 수동 마이그레이션 고려

## 보안 주의사항

1. **연결 문자열에 민감 정보 포함 시 주의**
2. **프로덕션에서는 강력한 암호 사용**
3. **최소 권한 원칙 적용**
4. **정기적인 보안 패치 적용**

## 성능 최적화

1. **연결 풀 설정 최적화**
2. **인덱스 적절히 생성**
3. **쿼리 성능 모니터링**
4. **정기적인 통계 업데이트**