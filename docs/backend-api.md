# 백엔드 API

## 입장 허가

```http
POST https://api.zzapcho.kr/launcher/permit
```

요청:

```json
{
  "playerUuid": "...",
  "playerName": "...",
  "launcherVersion": "1.0.0",
  "manifestVersion": "2026.05.09-001",
  "manifestHash": "...",
  "fileSetHash": "...",
  "timestamp": 1778300000,
  "clientNonce": "random-value"
}
```

응답:

```json
{
  "allowed": true,
  "expiresAt": 1778300120
}
```

## 크래시 리포트

```http
POST https://api.zzapcho.kr/crash-report
```

클라이언트는 GitHub 토큰을 갖지 않는다.
GitHub 이슈 생성은 백엔드가 나중에 처리한다.
