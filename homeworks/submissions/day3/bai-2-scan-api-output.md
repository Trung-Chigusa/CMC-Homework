# Bài 2 - Scan API Output

## Implemented endpoints

- `POST /assets/{id}/scan`
- `GET /scan-jobs/{id}`
- `GET /scan-jobs/{id}/results`
- `GET /assets/{id}/scans`
- `GET /assets/{id}/results`
- `GET /assets/{id}/dns`
- `GET /assets/{id}/whois`
- `GET /assets/{id}/subdomains`

## Implemented scan types

- Existing/passive: `dns`, `whois`, `subdomain`, `cert_trans`, `asn`, `all`
- New: `ip`, `port`, `ssl`, `tech`

## Runtime verification

Backend was verified on `http://localhost:18080` because port `8080` was already used by another local process.

```json
{
  "health_status": "ok",
  "storage_type": "sqlite",
  "domain_asset_id": "79c5ee86-22f8-40dd-be2d-58b7c2798eb9",
  "ip_asset_id": "d529482b-934e-4655-8776-d6f41a7f5c1e",
  "ip_job_id": "ac0c1b82-254d-4a21-81c6-159ce810468b",
  "dns_job_id": "82a828d0-5806-48d5-9285-fb0752860eaf",
  "ip_result_count": 1,
  "asset_scan_count": 1,
  "total_assets": 2
}
```

Port scan safety:

```text
Public IP port scan is rejected by validation.
Allowed targets: localhost/private IP ranges only.
```
