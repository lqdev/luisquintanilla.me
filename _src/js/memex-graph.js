// AI Memex Knowledge Graph Visualization
// Uses D3.js force-directed layout loaded from CDN

function toggleMemexGraph() {
  var container = document.getElementById('memex-graph');
  if (!container) return;
  
  if (container.style.display === 'none') {
    container.style.display = 'block';
    if (!container.dataset.loaded) {
      loadMemexGraph(container);
    }
  } else {
    container.style.display = 'none';
  }
}

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
  var height = 400;
  
  // Entry type colors (matching Desert Twilight palette)
  var typeColors = {
    'pattern': '#AB7FDF',
    'research': '#9B6FCF',
    'reference': '#6B4F9F',
    'project-report': '#7B4FAF',
    'blog-post': '#8B5FBF'
  };
  
  var svg = d3.select(container)
    .append('svg')
    .attr('width', width)
    .attr('height', height)
    .attr('viewBox', [0, 0, width, height]);
  
  // Tooltip
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
  
  // Edges
  var link = svg.append('g')
    .selectAll('line')
    .data(links)
    .join('line')
    .attr('class', 'memex-graph-edge')
    .attr('stroke-width', function(d) { return Math.max(1, d.weight * 2); });
  
  // Node groups
  var node = svg.append('g')
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
