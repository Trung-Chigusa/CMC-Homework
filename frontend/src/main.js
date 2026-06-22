import "./styles.css";

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:8080";

const app = document.querySelector("#app");

let assets = [];
let selectedResult = null;

app.innerHTML = `
  <div class="app-shell">
    <header class="topbar">
      <div class="brand">
        <div class="brand-mark">E</div>
        <div>
          <h1>CMC EASM Dashboard</h1>
          <div class="status-line" id="health">Backend: checking</div>
        </div>
      </div>
      <button id="refresh" title="Refresh data">Refresh</button>
    </header>

    <main class="layout">
      <aside class="stack">
        <section class="panel">
          <div class="panel-header"><h2>Create Asset</h2></div>
          <div class="panel-body">
            <form id="asset-form" class="stack">
              <div class="field">
                <label for="asset-name">Name</label>
                <input id="asset-name" name="name" placeholder="example.com" required />
              </div>
              <div class="field">
                <label for="asset-type">Type</label>
                <select id="asset-type" name="type">
                  <option value="domain">domain</option>
                  <option value="ip">ip</option>
                  <option value="service">service</option>
                </select>
              </div>
              <div class="field">
                <label for="asset-status">Status</label>
                <select id="asset-status" name="status">
                  <option value="active">active</option>
                  <option value="inactive">inactive</option>
                </select>
              </div>
              <button class="primary" type="submit">Create</button>
              <div class="message" id="form-message"></div>
            </form>
          </div>
        </section>

        <section class="panel">
          <div class="panel-header"><h2>Stats</h2></div>
          <div class="panel-body stats-grid" id="stats"></div>
        </section>
      </aside>

      <section class="stack">
        <section class="panel">
          <div class="panel-header">
            <h2>Assets</h2>
            <span class="badge" id="asset-count">0 total</span>
          </div>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th>ID</th>
                  <th>Scan</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody id="asset-rows"></tbody>
            </table>
          </div>
        </section>

        <section class="panel">
          <div class="panel-header"><h2>Scan Results</h2></div>
          <div class="panel-body">
            <pre class="result-view" id="results">${formatJson({ message: "No scan selected" })}</pre>
          </div>
        </section>
      </section>
    </main>
  </div>
`;

document.querySelector("#refresh").addEventListener("click", loadAll);
document.querySelector("#asset-form").addEventListener("submit", createAsset);

loadAll();

async function loadAll() {
  await Promise.all([loadHealth(), loadAssets(), loadStats()]);
}

async function loadHealth() {
  try {
    const health = await api("/health");
    document.querySelector("#health").textContent = `Backend: ${health.status} | storage: ${health.storage.type} | uptime: ${health.uptime_seconds}s`;
  } catch (error) {
    document.querySelector("#health").textContent = `Backend: ${error.message}`;
  }
}

async function loadAssets() {
  const response = await api("/assets?limit=100");
  assets = response.data || [];
  document.querySelector("#asset-count").textContent = `${assets.length} total`;
  renderAssets();
}

async function loadStats() {
  const stats = await api("/assets/stats");
  document.querySelector("#stats").innerHTML = `
    ${statCard(stats.total, "assets")}
    ${statCard(stats.by_type?.domain ?? 0, "domains")}
    ${statCard(stats.by_type?.ip ?? 0, "IPs")}
  `;
}

function renderAssets() {
  const rows = document.querySelector("#asset-rows");
  rows.innerHTML = assets.map(asset => `
    <tr>
      <td>${escapeHtml(asset.name)}</td>
      <td><span class="badge">${escapeHtml(asset.type)}</span></td>
      <td><span class="badge ${escapeHtml(asset.status)}">${escapeHtml(asset.status)}</span></td>
      <td class="mono">${escapeHtml(asset.id)}</td>
      <td>
        <div class="actions">
          <select data-scan-type="${asset.id}">
            ${scanOptions(asset.type).map(type => `<option value="${type}">${type}</option>`).join("")}
          </select>
          <button class="primary" data-scan="${asset.id}">Start</button>
        </div>
      </td>
      <td>
        <div class="actions">
          <button data-results="${asset.id}">Results</button>
          <button class="danger" data-delete="${asset.id}">Delete</button>
        </div>
      </td>
    </tr>
  `).join("");

  rows.querySelectorAll("[data-scan]").forEach(button => {
    button.addEventListener("click", () => startScan(button.dataset.scan));
  });

  rows.querySelectorAll("[data-results]").forEach(button => {
    button.addEventListener("click", () => loadAssetResults(button.dataset.results));
  });

  rows.querySelectorAll("[data-delete]").forEach(button => {
    button.addEventListener("click", () => deleteAsset(button.dataset.delete));
  });
}

async function createAsset(event) {
  event.preventDefault();
  const formElement = event.currentTarget;
  const form = new FormData(formElement);
  const message = document.querySelector("#form-message");

  try {
    const asset = await api("/assets", {
      method: "POST",
      body: {
        name: form.get("name"),
        type: form.get("type"),
        status: form.get("status")
      }
    });
    formElement.reset();
    message.className = "message";
    message.textContent = `Created ${asset.name}`;
    await loadAll();
  } catch (error) {
    message.className = "message error";
    message.textContent = error.message;
  }
}

async function deleteAsset(id) {
  await api(`/assets/batch?ids=${encodeURIComponent(id)}`, { method: "DELETE" });
  if (selectedResult === id) {
    selectedResult = null;
    document.querySelector("#results").textContent = formatJson({ message: "No scan selected" });
  }
  await loadAll();
}

async function startScan(assetId) {
  const select = document.querySelector(`[data-scan-type="${assetId}"]`);
  const job = await api(`/assets/${assetId}/scan`, {
    method: "POST",
    body: { scan_type: select.value }
  });

  document.querySelector("#results").textContent = formatJson(job);
  await waitForJob(job.id);
  await loadAssetResults(assetId);
}

async function waitForJob(jobId) {
  for (let attempt = 0; attempt < 10; attempt++) {
    const job = await api(`/scan-jobs/${jobId}`);
    if (!["pending", "running"].includes(job.status)) {
      return job;
    }
    await new Promise(resolve => setTimeout(resolve, 700));
  }
}

async function loadAssetResults(assetId) {
  selectedResult = assetId;
  const result = await api(`/assets/${assetId}/results`);
  document.querySelector("#results").textContent = formatJson(result);
}

async function api(path, options = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    method: options.method || "GET",
    headers: options.body ? { "Content-Type": "application/json" } : undefined,
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  if (!response.ok) {
    throw new Error(data?.message || `HTTP ${response.status}`);
  }

  return data;
}

function scanOptions(assetType) {
  if (assetType === "ip") {
    return ["ip", "asn", "port"];
  }

  if (assetType === "service") {
    return ["tech"];
  }

  return ["dns", "whois", "subdomain", "cert_trans", "ssl", "tech", "all"];
}

function statCard(value, label) {
  return `<div class="stat"><div class="stat-value">${value}</div><div class="stat-label">${label}</div></div>`;
}

function formatJson(value) {
  return JSON.stringify(value, null, 2);
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
