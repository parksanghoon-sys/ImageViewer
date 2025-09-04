# ImageViewer Docker 환경 구성 가이드

## 📋 개요

PostgreSQL 기반 프로덕션 환경을 위한 Docker Compose 구성이 완료되었습니다. 이 가이드는 개발 환경과 프로덕션 환경 모두를 다룹니다.

## 🏗️ 아키텍처

### 서비스 구성
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│     Nginx       │    │   API Gateway   │    │    Frontend     │
│   (Reverse      │    │   (Port 8080)   │    │   (Port 3000)   │
│    Proxy)       │    │                 │    │                 │
│  Port 80/443    │    └─────────────────┘    └─────────────────┘
└─────────────────┘            │                        │
        │                      │                        │
        └──────────────────────┼────────────────────────┘
                               │
        ┌─────────────────────────────────────────────┐
        │              마이크로서비스                    │
        ├─────────────┬─────────────┬─────────────────┤
        │ Auth Service│Image Service│ Share Service   │
        │ (Port 8080) │ (Port 8080) │ (Port 8080)     │
        └─────────────┴─────────────┴─────────────────┘
                               │
        ┌─────────────────────────────────────────────┐
        │               인프라스트럭처                   │
        ├─────────────┬─────────────┬─────────────────┤
        │ PostgreSQL  │  RabbitMQ   │     Redis       │
        │ (Port 5432) │ (Port 5672) │   (Port 6379)   │
        └─────────────┴─────────────┴─────────────────┘
                               │
        ┌─────────────────────────────────────────────┐
        │                모니터링                      │
        ├─────────────────────┬───────────────────────┤
        │    Prometheus       │       Grafana         │
        │    (Port 9090)      │     (Port 3001)       │
        └─────────────────────┴───────────────────────┘
```

## 📁 파일 구조

```
ImageViewer/
├── docker-compose.yml              # 개발 환경
├── docker-compose.prod.yml         # 프로덕션 환경
├── .env.development               # 개발 환경 변수
├── .env.production               # 프로덕션 환경 변수
├── configs/                      # 설정 파일들
│   ├── nginx/
│   │   ├── nginx.conf           # Nginx 설정
│   │   └── ssl/                 # SSL 인증서
│   ├── rabbitmq/
│   │   ├── rabbitmq.conf        # RabbitMQ 설정
│   │   ├── definitions.json     # 큐/익스체인지 정의
│   │   └── enabled_plugins      # 활성화된 플러그인
│   ├── redis/redis.conf         # Redis 설정
│   ├── postgres/postgresql.conf  # PostgreSQL 설정
│   └── prometheus/prometheus.yml # Prometheus 설정
├── scripts/                     # 관리 스크립트들
│   ├── start-dev.sh            # 개발 환경 시작
│   ├── start-prod.sh           # 프로덕션 환경 시작
│   ├── stop.sh                 # 개발 환경 종료
│   ├── stop-prod.sh            # 프로덕션 환경 종료
│   ├── backup.sh               # 백업
│   ├── restore.sh              # 복원
│   ├── logs.sh                 # 로그 조회
│   ├── health-check.sh         # 헬스체크
│   └── setup-ssl.sh            # SSL 설정
└── data/                       # 데이터 저장소
    ├── dev/                    # 개발 환경 데이터
    └── prod/                   # 프로덕션 환경 데이터
```

## 🚀 빠른 시작

### 개발 환경

```bash
# 1. 개발 환경 시작
./scripts/start-dev.sh

# 2. 서비스 상태 확인
./scripts/health-check.sh

# 3. 로그 확인
./scripts/logs.sh

# 4. 환경 종료
./scripts/stop.sh
```

### 프로덕션 환경

```bash
# 1. 환경 변수 설정
cp .env.production .env.production.local
# .env.production.local 파일을 편집하여 실제 값 설정

# 2. SSL 인증서 설정 (HTTPS 사용시)
./scripts/setup-ssl.sh yourdomain.com

# 3. 프로덕션 환경 시작
./scripts/start-prod.sh

# 4. 헬스체크
./scripts/health-check.sh docker-compose.prod.yml .env.production

# 5. 백업 설정 (cron 등록)
# 매일 새벽 2시 자동 백업
# 0 2 * * * /path/to/imageviewer/scripts/backup.sh
```

## ⚙️ 환경별 설정

### 개발 환경 (.env.development)
- 간단한 비밀번호 사용
- 디버그 모드 활성화
- CORS 모든 도메인 허용
- Swagger UI 활성화

### 프로덕션 환경 (.env.production)
- 강력한 비밀번호 필수
- 프로덕션 모드
- 보안 헤더 활성화
- SSL/TLS 설정

## 🔧 주요 스크립트 사용법

### 1. 로그 조회
```bash
# 개발환경 전체 로그
./scripts/logs.sh

# 프로덕션 특정 서비스 로그
./scripts/logs.sh -p api-gateway

# 실시간 로그 추적
./scripts/logs.sh -f postgres

# 최근 50줄만 조회
./scripts/logs.sh -t 50 auth-service
```

### 2. 헬스체크
```bash
# 개발환경 헬스체크
./scripts/health-check.sh

