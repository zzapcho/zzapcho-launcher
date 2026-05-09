# 서버 게이트 프로토콜

`ZzapchoGate` Paper 플러그인은 런처를 쓰지 않은 접속을 서버에서 막는 역할을 한다.

## 원칙

- 런처 단독 검사는 신뢰하지 않는다.
- 실제 강제는 Paper 서버에서 한다.
- 런처에는 서버 비밀키를 넣지 않는다.

## 흐름

1. 런처에서 Microsoft 로그인
2. manifest 검증
3. 파일 SHA-256 검증
4. 백엔드에 permit 요청
5. 백엔드는 UUID 기준 120초짜리 permit 생성
6. Minecraft 실행
7. Paper 플러그인이 UUID permit 확인
8. 접속 후 15초 안에 handshake 확인

## 킥 메시지

```text
잡초 약탈서버 런처로 접속해주세요.
```
