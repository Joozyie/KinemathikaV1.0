// WHAT IT DOES: Boots charts on Student page (donut + trend + concept bars)
(() => {
    // Student donut from MasteryRate%
    const donut = document.getElementById("studentDonut");
    if (donut) DonutProgress("#studentDonut", +donut.dataset.completed || 0, 100);

    // Student trend
    const trend = document.getElementById("studentTrend");
    const btns = document.querySelectorAll('[data-stu-metric]');
    async function draw(metric) {
        const studentId = trend?.dataset.studentid;
        const r = await fetch(`/api/analytics/student/${encodeURIComponent(studentId)}/trend?metric=${encodeURIComponent(metric)}`);
        const series = await r.json();
        LineChart("#studentTrend", series);
    }
    btns.forEach(b => b.addEventListener("click", () => {
        btns.forEach(x => x.classList.toggle("btn-primary", x === b));
        btns.forEach(x => x.classList.toggle("btn-outline-primary", x !== b));
        draw(b.dataset.stuMetric);
    }));
    if (trend) draw("Attempts");

    // Concept bar
    const bar = document.getElementById("studentConceptBar");
    if (bar) {
        const labels = JSON.parse(bar.dataset.labels || "[]");
        const values = JSON.parse(bar.dataset.values || "[]");
        const items = labels.map((x, i) => ({ x, y: +values[i] || 0 }));
        BarChart("#studentConceptBar", items);
    }
})();
