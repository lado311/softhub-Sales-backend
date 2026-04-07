# SoftHub CRM — Full Stack Setup Guide

## Stack

| Layer      | Technology                        | Hosting          | Cost        |
|------------|-----------------------------------|------------------|-------------|
| Frontend   | React 18 + Axios                  | Vercel           | Free        |
| Backend    | ASP.NET Core 8 Web API            | Railway          | ~$5/month   |
| Database   | PostgreSQL 16                     | Neon.tech        | Free (3GB)  |

---

## Default Login
```
Email:    admin@softhub.io
Password: Admin123!
```
Change the password immediately after first login via Settings → My Account.

---

## Step 1 — Database (Neon.tech, FREE)

1. Go to https://neon.tech and create a free account
2. Create a new project called `softhub`
3. Copy the **connection string** — it looks like:
   ```
   postgresql://username:password@ep-xxx.us-east-1.aws.neon.tech/softhub?sslmode=require
   ```
4. Convert it to the .NET format:
   ```
   Host=ep-xxx.us-east-1.aws.neon.tech;Database=softhub;Username=username;Password=password;SSL Mode=Require;Trust Server Certificate=true
   ```

---

## Step 2 — Backend (Local Development)

### Prerequisites
- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0

### Run locally

```bash
cd softhub-backend/SoftHub.API

# Edit appsettings.Development.json with your Neon connection string
# OR use a local PostgreSQL instance

# Run migrations (creates all tables + seeds admin user)
dotnet ef database update

# Start the API
dotnet run
# → API running at http://localhost:5000
# → Swagger UI at http://localhost:5000/swagger
```

### appsettings.Development.json (local dev)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=softhub_dev;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-dev-secret-min-32-chars-here!!",
    "Issuer": "SoftHub.API",
    "Audience": "SoftHub.Client",
    "AccessTokenMinutes": "60",
    "RefreshTokenDays": "30"
  },
  "AllowedOrigins": ["http://localhost:3000"]
}
```

### Generate a secure JWT secret
```bash
# On Linux/Mac:
openssl rand -base64 48

