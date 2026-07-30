#!/usr/bin/env python3
"""Builds the comparison tables: thesis (2020) vs baseline code vs fixed code."""
import json
import os
import statistics
import sys

import thesis_values as T

HERE = os.path.dirname(os.path.abspath(__file__))


def load(label, which):
    path = os.path.join(HERE, 'results', label, f'{which}.json')
    if not os.path.exists(path):
        return None
    with open(path) as f:
        return json.load(f)


def dif(value, optimum):
    return (value - optimum) / optimum * 100


def fmt(v, w=8, p=2):
    if v is None:
        return '-'.rjust(w)
    return f'{v:{w}.{p}f}'


def hc_table(out):
    base = load('baseline', 'hc')
    fix = load('fixed', 'hc')
    if not base or not fix:
        return

    def index(bundle):
        d = {}
        for r in bundle['hill_climbing']:
            d[(r['DescentType'], r['Instance'], r['NeighbourType'])] = r
        return d

    b, f = index(base), index(fix)

    out('## Hill Climbing (thesis table 7.3)\n')
    out('delta = tour length, ddif = % above the optimum, t = seconds.')
    out('"2020" is the thesis, "base" this machine running the pre-fix code, "fix" the current code.\n')
    out(f'{"descent":9s} {"instance":10s} {"op":7s} '
        f'{"delta 2020":>10s} {"delta base":>10s} {"delta fix":>10s}   '
        f'{"ddif 2020":>9s} {"ddif fix":>9s}   {"t 2020":>7s} {"t base":>7s} {"t fix":>7s}')
    exact = 0
    total = 0
    for descent in ['Next', 'Steepest']:
        for inst in T.INSTANCES:
            for op in ['Swap', 'TwoOpt']:
                th = T.HC[(descent, inst)][op]
                rb = b.get((descent, inst, op))
                rf = f.get((descent, inst, op))
                opt = T.OPTIMA[inst]
                total += 1
                if rb and abs(rb['TourLength'] - th[0]) < 0.5:
                    exact += 1
                out(f'{descent:9s} {inst:10s} {op:7s} '
                    f'{th[0]:10.0f} {rb["TourLength"]:10.0f} {rf["TourLength"]:10.0f}   '
                    f'{th[1]:9.2f} {dif(rf["TourLength"], opt):9.2f}   '
                    f'{th[2]:7.2f} {rb["RunTime"]:7.3f} {rf["RunTime"]:7.3f}')
    out(f'\nBaseline reproduces the thesis tour length in {exact}/{total} cells.\n')


def sa_table(out):
    base = load('baseline', 'sa')
    fix = load('fixed', 'sa')
    if not base or not fix:
        return

    def agg(bundle):
        buckets = {}
        for r in bundle['simulated_annealing']:
            key = (r['_phase'], r['Instance'], r['NeighbourType'])
            buckets.setdefault(key, []).append(r)
        out_ = {}
        for key, rows in buckets.items():
            lengths = [x['TourLength'] for x in rows]
            times = [x['RunTime'] for x in rows]
            out_[key] = (min(lengths), statistics.mean(lengths), statistics.mean(times), len(rows))
        return out_

    b, f = agg(base), agg(fix)

    out('## Simulated Annealing (thesis table 7.6)\n')
    out('10 runs per cell. dmin = best of 10, davg = mean, ddif = % of davg above the optimum.')
    out('Phase lengths 1 and 5 sit in appendix B of the thesis and are not in the supplied excerpt,')
    out('so only 10-80 can be compared against 2020.\n')
    out(f'{"ph":>3s} {"instance":10s} {"op":7s} '
        f'{"dmin 2020":>9s} {"dmin base":>9s} {"dmin fix":>9s}   '
        f'{"davg 2020":>9s} {"davg base":>9s} {"davg fix":>9s}   '
        f'{"ddif 2020":>9s} {"ddif fix":>9s}   {"t 2020":>7s} {"t base":>7s} {"t fix":>7s}')
    for phase in [1, 5, 10, 20, 40, 80]:
        for inst in T.INSTANCES:
            for op in ['Swap', 'TwoOpt']:
                th = T.SA.get((phase, inst), {}).get(op)
                rb = b.get((phase, inst, op))
                rf = f.get((phase, inst, op))
                if not rf:
                    continue
                opt = T.OPTIMA[inst]
                out(f'{phase:3d} {inst:10s} {op:7s} '
                    f'{fmt(th[0] if th else None, 9, 0)} {fmt(rb[0] if rb else None, 9, 0)} {fmt(rf[0], 9, 0)}   '
                    f'{fmt(th[1] if th else None, 9, 1)} {fmt(rb[1] if rb else None, 9, 1)} {fmt(rf[1], 9, 1)}   '
                    f'{fmt(th[2] if th else None, 9, 2)} {fmt(dif(rf[1], opt), 9, 2)}   '
                    f'{fmt(th[3] if th else None, 7, 2)} {fmt(rb[2] if rb else None, 7, 3)} {fmt(rf[2], 7, 3)}')
    out('')

    # How often did each variant reach the optimum?
    for label, data in [('2020', None), ('base', b), ('fix', f)]:
        if data is None:
            hits = sum(1 for (p, i), ops in T.SA.items() for op, v in ops.items()
                       if abs(v[0] - T.OPTIMA[i]) < 0.5)
            cells = sum(len(ops) for ops in T.SA.values())
        else:
            keys = [k for k in data if k[0] in (10, 20, 40, 80)]
            hits = sum(1 for k in keys if abs(data[k][0] - T.OPTIMA[k[1]]) < 0.5)
            cells = len(keys)
        out(f'optimum reached (dmin == opt), phases 10-80: {label:5s} {hits}/{cells}')
    out('')


