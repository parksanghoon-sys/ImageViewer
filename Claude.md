좋습니다 👍

요구사항이 꽤 복잡하니, **아키텍처 설계 → 기능 모듈 분리 → 구현 순서 → 테스트 전략 → 배포 계획**까지 1개월 안에 끝낼 수 있는 **실전형 개발 계획**으로 짜드리겠습니다.

---

## **1. 전체 개발 계획**

### 1.1 목표

1개월 안에 **OOP 기반, 유지보수/확장성 좋은 구조**로

**모듈러 모놀리식** + **RabbitMQ**를 적용한 이미지 뷰어 웹 서비스 완성.

핵심 기능은  **회원 인증** ,  **이미지 업로드/조회/승인 공유** ,  **미리보기 블러 처리 UI** .

주석은 자세하게

---

## **2. 아키텍처 설계**

### 2.1 백엔드 구조 (C#, .NET 8)

* **모듈러 모놀리식**으로 시작 (1달 안에 마이크로서비스로 쪼갤 수 있게 설계)
* **도메인별 프로젝트 분리**
  * `AuthService` (회원 가입, 로그인, 토큰 발급/갱신)
  * `ImageService` (이미지 업로드/다운로드/미리보기 생성)
  * `ShareService` (회원 간 이미지 공유 요청/승인)
  * `NotificationService` (RabbitMQ 기반 알림)
* **공유 라이브러리**
  * `Domain` (Entity, Value Object, Aggregate Root)
  * `Application` (UseCase, Service Interface)
  * `Infrastructure` (EF Core, Storage, MessageBus)
  * `Contracts` (DTO, Command, Event)
* **통신**
  * REST API (주요 기능)
  * RabbitMQ Event (이미지 업로드 → 미리보기 생성, 공유 승인 알림)

---

### 2.2 프론트엔드 구조 (React + Tailwind + Axios)

* **반응형 디자인** (휴대폰 대응)
* 주요 페이지
  1. 로그인/회원가입
  2. 내 이미지 목록 (미리보기, 블러 처리)
  3. 이미지 업로드 페이지
  4. 이미지 공유 요청/승인 페이지
  5. 타 회원 이미지 보기
* **UI 특징**
  * 미리보기 크기 및 개수 설정 가능
  * 블러 처리 후 hover/클릭 시 선명하게 표시
* 상태 관리 → **Redux Toolkit** or **React Query**
* 인증 → **JWT + Refresh Token 재발급 자동 처리**

---

## **3. 기능별 상세 계획**

### 3.1 인증 서비스

* [ ] 회원 가입 API (`POST /api/auth/register`)
* [ ] 로그인 API (`POST /api/auth/login`)
* [ ] 토큰 발급 (Access + Refresh)
* [ ] Refresh Token Rotation 적용
* [ ] Serilog 로깅 + Swagger JWT 인증 테스트

---

### 3.2 이미지 서비스

* [ ] 이미지 업로드 (회원 단위 디렉토리 구조)
* [ ] 업로드 시 썸네일(미리보기) 생성 (RabbitMQ 비동기 처리)
* [ ] 업로드 이미지 목록 조회
* [ ] 미리보기 개수 설정 저장/불러오기

---

### 3.3 공유 서비스

* [ ] 이미지 공유 요청 (`POST /api/share/request`)
* [ ] 요청 승인/거절
* [ ] 승인된 회원의 이미지 조회 가능
* [ ] RabbitMQ로 알림 이벤트 발행 → 프론트에 SignalR로 푸시

---

### 3.4 프론트엔드 UI

* [ ] 로그인/회원가입 화면
* [ ] 이미지 목록 UI (미리보기 블러 처리)
* [ ] 업로드 페이지 (Drag&Drop 지원)
* [ ] 공유 요청/승인 페이지
* [ ] 반응형 모바일 뷰

---

## **4. 구현 순서 (4주)**

| 주차            | 목표                    | 세부 작업                                                                                   |
| --------------- | ----------------------- | ------------------------------------------------------------------------------------------- |
| **1주차** | 백엔드 기반 구축 + 인증 | .NET 솔루션 구조 생성모듈별 프로젝트 분리AuthService 구현EF Core DB 설계Swagger 연동        |
| **2주차** | 이미지 업로드/미리보기  | ImageService 구현파일 저장 + 썸네일 생성RabbitMQ 연결미리보기 블러 처리 API                 |
| **3주차** | 공유 기능 + 알림        | ShareService 구현RabbitMQ 이벤트 → SignalR 푸시권한 검사 로직 추가프론트 공유 요청/승인 UI |
| **4주차** | 통합/테스트/배포        | Unit/E2E 테스트 작성버그 수정Docker Compose 배포최종 리팩토링                               |

---

## **5. 테스트 전략**

* **단위 테스트** :
* xUnit + Moq 사용
* 서비스 레이어, 도메인 로직 80% 커버리지 목표
* **통합 테스트** :
* Testcontainers로 RabbitMQ/DB 컨테이너 띄워서 테스트
* **프론트엔드 테스트** :
* React Testing Library + Cypress (E2E)

---

## **6. 배포**

* **Docker Compose**
  * API 서버
  * RabbitMQ
  * PostgreSQL (또는 SQL Server)
  * React 프론트엔드
* 추후 **Kubernetes**로 확장 가능하도록 설계

---

## **7. 기술 스택**

* Backend: `.NET 8`, EF Core, RabbitMQ, Serilog, xUnit, Swagger
* Frontend: `React 18`, TailwindCSS, Axios, Redux Toolkit, React Query
* DB: PostgreSQL (or MSSQL)
* Infra: Docker, RabbitMQ, Nginx (프론트엔드 서빙)

---
