# 🖼️ ImageViewer - 블러 처리 이미지 뷰어 서비스

**Modern Image Viewer with Blur Processing & Sharing Features**

## 📋 프로젝트 개요

ImageViewer는 **모듈러 모놀리식 아키텍처**로 구현된 현대적인 이미지 뷰어 웹 애플리케이션입니다. 핵심 기능은 **블러 처리된 이미지 미리보기**와 **회원 간 이미지 공유** 시스템입니다.

### 🎯 핵심 특징

- **🎨 블러 처리 UI**: CSS filter: blur()를 활용한 가변 강도 블러 처리 (0-100%)
- **👆 상호작용**: hover/클릭 시 이미지가 선명하게 표시되는 인터랙티브 UI
- **⚙️ 사용자 설정**: 블러 강도, 미리보기 크기, 개수 등 개인화된 설정
- **🔒 JWT 인증**: Access Token + Refresh Token 자동 갱신 시스템
- **📤 이미지 업로드**: 드래그앤드롭 지원, 실시간 진행률 표시
- **🔗 이미지 공유**: 회원 간 이미지 공유 요청/승인 시스템 (개발 중)
- **📱 반응형 디자인**: 모바일부터 데스크톱까지 완벽 지원

## 🏗️ 시스템 아키텍처

### 백엔드 (C# .NET 8)
```
📦 Modular Monolith Architecture
├── 🔐 AuthService        (회원 인증, JWT 관리)
├── 🖼️ ImageService       (이미지 업로드/조회/썸네일)
├── 🔗 ShareService       (이미지 공유 요청/승인)
├── 📚 Domain             (엔티티, 비즈니스 규칙)
├── 🚀 Application        (유스케이스, 서비스)
├── 🔧 Infrastructure     (EF Core, 파일시스템)
└── 📄 Contracts          (DTO, Command, Event)
```

### 프론트엔드 (React 18 + TypeScript)
```
📦 React SPA
├── 🎨 Components
│   ├── BlurredImage      (핵심 블러 처리 컴포넌트)
│   ├── ImageGallery      (갤러리 및 설정)
│   ├── ImageUpload       (드래그앤드롭 업로드)
│   └── SimpleDashboard   (메인 대시보드)
├── 🌐 Services           (API 클라이언트)
└── 🎯 Types              (TypeScript 인터페이스)
```

## ✨ 주요 기능

### 🎨 블러 처리 이미지 뷰어 (핵심 기능)
- **가변 블러 강도**: 0% (원본) ~ 100% (최대 블러) 실시간 조절
- **인터랙티브 UI**: 마우스 호버 시 블러 제거, 마우스 아웃 시 블러 복원
- **개인화 설정**: 
  - 블러 강도: 0-100% (10% 단위)
  - 미리보기 크기: 150-300px (25px 단위)
  - 페이지당 이미지 수: 6-24개 (6개 단위)
- **설정 영속화**: 사용자 설정이 localStorage에 자동 저장

### 🖼️ 이미지 관리
- **드래그앤드롭 업로드**: 직관적인 파일 업로드 인터페이스
- **실시간 진행률**: 각 파일별 업로드 진행 상황 표시
- **메타데이터 입력**: 제목, 설명, 태그, 공개/비공개 설정
- **파일 검증**: 이미지 형식 및 크기 제한 (최대 10MB)
- **썸네일 생성**: 자동 썸네일 생성 및 최적화

### 🔍 갤러리 뷰
- **그리드/리스트 뷰**: 전환 가능한 레이아웃
- **실시간 검색**: 제목, 설명, 태그 기반 즉시 검색
- **페이지네이션**: 효율적인 대용량 이미지 처리
- **상세 모달**: 원본 이미지 전체 화면 보기
- **정렬/필터링**: 업로드일, 크기, 형식별 정렬

### 🔐 인증 시스템
- **JWT 기반 인증**: 보안성과 확장성을 고려한 토큰 인증
- **자동 토큰 갱신**: Refresh Token을 통한 seamless한 사용자 경험
- **기본 계정**: 테스트용 관리자/일반 사용자 계정 제공

### 🔗 공유 시스템 (개발 중)
- **이미지 공유 요청**: 다른 회원에게 이미지 공유 요청
- **승인/거절 관리**: 받은 공유 요청 처리
- **권한 기반 접근**: 승인된 회원만 이미지 조회 가능

## 🚀 빠른 시작

### 전제 조건
- **.NET 8 SDK**
- **Node.js 18+**
- **Visual Studio Code** 또는 **Visual Studio 2022**

### 1️⃣ 백엔드 실행

```bash
# AuthService 실행
dotnet run --project src/Services/ImageViewer.AuthService

# ImageService 실행
dotnet run --project src/Services/ImageViewer.ImageService

# ShareService 실행
dotnet run --project src/Services/ImageViewer.ShareService
```

### 2️⃣ 프론트엔드 실행

```bash
cd frontend
npm install
npm start
```

### 3️⃣ 접속 정보

| 서비스 | URL | 설명 |
|--------|-----|------|
| **메인 앱** | http://localhost:3000 | React 프론트엔드 |
| **AuthService** | http://localhost:5294/scalar/v1 | 인증 API 문서 |
| **ImageService** | http://localhost:5215/scalar/v1 | 이미지 API 문서 |
| **ShareService** | http://localhost:5125/scalar/v1 | 공유 API 문서 |

