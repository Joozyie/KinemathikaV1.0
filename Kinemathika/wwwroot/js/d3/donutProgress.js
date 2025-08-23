// WHAT IT DOES: Circular progress donut (completed/total → %)
window.DonutProgress = (sel, completed, total) => {
    const size = 220, thick = 30;
    const root = D3Base.makeSvg(sel, size, size, { top: 0, right: 0, bottom: 0, left: 0 });
    const g = root.svg.append("g").attr("transform", `translate(${size / 2},${size / 2})`);
    const pct = total > 0 ? Math.max(0, Math.min(1, completed / total)) : 0;
    const arc = d3.arc().innerRadius((size / 2) - thick).outerRadius(size / 2).startAngle(0);
    g.append("path").datum({ endAngle: 2 * Math.PI }).attr("class", "bg").attr("d", arc);
    g.append("path").datum({ endAngle: 2 * Math.PI * pct }).attr("class", "fg").attr("d", arc);
    g.append("text").attr("text-anchor", "middle").attr("dy", "0.35em")
        .text(`${(pct * 100).toFixed(1)}%`);
};
