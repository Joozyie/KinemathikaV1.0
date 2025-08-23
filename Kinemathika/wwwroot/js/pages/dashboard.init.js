// WHAT IT DOES: Boots charts on Dashboard (overall & per-class modes)
(() => {
  // Overview donut (use MasteryRate% from data-* as completed/100)
  const donut = document.getElementById("overallDonut");
  if (donut) {
    const completed = +donut.dataset.completed || 0; // e.g., 62
    DonutProgress("#overallDonut", completed, 100);
  }

  // Overview trend (Attempts/TimeMs)
  const ovTrend = document.getElementById("overviewTrend");
  const ovBtns = document.querySelectorAll('[data-ov-metric]');
  async function drawOv(metric) {
    const r = await fetch(`/api/analytics/overview/trend?metric=${encodeURIComponent(metric)}`);
    const series = await r.json();
    LineChart("#overviewTrend", series);
  }
  ovBtns.forEach(b => b.addEventListener("click", () => {
    ovBtns.forEach(x => x.classList.toggle("btn-primary", x === b));
    ovBtns.forEach(x => x.classList.toggle("btn-outline-primary", x !== b));
    drawOv(b.dataset.ovMetric);
  }));
  if (ovTrend) drawOv("Attempts");

  // Per-class trend (Attempts/TimeMs) — reads classId from data-*
  const clsTrend = document.getElementById("classTrend");
  const clsBtns = document.querySelectorAll('[data-cls-metric]');
  const container = document.getElementById("classTrendWrap");
  async function drawCls(metric) {
    const classId = container?.dataset.classid;
    const conceptId = document.getElementById("conceptSelect")?.value || "";
    if (!classId) return;
    const url = `/api/analytics/class/${classId}/trend?metric=${encodeURIComponent(metric)}&conceptId=${encodeURIComponent(conceptId)}`;
    const r = await fetch(url);
    const series = await r.json();
    LineChart("#classTrend", series);
  }
  clsBtns.forEach(b => b.addEventListener("click", () => {
    clsBtns.forEach(x => x.classList.toggle("btn-primary", x === b));
    clsBtns.forEach(x => x.classList.toggle("btn-outline-primary", x !== b));
    drawCls(b.dataset.clsMetric);
  }));
  document.getElementById("conceptSelect")?.addEventListener("change", () => {
    const active = document.querySelector('[data-cls-metric].btn-primary')?.dataset.clsMetric || "Attempts";
    drawCls(active);
  });
  if (clsTrend) drawCls("Attempts");

  // Concept bars (uses server-provided arrays in data-* to avoid extra calls)
  const conceptBar = document.getElementById("conceptBar");
  if (conceptBar) {
    // expects data-labels='["c1","c2"]' data-values='[60,40]'
    try {
      const labels = JSON.parse(conceptBar.dataset.labels || "[]");
      const values = JSON.parse(conceptBar.dataset.values || "[]");
      const items = labels.map((x, i) => ({ x, y: +values[i] || 0 }));
      BarChart("#conceptBar", items);
    } catch { /* ignore */ }
  }
})();
