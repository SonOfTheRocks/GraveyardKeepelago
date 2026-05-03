import json
import os
import re
import math
from pyvis.network import Network

# Folder containing your FlowCanvas JSON files
JSON_FOLDER = "FlowGraphExports"  # adjust to your folder path
OUTPUT_FOLDER = "FlowGraphExports_HTML"

os.makedirs(OUTPUT_FOLDER, exist_ok=True)

color_map = {
    "CustomEvent": "orange",
    "Flow_PlayerEnable": "skyblue",
    "Flow_MultiAnswer": "green",
    "CustomFunctionCall": "purple",
    "Flow_Talk": "yellow",
    "Flow_SetPlayerParam": "cyan",
    "Flow_AddPlayerParam": "cyan",
    "Flow_AddRelation": "red"
}

def get_node_label(node_type):
    if not node_type:
        return "Unknown"

    match = re.match(r"([^\`]+)`\d+\[\[([^\],]+)", node_type)
    if match:
        return match.group(2).split('.')[-1]

    return node_type.split('.')[-1]

def strip_color_tags(text):
    """Helper to remove Unity rich text tags from tooltips"""
    return re.sub(r'<.*?>', '', text)

def visualize_flowcanvas_json(json_path, output_path):
    with open(json_path, "r", encoding="utf-8") as f:
        raw = f.read()
    raw = re.sub(r'[\x00-\x1f]', '', raw)
    data = json.loads(raw)

    net = Network(height="1000px", width="100%", directed=True)

    uid_to_node_id = {}
    node_id_to_data = {}
    event_name_to_node_id = {}
    search_data = []

    node_ports = {}
    for node in data.get("nodes", []):
        node_id = str(node.get("$id"))
        if node_id == "None": continue
        node_ports[node_id] = {"in": set(), "out": set()}

        input_vals = node.get("_inputPortValues", {})
        for k in input_vals.keys():
            node_ports[node_id]["in"].add(k)

        if "_UID" in node:
            uid_to_node_id[node["_UID"]] = node_id
        node_id_to_data[node_id] = node

        if "eventName" in node and "_value" in node["eventName"]:
            event_name_to_node_id[node["eventName"]["_value"]] = node_id

    for conn in data.get("connections", []):
        src = str(conn["_sourceNode"]["$ref"])
        tgt = str(conn["_targetNode"]["$ref"])
        src_port = conn.get("_sourcePortName", "Out")
        tgt_port = conn.get("_targetPortName", "In")
        if src in node_ports: node_ports[src]["out"].add(src_port)
        if tgt in node_ports: node_ports[tgt]["in"].add(tgt_port)

    NODE_WIDTH = 100
    PORT_SPACING = 15 # Increased slightly to accommodate extra mapping lines
    PORT_OFFSET = 5
    PORT_SIZE = 4
    port_bindings = []

    for node in data.get("nodes", []):
        node_id = str(node.get("$id"))
        if node_id == "None": continue

        node_type = node.get("$type", "Unknown")
        node_label = get_node_label(node_type)
        color = color_map.get(node_label, "lightgray")
        pos = node.get("_position", {"x": 0, "y": 0})
        x, y = pos["x"], -pos["y"]

        input_vals = node.get("_inputPortValues", {})
        event_name = node.get("eventName", {}).get("_value")

        param_name = None
        if "param" in input_vals and isinstance(input_vals["param"], dict):
            param_name = input_vals["param"].get("$content")

        if node_label == "CustomEvent" and event_name:
            node_label = f"{node_label}\n({event_name})"
        elif node_label == "Flow_GetPlayerParamInt" and param_name:
            node_label = f"{node_label}\n({param_name})"
        elif node_label == "Flow_SetPlayerParam" and "Param name" in input_vals and "Value" in input_vals:
            node_label = f"{node_label}\n{input_vals['Param name']['$content']}: {input_vals['Value']['$content']}"
        elif node_label == "Flow_SmartRes" and "id" in input_vals and "v" in input_vals:
            node_label = f"{node_label}\n{input_vals['id']['$content']}: {input_vals['v']['$content']}"
        elif node_label == "Flow_SetTaskState" and "Task" in input_vals and "State" in input_vals:
            node_label = f"{node_label}\n{input_vals['Task']['$content']}: {input_vals['State']['$content']}"
        elif node_label == "Flow_Talk" and "Text" in input_vals and input_vals.get("Text"):
            node_label = f"{node_label}\n({input_vals['Text']['$content']})"
        elif node_label == "Flow_AddPhraseToBlacklist" and "Phrase ID" in input_vals:
            node_label = f"{node_label}\n({input_vals['Phrase ID']['$content']})"
        elif node_label == "Flow_UnlockPhrase" and "Phrase" in input_vals:
            node_label = f"{node_label}\n({input_vals['Phrase']['$content']})"
        elif node_label == "Flow_SetWGOParam" and "Param name" in input_vals and input_vals.get("Param name"):
            node_label = f"{node_label}\n({input_vals['Param name']['$content']})"
        elif node_label == "Flow_PlayerFlag" and "param" in input_vals:
            node_label = f"{node_label}\n({input_vals['param']['$content']})"
        elif node_label == "Flow_CompareRelation" and "npc_id" in input_vals and input_vals.get("npc_id") and "Value" in input_vals:
            node_label = f"{node_label}\n{input_vals['npc_id'].get('$content', '?')}: {input_vals['Value']['$content']}"
        elif node_label == "Wait" and "time" in input_vals:
            node_label = f"{node_label} {input_vals['time']['$content']}"
        elif node_label == "Flow_CustomFunctionEvent" and "identifier" in node.keys():
            node_label = f"{node_label}\n({node.get('identifier')})"
        elif node_label == "Flow_CheckKeyQuest" and "Quest Key" in input_vals:
            node_label = f"{node_label}\n({input_vals["Quest Key"]})"
        elif node_label == "Flow_ParamFlag" and "param" in input_vals:
            node_label = f"{node_label}\n({input_vals["param"]})"
        elif node_label == "CustomEventFunctionEvent" and "ID" in node.keys():
            node_label = f"{node_label}\n({node.get("ID", "<unknown>")})"

        # --- NEW: Append the mapping directly to the MultiAnswer node box label ---
        elif node_label == "Flow_MultiAnswer":
            answers = node.get("answers", [])
            if answers:
                # Truncate long strings for the node face to keep boxes tidy
                maps = [f"#{i} \u2192 {ans[:15] + '...' if len(ans) > 15 else ans}" for i, ans in enumerate(answers)]
                node_label = f"{node_label}\n\n" + "\n".join(maps)

        target_var = None
        value = None
        if "targetVariable" in node and isinstance(node["targetVariable"], dict):
            target_var = node["targetVariable"].get("_name")
        if target_var:
            value = input_vals.get("Value")
            if isinstance(value, dict):
                value = value.get("$content")

        tooltip = f"{node_label}\n"
        tooltip += f"ID: {node.get('identifier', '')}\n"
        tooltip += f"Comment: {node.get('_comment', '')}\n"
        if event_name:
            tooltip += f"Event Name: {event_name}\n"
        if target_var and value:
            tooltip += f"Target Variable: {target_var} = {value}\n"

        tooltip += "Inputs:\n"
        for k, v in input_vals.items():
            val_str = v.get("$content", str(v)) if isinstance(v, dict) else str(v)
            tooltip += f"{strip_color_tags(k)}: {val_str}\n"

        # --- NEW: Append full detailed mapping to the tooltip ---
        if get_node_label(node_type) == "Flow_MultiAnswer":
            tooltip += "\nMappings:\n"
            for i, ans in enumerate(node.get("answers", [])):
                tooltip += f"  #{i} \u2192 out_{i} ({ans})\n"

        in_ports = sorted(list(node_ports[node_id]["in"]))
        out_ports = sorted(list(node_ports[node_id]["out"]))

        max_ports = max(len(in_ports), len(out_ports), 1)
        required_height = max_ports * PORT_SPACING + 10
        approx_line_height = 18
        current_lines = node_label.count('\n') + 1

        if current_lines * approx_line_height < required_height:
            extra_lines = int(math.ceil((required_height - current_lines * approx_line_height) / approx_line_height))
            node_label += '\n' * extra_lines

        net.add_node(
            node_id,
            label=node_label,
            title=strip_color_tags(tooltip), # Clean Unity rich text tags
            shape="box",
            widthConstraint={"minimum": NODE_WIDTH, "maximum": NODE_WIDTH},
            x=x, y=y,
            physics=False,
            color=color
        )

        search_data.append({
            "label": node_label.replace('\n', ' ').strip(),
            "title": tooltip
        })

        start_y = y - ((len(in_ports) - 1) * PORT_SPACING) / 2
        for i, p_name in enumerate(in_ports):
            p_id = f"{node_id}_in_{p_name}"
            px = x - (NODE_WIDTH / 2) - PORT_OFFSET
            py = start_y + i * PORT_SPACING
            net.add_node(p_id, label=" ", title=f"In: {strip_color_tags(p_name)}", shape="dot", size=PORT_SIZE, x=px, y=py, physics=False, color="#3498db", fixed=True)
            port_bindings.append({"parent": node_id, "child": p_id, "ox": -NODE_WIDTH/2 - PORT_OFFSET, "oy": py - y})

        start_y = y - ((len(out_ports) - 1) * PORT_SPACING) / 2
        for i, p_name in enumerate(out_ports):
            p_id = f"{node_id}_out_{p_name}"
            px = x + (NODE_WIDTH / 2) + PORT_OFFSET
            py = start_y + i * PORT_SPACING
            net.add_node(p_id, label=" ", title=f"Out: {p_name}", shape="dot", size=PORT_SIZE, x=px, y=py, physics=False, color="#e74c3c", fixed=True)
            port_bindings.append({"parent": node_id, "child": p_id, "ox": NODE_WIDTH/2 + PORT_OFFSET, "oy": py - y})


    for conn in data.get("connections", []):
        src = str(conn["_sourceNode"]["$ref"])
        tgt = str(conn["_targetNode"]["$ref"])
        raw_label = conn.get("_sourcePortName", "")

        src_port = raw_label if raw_label else "Out"
        tgt_port = conn.get("_targetPortName", "In")
        label = raw_label

        src_node = node_id_to_data.get(src)
        if src_node and src_node.get("$type") == "FlowCanvas.Nodes.Flow_MultiAnswer":
            answers = src_node.get("answers", [])
            match = re.search(r"out_(\d+)", raw_label)
            if match:
                idx = int(match.group(1))
                if idx < len(answers):
                    label = answers[idx]

        p_src_id = f"{src}_out_{src_port}"
        p_tgt_id = f"{tgt}_in_{tgt_port}"

        final_src = p_src_id if src in node_ports and src_port in node_ports[src]["out"] else src
        final_tgt = p_tgt_id if tgt in node_ports and tgt_port in node_ports[tgt]["in"] else tgt

        net.add_edge(final_src, final_tgt, title=label, arrows="to")

    for node_id, node in node_id_to_data.items():
        if node.get("$type") == "FlowCanvas.Nodes.CustomFunctionCall":
            target_uid = node.get("_sourceOutputUID")
            target_node_id = uid_to_node_id.get(target_uid)
            if target_node_id:
                net.add_edge(node_id, target_node_id, title="calls", color="purple", arrows="to")

    for node_id, node in node_id_to_data.items():
        input_vals = node.get("_inputPortValues", {})
        if "event" in input_vals and isinstance(input_vals["event"], dict):
            subscribed_event = input_vals["event"].get("$content")
            if subscribed_event and subscribed_event in event_name_to_node_id:
                target_node_id = event_name_to_node_id[subscribed_event]
                net.add_edge(node_id, target_node_id, title=f"event: {subscribed_event}", color="orange", arrows="to")

    net.write_html(output_path)

    # Inject UI mechanics
    with open(output_path, "a", encoding="utf-8") as f:
        f.write(f"""<style>
            #info-panel {{
                position: fixed;
                top: 0;
                right: -400px; /* Hidden off-screen initially */
                width: 350px;
                height: 100%;
                background-color: #f8f9fa;
                box-shadow: -3px 0 10px rgba(0,0,0,0.15);
                z-index: 1000;
                transition: right 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
                display: flex;
                flex-direction: column;
                font-family: Arial, sans-serif;
            }}
            #info-panel.open {{
                right: 0;
            }}
            #info-header {{
                padding: 15px 20px;
                background: #e9ecef;
                border-bottom: 1px solid #dee2e6;
                display: flex;
                justify-content: space-between;
                align-items: center;
            }}
            #info-header h3 {{ 
                margin: 0; 
                font-size: 18px; 
                color: #343a40;
            }}
            #close-btn {{ 
                cursor: pointer; 
                border: none; 
                background: none; 
                font-size: 24px; 
                font-weight: bold; 
                color: #6c757d;
            }}
            #close-btn:hover {{ color: #dc3545; }}
            #info-body {{
                padding: 20px;
                overflow-y: auto;
                flex-grow: 1;
                white-space: pre-wrap; /* Preserves newlines from the tooltip */
                word-break: break-word;
                font-size: 14px;
                color: #495057;
                line-height: 1.5;
            }}
        </style>
        
        <div id="info-panel">
            <div id="info-header">
                <h3>Node Details</h3>
                <button id="close-btn" title="Close Panel">&times;</button>
            </div>
            <div id="info-body">Select a node to view details...</div>
        </div>
        
        <script>
            var portBindings = {json.dumps(port_bindings)};
            
            setTimeout(function() {{
                if (typeof network !== 'undefined' && typeof nodes !== 'undefined') {{
                    network.on("dragging", function(params) {{
                        if (params.nodes.length > 0) {{
                            var updates = [];
                            for (var i=0; i<params.nodes.length; i++) {{
                                var draggedId = params.nodes[i];
                                var pos = network.getPositions([draggedId])[draggedId];
                                
                                portBindings.forEach(function(bind) {{
                                    if (bind.parent === draggedId) {{
                                        updates.push({{
                                            id: bind.child,
                                            x: pos.x + bind.ox,
                                            y: pos.y + bind.oy
                                        }});
                                    }}
                                }});
                            }}
                            if (updates.length > 0) nodes.update(updates);
                        }}
                    }});
                    
                    network.on("click", function(params) {{
                        if (params.nodes.length > 0) {{
                            var nodeId = params.nodes[0];
                            
                            // Ignore clicks on tiny port nubs, only show panel for actual boxes
                            if (String(nodeId).includes("_in_") || String(nodeId).includes("_out_")) {{
                                return;
                            }}
                            
                            var clickedNode = nodes.get(nodeId);
                            var infoBody = document.getElementById('info-body');
                            
                            if (clickedNode && clickedNode.title) {{
                                // Set the panel text to the node's tooltip (title)
                                infoBody.textContent = clickedNode.title;
                                document.getElementById('info-panel').classList.add('open');
                            }}
                        }} else {{
                            // Hide the panel if clicking on empty canvas space
                            document.getElementById('info-panel').classList.remove('open');
                        }}
                    }});
                }}
                
                var hash = window.location.hash.substring(1);
                if (hash && typeof nodes !== 'undefined' && typeof network !== 'undefined') {{
                    var searchTerm = decodeURIComponent(hash).toLowerCase();
                    var allNodes = nodes.get();
                    var updates = [];
                    var highlightIds = [];
                    
                    for (var i = 0; i < allNodes.length; i++) {{
                        var n = allNodes[i];
                        var textToSearch = ((n.label || "") + " " + (n.title || "")).toLowerCase();
                        
                        if (textToSearch.includes(searchTerm)) {{
                            updates.push({{
                                id: n.id, 
                                color: {{ background: '#ff4d4d', border: '#cc0000' }},
                                borderWidth: 4
                            }});
                            highlightIds.push(n.id);
                        }}
                    }}
                    
                    if (updates.length > 0) {{
                        nodes.update(updates);
                        network.fit({{ nodes: highlightIds, animation: true }});
                    }}
                }}
            }}, 500);
        </script>
        """)

    print(f"Saved: {output_path}")
    return search_data

