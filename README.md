# GameServer API
ASP.NET Core 8 | Onion Architecture | SQL Server | JWT | SMTP

## Baslangic

### 1. appsettings.json duzenle
- Jwt:Secret -> Min 32 karakter bir key yaz
- ConnectionStrings:DefaultConnection -> SQL Server connection string
- Smtp -> Gmail bilgilerin

### 2. Migration olustur
cd src/GameServer.API
dotnet ef migrations add InitialCreate --project ../GameServer.Infrastructure

### 3. Calistir
dotnet run
Swagger: http://localhost:5000/swagger

## Endpointler

### Auth
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh-token
POST   /api/auth/logout
POST   /api/auth/forgot-password
POST   /api/auth/reset-password
GET    /api/auth/verify-email?token=...
POST   /api/auth/resend-verification
POST   /api/auth/change-password

### Player
GET    /api/player/me
PUT    /api/player/me
GET    /api/player/me/stats
GET    /api/player/me/achievements
GET    /api/player/{id}
GET    /api/player/{id}/stats
GET    /api/player/leaderboard

### Game
GET    /api/game
GET    /api/game/sessions
POST   /api/game/sessions
GET    /api/game/sessions/{id}
POST   /api/game/sessions/join/{code}
POST   /api/game/sessions/{id}/leave
POST   /api/game/sessions/{id}/start
POST   /api/game/sessions/{id}/end   [Admin]
GET    /api/game/history
GET    /api/game/history/{userId}

### Admin (Rol: Admin, SuperAdmin)
GET    /api/admin/dashboard
GET    /api/admin/users
GET    /api/admin/users/{id}
POST   /api/admin/users/{id}/ban
POST   /api/admin/users/{id}/unban
PUT    /api/admin/users/{id}/role    [SuperAdmin]
POST   /api/admin/users/credit
GET    /api/admin/games
POST   /api/admin/games
PUT    /api/admin/games/{id}
DELETE /api/admin/games/{id}
POST   /api/admin/notifications
GET    /api/admin/audit-logs
GET    /api/admin/settings
PUT    /api/admin/settings/{key}

### Notification
GET    /api/notification
POST   /api/notification/{id}/read
POST   /api/notification/read-all
