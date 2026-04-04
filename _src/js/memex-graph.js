// AI Memex - Filter + Knowledge Graph Visualization
// Filter: wires up .ai-memex-filter-btn buttons to show/hide list items
// Graph: D3.js force-directed layout loaded from CDN

// --- Filter Logic ---
function initMemexFilters() {
  var buttons = document.querySelectorAll('.ai-memex-filter-btn[data-filter]');
  if (!buttons.length) return;

  buttons.forEach(function(btn) {
    btn.addEventListener('click', function() {
      var filter = btn.getAttribute('data-filter');
      
      // Toggle active class
      buttons.forEach(function(b) { b.classList.remove('active'); });
      btn.classList.add('active');
      
      // Filter list items
      var items = document.querySelectorAll('.ai-memex-list li');
      items.forEach(function(item) {
        if (filter === 'all' || item.getAttribute('data-entry-type') === filter) {
          item.style.display = '';
        } else {
          item.style.display = 'none';
        }
      });
    });
  });
}

// --- Graph Logic ---

function loadMemexGraph(container) {
  // Load D3.js from CDN if not already loaded
  if (typeof d3 === 'undefined') {
    var script = document.createElement('script');
    script.src = 'https://d3js.org/d3.v7.min.js';
    script.onload = function() { initGraph(container); };
    document.head.appendChild(script);
  } else {
    initGraph(container);
  }
}

function initGraph(container) {
  container.innerHTML = '';
  container.dataset.loaded = 'true';
  
  fetch('/resources/ai-memex/graph.json')
    .then(function(r) { return r.json(); })
    .then(function(data) { renderGraph(container, data); })
    .catch(function(e) {
      container.innerHTML = '<p>Could not load knowledge graph.</p>';
    });
}