master_index = {}
for filename in os.listdir(JSON_FOLDER):
    if filename.endswith(".json"):
        json_path = os.path.join(JSON_FOLDER, filename)
        html_filename = filename.replace(".json", ".html")
        output_path = os.path.join(OUTPUT_FOLDER, html_filename)

        file_search_data = visualize_flowcanvas_json(json_path, output_path)
        master_index[html_filename] = file_search_data

index_html_path = "index.html"
with open(index_html_path, "w", encoding="utf-8") as f:
    f.write(f"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>FlowCanvas Network Explorer</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <style>
        body, html {{ height: 100%; margin: 0; display: flex; flex-direction: column; overflow: hidden; }}
        #top-bar {{ background: #f8f9fa; padding: 15px; border-bottom: 1px solid #ddd; flex-shrink: 0; }}
        #viewer {{ flex-grow: 1; border: none; width: 100%; }}
    </style>
</head>
<body>

    <div id="top-bar">
        <div class="container-fluid">
            <div class="row align-items-end">
                <div class="col-md-5">
                    <label for="searchInput" class="form-label"><strong>Search Nodes (Label or Tooltip):</strong></label>
                    <input type="text" id="searchInput" class="form-control" placeholder="Type to search across all graphs...">
                </div>
                <div class="col-md-5">
                    <label for="fileSelect" class="form-label"><strong>Matching Graphs:</strong></label>
                    <select id="fileSelect" class="form-select">
                        <option value="">-- Type to search or select a graph --</option>
                    </select>
                </div>
                <div class="col-md-2">
                    <button id="loadBtn" class="btn btn-primary w-100">Load Graph</button>
                </div>
            </div>
            <div class="row mt-2">
                <div class="col-12 text-muted" id="resultsCount">Waiting for search...</div>
            </div>
        </div>
    </div>

    <iframe id="viewer" src=""></iframe>

    <script>
        const masterIndex = {json.dumps(master_index)};
        
        const searchInput = document.getElementById('searchInput');
        const fileSelect = document.getElementById('fileSelect');
        const loadBtn = document.getElementById('loadBtn');
        const viewer = document.getElementById('viewer');
        const resultsCount = document.getElementById('resultsCount');

        function populateDropdown(files) {{
            fileSelect.innerHTML = '<option value="">-- Select a graph --</option>';
            files.forEach(file => {{
                let opt = document.createElement('option');
                opt.value = file;
                opt.textContent = file;
                fileSelect.appendChild(opt);
            }});
        }}
        
        populateDropdown(Object.keys(masterIndex));

        searchInput.addEventListener('input', (e) => {{
            const term = e.target.value.toLowerCase();
            if (!term) {{
                populateDropdown(Object.keys(masterIndex));
                resultsCount.textContent = "Showing all graphs.";
                return;
            }}

            let matchingFiles = [];
            
            for (const [filename, nodes] of Object.entries(masterIndex)) {{
                const hasMatch = nodes.some(n => 
                    (n.label && n.label.toLowerCase().includes(term)) || 
                    (n.title && n.title.toLowerCase().includes(term))
                );
                
                if (hasMatch) {{
                    matchingFiles.push(filename);
                }}
            }}

            populateDropdown(matchingFiles);
            resultsCount.textContent = `Found matches in ${{matchingFiles.length}} graphs.`;
            
            if (matchingFiles.length > 0) {{
                fileSelect.selectedIndex = 1;
            }}
        }});

        loadBtn.addEventListener('click', () => {{
            const selectedFile = fileSelect.value;
            const searchTerm = searchInput.value.trim();
            
            if (selectedFile) {{
                let srcUrl = "{OUTPUT_FOLDER}/" + selectedFile;
                if (searchTerm) {{
                    srcUrl += "#" + encodeURIComponent(searchTerm);
                }}
                viewer.src = srcUrl;
            }}
        }});
    </script>
</body>
</html>
""")

print(f"--- Successfully generated Master Search Page at: {index_html_path} ---")