def as_table(out):
    base = load('baseline', 'as')
    fix = load('fixed', 'as')
    if not base or not fix:
        return

    def index(bundle):
        return {(r['AntCount'], r['Instance'], r['Iterations'] if r['Iterations'] else None): r
                for r in bundle['ant_system']}

    def index_by_arg(bundle, iters_list=(5, 10, 20)):
        # Iterations in the result is the count actually executed, so rebuild the grid in order.
        d = {}
        rows = bundle['ant_system']
        i = 0
        for ants in [5, 10]:
            for inst in T.INSTANCES:
                for it in iters_list:
                    d[(ants, inst, it)] = rows[i]
                    i += 1
        return d

    b, f = index_by_arg(base), index_by_arg(fix)

    out('## Ant System (thesis table 7.8)\n')
    out('One run per cell, as in the thesis (the 2020 runtimes made repetition impractical).\n')
    out(f'{"ants":>4s} {"instance":10s} {"it":>3s} '
        f'{"delta 2020":>10s} {"delta base":>10s} {"delta fix":>10s}   '
        f'{"ddif 2020":>9s} {"ddif fix":>9s}   '
        f'{"t 2020":>9s} {"t base":>9s} {"t fix":>9s} {"speedup":>8s}')
    exact = 0
    total = 0
    for ants in [5, 10]:
        for inst in T.INSTANCES:
            for it in [5, 10, 20]:
                th = T.AS[(ants, inst)][it]
                rb, rf = b[(ants, inst, it)], f[(ants, inst, it)]
                opt = T.OPTIMA[inst]
                total += 1
                if abs(rb['TourLength'] - th[0]) < 0.5:
                    exact += 1
                speed = th[2] / rf['RunTime'] if rf['RunTime'] > 0 else None
                out(f'{ants:4d} {inst:10s} {it:3d} '
                    f'{th[0]:10.0f} {rb["TourLength"]:10.0f} {rf["TourLength"]:10.0f}   '
                    f'{th[1]:9.2f} {dif(rf["TourLength"], opt):9.2f}   '
                    f'{th[2]:9.2f} {rb["RunTime"]:9.3f} {rf["RunTime"]:9.3f} '
                    f'{fmt(speed, 8, 0) if speed else "-":>8s}')
    out(f'\nBaseline reproduces the thesis tour length in {exact}/{total} cells.\n')


def timings_table(out):
    base = load('baseline', 'timings')
    fix = load('fixed', 'timings')
    if not base or not fix:
        return

    web_path = os.path.join(HERE, 'web_timings.json')
    web = json.load(open(web_path)) if os.path.exists(web_path) else {}
    mt8_path = os.path.join(HERE, 'sa_mintemp8.json')
    mt8 = json.load(open(mt8_path)) if os.path.exists(mt8_path) else {}
    bweb_path = os.path.join(HERE, 'web_timings_baseline.json')
    bweb = json.load(open(bweb_path)) if os.path.exists(bweb_path) else {}

    b = {r['instance']: r for r in base['script_timings']}
    f = {r['instance']: r for r in fix['script_timings']}

    for key, name, thesis in [('hc', 'Hill Climbing (2-opt, steepest)', T.WEB_HC),
                              ('sa', 'Simulated Annealing (swap, phase 10)', T.WEB_SA),
                              ('as', 'Ant System (5 ants, 5 iterations)', T.WEB_AS)]:
        note = ('  (script column re-measured at MinTemp 1e-8, the value the page uses)'
                if key == 'sa' else '')
        out(f'## {name}: script vs web{note}\n')
        out(f'{"instance":10s} '
            f'{"scr 2020":>9s} {"scr base":>9s} {"scr fix":>9s}   '
            f'{"web 2020":>9s} {"web base":>9s} {"web fix":>9s}   '
            f'{"fac 2020":>8s} {"fac base":>8s} {"fac fix":>8s}')
        for inst in T.RANDOM:
            th = thesis[inst]
            sb = b[inst][key]
            sf = f[inst][key]
            # The web page cannot be set below MinTemp 1e-7 and defaults to 1e-8, while the
            # script run above uses the thesis value 1e-9 - so the factor needs a script run
            # at the temperature the browser actually used.
            if key == 'sa' and mt8:
                sb = mt8['baseline'][inst]
                sf = mt8['fixed'][inst]
            wf = (web.get(key) or {}).get(inst)
            wb = (bweb.get(key) or {}).get(inst)
            factor = wf / sf if wf and sf else None
            factor_b = wb / sb if wb and sb else None
            out(f'{inst:10s} '
                f'{th[0]:9.3f} {sb:9.3f} {sf:9.3f}   '
                f'{th[1]:9.3f} {fmt(wb, 9, 3)} {fmt(wf, 9, 3)}   '
                f'{th[2]:8.2f} {fmt(factor_b, 8, 2)} {fmt(factor, 8, 2)}')
        out('')


def main():
    lines = []

    def out(msg=''):
        lines.append(msg)

    out('# Re-run of the chapter 7 performance analysis')
    out('')
    out('Instances: the six TSPLIB problems of table 7.1, converted to the project format,')
    out('scored with the TSPLIB metric each one declares (GEO for burma14 and ulysses16,')
    out('EUC_2D for the rest). random10..random80 regenerated with GraphGenerator at 100x100.')
    out('')
    hc_table(out)
    sa_table(out)
    as_table(out)
    timings_table(out)

    text = '\n'.join(lines)
    with open(os.path.join(HERE, 'report.txt'), 'w') as fh:
        fh.write(text + '\n')
    print(text)


if __name__ == '__main__':
    main()