function renderGraph(container, data) {
  var width = container.clientWidth || 800;
  var height = container.clientHeight || 600;
  
  // Entry type colors (distinct desert palette colors)
  var typeColors = {
    'pattern': '#E07A3A',
    'research': '#5BA8C8',
    'reference': '#5F8A58',
    'project-report': '#9B6FCF',
    'blog-post': '#D4849A'
  };
  
  var svg = d3.select(container)
    .append('svg')
    .attr('width', width)
    .attr('height', height)
    .attr('viewBox', [0, 0, width, height]);
  
  // Zoom behavior
  var g = svg.append('g');
  var zoom = d3.zoom()
    .scaleExtent([0.3, 5])
    .on('zoom', function(event) {
      g.attr('transform', event.transform);
    });
  svg.call(zoom);
  
  // Zoom controls
  var controls = d3.select(container)
    .append('div')
    .attr('class', 'memex-graph-controls');
  controls.append('button')
    .attr('class', 'memex-graph-zoom-btn')
    .attr('title', 'Zoom in')
    .attr('aria-label', 'Zoom in')
    .text('+')
    .on('click', function() { svg.transition().duration(300).call(zoom.scaleBy, 1.4); });
  controls.append('button')
    .attr('class', 'memex-graph-zoom-btn')
    .attr('title', 'Zoom out')
    .attr('aria-label', 'Zoom out')
    .text('−')
    .on('click', function() { svg.transition().duration(300).call(zoom.scaleBy, 0.7); });
  controls.append('button')
    .attr('class', 'memex-graph-zoom-btn')
    .attr('title', 'Reset zoom')
    .attr('aria-label', 'Reset zoom')
    .text('⟲')
    .on('click', function() { svg.transition().duration(300).call(zoom.transform, d3.zoomIdentity); });
  
  // Tooltip (outside SVG, in container)
  var tooltip = d3.select(container)
    .append('div')
    .attr('class', 'memex-graph-tooltip')
    .style('display', 'none');
  
  // Build node/link data
  var nodeMap = {};
  data.nodes.forEach(function(n) { nodeMap[n.id] = n; });
  
  var links = data.edges.map(function(e) {
    return {
      source: e.source,
      target: e.target,
      weight: e.weight,
      reason: e.reason,
      type: e.type
    };
  }).filter(function(l) {
    return nodeMap[l.source] && nodeMap[l.target];
  });
  
  var nodes = data.nodes.map(function(n) {
    return {
      id: n.id,
      title: n.title,
      entryType: n.entryType,
      url: n.url,
      description: n.description,
      connectionCount: n.connectionCount,
      radius: Math.max(5, Math.min(15, 4 + n.connectionCount * 0.5))
    };
  });
  
  // Force simulation
  var simulation = d3.forceSimulation(nodes)
    .force('link', d3.forceLink(links).id(function(d) { return d.id; }).distance(80))
    .force('charge', d3.forceManyBody().strength(-120))
    .force('center', d3.forceCenter(width / 2, height / 2))
    .force('collision', d3.forceCollide().radius(function(d) { return d.radius + 5; }));
  
  // Edges (in g group for zoom)
  var link = g.append('g')
    .selectAll('line')
    .data(links)
    .join('line')
    .attr('class', 'memex-graph-edge')
    .attr('stroke-width', function(d) { return Math.max(1, d.weight * 2); });
  
  // Node groups (in g group for zoom)
  var node = g.append('g')
    .selectAll('g')
    .data(nodes)
    .join('g')
    .attr('class', 'memex-graph-node')
    .call(d3.drag()
      .on('start', dragstarted)
      .on('drag', dragged)
      .on('end', dragended));
  
  // Circles
  node.append('circle')
    .attr('r', function(d) { return d.radius; })
    .attr('fill', function(d) { return typeColors[d.entryType] || '#8B5FBF'; })
    .attr('stroke', '#fff')
    .attr('stroke-width', 1.5);
  
  // Labels (shortened titles)
  node.append('text')
    .attr('class', 'memex-graph-label')
    .attr('dx', function(d) { return d.radius + 3; })
    .attr('dy', '0.35em')
    .text(function(d) { 
      var t = d.title.replace(/^Pattern:\s*|^Research:\s*|^Project Report:\s*/i, '');
      return t.length > 25 ? t.substring(0, 23) + '…' : t;
    });
  
  // Interactions
  node.on('mouseover', function(event, d) {
    tooltip.style('display', 'block');
    tooltip.selectAll('*').remove();
    tooltip.text('');
    tooltip.append('strong').text(d.title);
    tooltip.append('br');
    var meta = tooltip.append('span');
    meta.append('em').text(d.entryType);
    meta.append('span').text(' · ' + d.connectionCount + ' connections');
    if (d.description) {
      tooltip.append('br');
      tooltip.append('span').text(d.description.substring(0, 100) + '…');
    }
    var rect = container.getBoundingClientRect();
    tooltip
      .style('left', (event.clientX - rect.left + 15) + 'px')
      .style('top', (event.clientY - rect.top - 10) + 'px');
  })
  .on('mouseout', function() {
    tooltip.style('display', 'none');
  })
  .on('click', function(event, d) {
    window.location.href = d.url;
  });
  
  // Tick
  simulation.on('tick', function() {
    link
      .attr('x1', function(d) { return d.source.x; })
      .attr('y1', function(d) { return d.source.y; })
      .attr('x2', function(d) { return d.target.x; })
      .attr('y2', function(d) { return d.target.y; });
    
    node.attr('transform', function(d) {
      d.x = Math.max(d.radius, Math.min(width - d.radius, d.x));
      d.y = Math.max(d.radius, Math.min(height - d.radius, d.y));
      return 'translate(' + d.x + ',' + d.y + ')';
    });
  });
  
  function dragstarted(event) {
    if (!event.active) simulation.alphaTarget(0.3).restart();
    event.subject.fx = event.subject.x;
    event.subject.fy = event.subject.y;
  }
  
  function dragged(event) {
    event.subject.fx = event.x;
    event.subject.fy = event.y;
  }
  
  function dragended(event) {
    if (!event.active) simulation.alphaTarget(0);
    event.subject.fx = null;
    event.subject.fy = null;
  }
}

// --- Initialization ---
document.addEventListener('DOMContentLoaded', function() {
  // Always init filters (landing page)
  initMemexFilters();
  
  // Auto-load graph on dedicated graph page (container is visible)
  var graphContainer = document.getElementById('memex-graph');
  if (graphContainer && graphContainer.style.display !== 'none' && !graphContainer.dataset.loaded) {
    loadMemexGraph(graphContainer);
  }
});