# 프로덕션 환경 헬스체크
./scripts/health-check.sh docker-compose.prod.yml .env.production
```

### 3. 백업 및 복원
```bash
# 프로덕션 백업
./scripts/backup.sh docker-compose.prod.yml .env.production

# 개발환경 백업
./scripts/backup.sh

# 백업 복원
./scripts/restore.sh 20241227_143000
```

### 4. SSL 설정
```bash
# 자체 서명 인증서 (개발/테스트)
./scripts/setup-ssl.sh localhost

# Let's Encrypt (프로덕션)
./scripts/setup-ssl.sh yourdomain.com
```

## 🔍 모니터링

### Prometheus 메트릭
- **애플리케이션 메트릭**: 각 마이크로서비스의 성능 지표
- **인프라 메트릭**: PostgreSQL, RabbitMQ, Redis 상태
- **시스템 메트릭**: CPU, 메모리, 디스크, 네트워크

### Grafana 대시보드
- **시스템 개요**: 전체 서비스 상태 한눈에 보기
- **데이터베이스**: PostgreSQL 성능 및 쿼리 분석
- **메시지 큐**: RabbitMQ 처리량 및 대기열 상태
- **애플리케이션**: 서비스별 응답시간, 에러율

### 접속 정보
- **Grafana**: http://localhost:3001 (admin / 환경변수에서 설정한 비밀번호)
- **Prometheus**: http://localhost:9090
- **RabbitMQ 관리**: http://localhost:15672

## 🔒 보안 고려사항

### 프로덕션 환경 체크리스트
- [ ] 강력한 비밀번호 설정 (최소 16자, 특수문자 포함)
- [ ] SSL/TLS 인증서 설정
- [ ] 방화벽 설정 (필요한 포트만 오픈)
- [ ] 정기적인 보안 업데이트
- [ ] 백업 및 복원 테스트
- [ ] 로그 모니터링 설정
- [ ] 접근 로그 분석

### 보안 설정
```bash
# 1. 파일 권한 설정
chmod 600 .env.production
chmod 600 configs/nginx/ssl/*

# 2. 방화벽 설정 (예시)
ufw allow 80
ufw allow 443
ufw deny 5432  # DB 포트 외부 차단
ufw deny 15672 # RabbitMQ 관리 포트 차단

# 3. SSL 인증서 자동 갱신 (Let's Encrypt)
crontab -e
# 0 3 * * * /path/to/scripts/setup-ssl.sh yourdomain.com
```

## 🐛 트러블슈팅

### 일반적인 문제들

#### 1. 컨테이너 시작 실패
```bash
# 로그 확인
docker-compose logs 서비스명

# 컨테이너 상태 확인
docker-compose ps

# 포트 충돌 확인
netstat -tulpn | grep :포트번호
```

#### 2. 데이터베이스 연결 실패
```bash
# PostgreSQL 컨테이너 로그 확인
docker-compose logs postgres

# 데이터베이스 연결 테스트
docker-compose exec postgres psql -U imageviewer_user -d ImageViewerDB
```

#### 3. RabbitMQ 연결 문제
```bash
# RabbitMQ 로그 확인
docker-compose logs rabbitmq

# 관리 UI 접속
# http://localhost:15672
```

#### 4. SSL 인증서 문제
```bash
# 인증서 유효성 확인
openssl x509 -in configs/nginx/ssl/cert.pem -text -noout

# Nginx 설정 테스트
docker-compose exec nginx nginx -t
```

### 성능 최적화

#### 1. 메모리 사용량 최적화
```yaml
# docker-compose.prod.yml에서 리소스 제한 설정
deploy:
  resources:
    limits:
      memory: 512m
      cpus: '0.5'
```

#### 2. 데이터베이스 튜닝
- `configs/postgres/postgresql.conf` 파일 수정
- 메모리 설정 조정 (shared_buffers, work_mem)
- 연결 풀링 설정

#### 3. Redis 캐싱 활용
- 세션 캐싱
- API 응답 캐싱
- 데이터베이스 쿼리 결과 캐싱

## 📊 백업 전략

### 자동 백업 설정
```bash
# crontab 등록
crontab -e

# 매일 새벽 2시 백업
0 2 * * * /path/to/imageviewer/scripts/backup.sh docker-compose.prod.yml .env.production

# 주간 백업 (매주 일요일)
0 1 * * 0 /path/to/imageviewer/scripts/backup.sh docker-compose.prod.yml .env.production
```

### 백업 보관 정책
- **일간 백업**: 30일 보관
- **주간 백업**: 12주 보관
- **월간 백업**: 12개월 보관

## 📞 지원 및 문의

문제가 발생하거나 추가 설정이 필요한 경우:

1. **로그 확인**: `./scripts/logs.sh` 실행
2. **헬스체크**: `./scripts/health-check.sh` 실행
3. **이슈 리포팅**: 로그와 함께 상세한 에러 내용 제공

---

**🎉 Docker 환경 구성이 완료되었습니다! 안전하고 확장 가능한 ImageViewer 서비스를 운영하세요.**