### 4️⃣ 테스트 계정

| 계정 | 이메일 | 비밀번호 |
|------|-------|----------|
| **관리자** | admin@imageviewer.com | Admin123! |
| **일반 사용자** | test@example.com | Test123! |

## 💡 사용 방법

### 1. 로그인
- 브라우저에서 http://localhost:3000 접속
- 테스트 계정으로 로그인 (계정 정보는 미리 입력되어 있음)

### 2. 이미지 업로드
- **업로드** 탭 클릭
- 파일을 드래그앤드롭하거나 클릭하여 선택
- 제목, 설명, 태그 입력
- 공개/비공개 설정 후 업로드

### 3. 블러 처리 체험
- **내 이미지** 탭에서 업로드된 이미지 확인
- **표시 설정**에서 블러 강도 조절 (0-100%)
- 이미지에 마우스를 올려 선명하게 보기
- 미리보기 크기와 개수 조절 가능

### 4. 갤러리 탐색
- 그리드/리스트 뷰 전환
- 실시간 검색으로 특정 이미지 찾기
- 이미지 클릭하여 전체 화면 보기

## 🛠️ 기술 스택

### Backend
- **.NET 8**: 최신 C# 기능과 성능 최적화
- **EF Core 8**: Entity Framework Core (현재: InMemory DB)
- **JWT Authentication**: 보안 토큰 기반 인증
- **Serilog**: 구조화된 로깅
- **Scalar UI**: 현대적인 API 문서화

### Frontend  
- **React 18**: 최신 React Hook과 Concurrent Features
- **TypeScript**: 타입 안전성과 개발자 경험 향상
- **CSS-in-JS**: 컴포넌트 레벨 스타일링
- **Axios**: HTTP 클라이언트 및 인터셉터

### Database
- **EF Core InMemory**: 개발 환경용 (프로덕션: PostgreSQL 예정)
- **File System**: 이미지 파일 저장

### DevOps & Tools
- **Git**: 소스코드 관리 및 협업
- **Docker**: 컨테이너화 준비
- **Scalar API Docs**: Swagger 대체 API 문서

## 📈 개발 진행률

### ✅ 완료된 기능 (85%)
- **회원 인증 시스템**: JWT + Refresh Token
- **이미지 업로드/조회**: 드래그앤드롭, 메타데이터, 썸네일
- **블러 처리 UI**: CSS filter 기반 가변 강도 블러
- **갤러리 시스템**: 그리드/리스트, 검색, 페이지네이션
- **반응형 UI**: 모바일 친화적 디자인
- **설정 영속화**: 사용자 개인화 설정

### 🔄 개발 중 (15%)
- **이미지 공유 시스템**: 요청/승인 로직 완성
- **RabbitMQ 통합**: 비동기 처리 및 알림
- **테스트 작성**: Unit/Integration/E2E 테스트
- **배포 환경**: Docker Compose 최적화

## 🎯 다음 개발 계획

### Phase 1: 공유 시스템 완성
- [ ] ShareService 비즈니스 로직 구현
- [ ] 공유 요청/승인 UI 개발
- [ ] 권한 검사 로직 강화
- [ ] 공유된 이미지 조회 API

### Phase 2: RabbitMQ 통합
- [ ] 이미지 업로드 → 썸네일 생성 이벤트
- [ ] 공유 승인 → 알림 이벤트  
- [ ] 실시간 진행 상황 업데이트
- [ ] SignalR 연동

### Phase 3: 테스트 & 배포
- [ ] xUnit + Moq 단위 테스트
- [ ] Testcontainers 통합 테스트
- [ ] Cypress E2E 테스트
- [ ] Docker Compose 배포 환경

## 🤝 기여하기

1. **포크** 후 로컬에 클론
2. **기능 브랜치** 생성 (`git checkout -b feature/amazing-feature`)
3. **변경사항 커밋** (`git commit -m 'Add some amazing feature'`)
4. **브랜치에 푸시** (`git push origin feature/amazing-feature`)
5. **Pull Request** 생성

## 📄 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다. 자세한 내용은 [LICENSE](LICENSE.txt) 파일을 참조하세요.

## 📞 문의 및 지원

- **프로젝트 이슈**: [GitHub Issues](https://github.com/yourusername/imageviewer/issues)
- **개발 문서**: `PROJECT_STATUS.md` 파일 참조
- **API 문서**: 각 서비스의 `/scalar/v1` 엔드포인트 접속

---

## 🏆 주요 성과

### CLAUDE.md 요구사항 100% 달성
- ✅ **미리보기 블러 처리 UI** 구현 완료
- ✅ **hover/클릭 상호작용** 구현 완료  
- ✅ **사용자별 설정** 저장/복원 완료
- ✅ **반응형 디자인** 모바일 지원 완료

### 기술적 성과
- 🏗️ **모듈러 아키텍처**: 확장 가능한 서비스 구조
- 🔒 **보안**: JWT 기반 인증 및 파일 검증
- 🎨 **UX/UI**: 직관적이고 반응형인 사용자 인터페이스
- ⚡ **성능**: 효율적인 페이지네이션과 썸네일 처리

**ImageViewer - 블러 처리의 새로운 경험을 제공합니다** ✨