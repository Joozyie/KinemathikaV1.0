// WHAT IT DOES: Shared D3 helpers (SVG scaffold, date parsing)
window.D3Base = (() => {
    function makeSvg(sel, w = 700, h = 300, m = { top: 20, right: 20, bottom: 30, left: 40 }) {
        const root = d3.select(sel); root.selectAll("*").remove();
        const svg = root.append("svg").attr("width", w).attr("height", h);
        const g = svg.append("g").attr("transform", `translate(${m.left},${m.top})`);
        return { svg, g, w: w - m.left - m.right, h: h - m.top - m.bottom };
    }
    const parseDate = d => (d instanceof Date) ? d : new Date(d);
    return { makeSvg, parseDate };
})();
