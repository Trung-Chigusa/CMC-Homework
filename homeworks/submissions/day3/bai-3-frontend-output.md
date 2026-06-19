# Bài 3 - Frontend Output

## Implemented

- Dashboard stats.
- Asset list.
- Create asset form.
- Delete asset action.
- Start scan action.
- Scan results viewer.
- CORS configured in backend.
- Frontend API URL configured with `VITE_API_URL`.

## Build

```text
npm run build

vite v8.0.16 building client environment for production...
✓ built
```

## Browser verification

Frontend was verified in the in-app browser at `http://localhost:5173` with backend at `http://localhost:18080`.

```json
{
  "title": "CMC EASM Dashboard",
  "h1": "CMC EASM Dashboard",
  "health": "Backend: ok | storage: sqlite",
  "row_count_after_create": 4,
  "scan_result_status": "completed"
}
```
