// WHAT IT DOES: Simple vertical bar chart (category X, number Y)
window.BarChart = (sel, items) => {
    const { g, w, h } = D3Base.makeSvg(sel);
    const x = d3.scaleBand().domain(items.map(d => d.x)).range([0, w]).padding(0.2);
    const y = d3.scaleLinear().domain([0, d3.max(items, d => d.y) || 1]).range([h, 0]).nice();
    g.append("g").attr("transform", `translate(0,${h})`).call(d3.axisBottom(x));
    g.append("g").call(d3.axisLeft(y));
    g.selectAll("rect").data(items).enter().append("rect")
        .attr("x", d => x(d.x)).attr("y", d => y(d.y))
        .attr("width", x.bandwidth()).attr("height", d => h - y(d.y));
};