# On Windows PowerShell:
[System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

---

## Step 3 — Frontend (Local Development)

```bash
cd softhub-frontend

# Create .env.development
echo "REACT_APP_API_URL=http://localhost:5000" > .env.development

npm install
npm start
# → Frontend at http://localhost:3000
```

---

## Step 4 — Deploy Backend to Railway

1. Push your code to GitHub:
   ```bash
   cd softhub-backend
   git init
   git add .
   git commit -m "Initial commit"
   git remote add origin https://github.com/YOUR_USERNAME/softhub-backend.git
   git push -u origin main
   ```

2. Go to https://railway.app → New Project → Deploy from GitHub repo
3. Select your `softhub-backend` repo
4. Railway auto-detects the Dockerfile ✓

5. Set **Environment Variables** in Railway dashboard:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ConnectionStrings__DefaultConnection=Host=ep-xxx.neon.tech;Database=softhub;Username=user;Password=pass;SSL Mode=Require;Trust Server Certificate=true
   Jwt__Secret=YOUR_GENERATED_SECRET_MIN_32_CHARS
   Jwt__Issuer=SoftHub.API
   Jwt__Audience=SoftHub.Client
   Jwt__AccessTokenMinutes=60
   Jwt__RefreshTokenDays=30
   AllowedOrigins__0=https://your-frontend.vercel.app
   ```

6. Click **Deploy** — Railway builds the Docker image and runs it
7. Copy the Railway URL: `https://softhub-backend-xxx.railway.app`

> The app runs `db.Database.Migrate()` on startup — tables are created automatically.

---

## Step 5 — Deploy Frontend to Vercel

1. Push frontend to GitHub:
   ```bash
   cd softhub-frontend
   git init
   git add .
   git commit -m "Initial commit"
   git remote add origin https://github.com/YOUR_USERNAME/softhub-frontend.git
   git push -u origin main
   ```

2. Go to https://vercel.com → New Project → Import from GitHub
3. Select `softhub-frontend`
4. Set **Environment Variable**:
   ```
   REACT_APP_API_URL = https://softhub-backend-xxx.railway.app
   ```
5. Click Deploy → copy your Vercel URL

6. Go back to Railway → update `AllowedOrigins__0` to your Vercel URL
7. Redeploy Railway

---

## Step 6 — Verify Everything Works

```bash
# Test the API health check
curl https://softhub-backend-xxx.railway.app/health

# Test login
curl -X POST https://softhub-backend-xxx.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@softhub.io","password":"Admin123!"}'
```

Open your Vercel URL → login with `admin@softhub.io` / `Admin123!`

---

## API Reference

All endpoints require `Authorization: Bearer <token>` except `/api/auth/login`.

| Method | Endpoint                        | Description                  | Auth     |
|--------|---------------------------------|------------------------------|----------|
| POST   | `/api/auth/login`               | Login → returns JWT tokens   | Public   |
| POST   | `/api/auth/refresh`             | Refresh access token         | Public   |
| POST   | `/api/auth/logout`              | Revoke refresh token         | Required |
| GET    | `/api/auth/me`                  | Current user profile         | Required |
| PUT    | `/api/auth/me/password`         | Change own password          | Required |
| POST   | `/api/auth/register`            | Create new user              | Admin    |
| GET    | `/api/leads`                    | List leads (paginated+filter)| Required |
| POST   | `/api/leads`                    | Create lead                  | Required |
| GET    | `/api/leads/{id}`               | Get lead details             | Required |
| PUT    | `/api/leads/{id}`               | Update lead                  | Required |
| PATCH  | `/api/leads/{id}/move`          | Move to pipeline stage       | Required |
| DELETE | `/api/leads/{id}`               | Delete lead                  | Required |
| POST   | `/api/leads/bulk`               | Bulk status/assign update    | Required |
| POST   | `/api/leads/{id}/notes`         | Add note to lead             | Required |
| DELETE | `/api/leads/{leadId}/notes/{id}`| Delete note                  | Required |
| GET    | `/api/users`                    | List all users               | Required |
| PUT    | `/api/users/{id}`               | Update user                  | Admin    |
| DELETE | `/api/users/{id}`               | Delete user                  | Admin    |
| GET    | `/api/dashboard/stats`          | KPI stats                    | Required |
| GET    | `/api/dashboard/followups`      | Follow-up buckets            | Required |

Full interactive docs at: `https://your-backend.railway.app/swagger`

---

## Lead Query Parameters

```
GET /api/leads?search=acme&status=Negotiation&industry=Retail
              &assignedToId=2&dateFrom=2025-01-01&dateTo=2025-12-31
              &sortBy=potentialValue&sortDir=desc&page=1&pageSize=50
```

---

## Adding Migrations (when you change models)

```bash
cd softhub-backend/SoftHub.API
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

---

## Environment Variables Summary

### Backend (Railway)
| Variable | Example Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=ep-xxx.neon.tech;...` |
| `Jwt__Secret` | 48+ char random string |
| `Jwt__Issuer` | `SoftHub.API` |
| `Jwt__Audience` | `SoftHub.Client` |
| `Jwt__AccessTokenMinutes` | `60` |
| `Jwt__RefreshTokenDays` | `30` |
| `AllowedOrigins__0` | `https://your-app.vercel.app` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

### Frontend (Vercel)
| Variable | Example Value |
|---|---|
| `REACT_APP_API_URL` | `https://your-backend.railway.app` |

---

## Monthly Cost Estimate

| Service | Plan | Cost |
|---|---|---|
| Neon.tech | Free tier (3GB) | $0 |
| Railway | Starter (500 hrs/month) | ~$5 |
| Vercel | Hobby (free) | $0 |
| **Total** | | **~$5/month** |

For production with more users, upgrade Railway to Pro ($20/month) and Neon to Launch ($19/month).
