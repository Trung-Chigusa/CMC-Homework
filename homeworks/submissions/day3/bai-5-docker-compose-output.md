# Bài 5 - Docker Compose Output

Files:

- `src/CmcHomework.Api/Dockerfile`
- `frontend/Dockerfile`
- `docker-compose.yml`

Config validation:

```text
docker compose config

services:
  backend:
    ports:
      - "8080:8080"
  frontend:
    ports:
      - "3000:80"
```

Docker image build was not executed successfully on this machine because Docker Desktop/Linux engine was not running:

```text
open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified.
```
