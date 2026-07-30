#!/usr/bin/env python3
"""Convert TSPLIB .tsp files into the node list format the project parses."""
import os
import re
import sys

SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'tsplib')
DST = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'instances')
os.makedirs(DST, exist_ok=True)

for name in ['burma14', 'ulysses16', 'eil51', 'berlin52', 'st70', 'eil76']:
    path = os.path.join(SRC, name + '.tsp')
    with open(path) as f:
        lines = f.read().splitlines()

    metric = 'EUC_2D'
    coords = []
    in_coords = False
    for line in lines:
        s = line.strip()
        if s.startswith('EDGE_WEIGHT_TYPE'):
            metric = s.split(':', 1)[1].strip()
        if s.startswith('NODE_COORD_SECTION'):
            in_coords = True
            continue
        if s in ('EOF', 'DISPLAY_DATA_SECTION') or s.startswith('EOF'):
            in_coords = False
            continue
        if in_coords and s:
            parts = re.split(r'\s+', s)
            if len(parts) >= 3:
                coords.append((int(parts[0]), parts[1], parts[2]))

    out = [f'# NAME : {name} ( {metric} )']
    for idx, x, y in coords:
        out.append(f'n,{idx},{x},{y}')

    with open(os.path.join(DST, name + '.txt'), 'w') as f:
        f.write('\n'.join(out) + '\n')

    print(f'{name:10s} metric={metric:7s} nodes={len(coords)}')
