// WHAT IT DOES: Lightweight time-series line chart (date X, number Y)
window.LineChart = (sel, series) => {
    const { g, w, h } = D3Base.makeSvg(sel);
    const data = (series.points || []).map(p => ({ x: D3Base.parseDate(p.x), y: +p.y }));
    const x = d3.scaleTime().range([0, w]).domain(d3.extent(data, d => d.x) || [new Date(), new Date()]);
    const y = d3.scaleLinear().range([h, 0]).domain([0, d3.max(data, d => d.y) || 1]).nice();
    const line = d3.line().x(d => x(d.x)).y(d => y(d.y));
    g.append("g").attr("transform", `translate(0,${h})`).call(d3.axisBottom(x));
    g.append("g").call(d3.axisLeft(y));
    g.append("path").datum(data).attr("fill", "none").attr("stroke-width", 2).attr("d", line);
};
