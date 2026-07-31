#!/usr/bin/env python3
"""Re-runs the performance analysis of chapter 7 of the thesis.

Runs sequentially on purpose: the runtimes are the measurement, so parallel
execution on 4 cores would distort them.
"""
import json
import os
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
INSTANCES = os.path.join(HERE, 'instances')
ENV = dict(os.environ, DOTNET_ROLL_FORWARD='Major')

# name -> (metric, optimum) from thesis table 7.1
TSPLIB = [
    ('burma14',   'GEO',    3323),
    ('ulysses16', 'GEO',    6859),
    ('eil51',     'EUC_2D',  426),
    ('berlin52',  'EUC_2D', 7542),
    ('st70',      'EUC_2D',  675),
    ('eil76',     'EUC_2D',  538),
]
RANDOM = [f'random{n}' for n in range(10, 90, 10)]


def cli(exe_dir):
    return os.path.join(exe_dir, 'PerformanceAnalysis.cli')


def run_one(exe_dir, out_dir, instance, metric, args, number=0):
    """Runs one configuration and returns the parsed SimulationResult."""
    graph = os.path.join(INSTANCES, instance + '.txt')
    prefix = os.path.join(out_dir, 'r_')
    cmd = [cli(exe_dir), '--graph', graph, '--metric', metric,
           '--path', prefix, '--number', str(number)] + args

    before = set(os.listdir(out_dir)) if os.path.isdir(out_dir) else set()
    proc = subprocess.run(cmd, env=ENV, capture_output=True, text=True)
    if proc.returncode != 0:
        raise RuntimeError(f'{instance} {args}: exit {proc.returncode}\n{proc.stderr[:400]}')

    after = set(os.listdir(out_dir))
    new = after - before
    target = os.path.join(out_dir, sorted(new)[0]) if new else None
    if target is None:
        # File name is deterministic per config, so a repeat overwrites it.
        candidates = [f for f in after if f.endswith('.json')]
        target = os.path.join(out_dir, sorted(candidates, key=lambda f: os.path.getmtime(os.path.join(out_dir, f)))[-1])

    with open(target) as f:
        return json.load(f)


def hill_climbing(exe_dir, out_dir, log):
    results = []
    for descent in ['Next', 'Steepest']:
        for name, metric, opt in TSPLIB:
            for neighbour in ['Swap', 'TwoOpt']:
                r = run_one(exe_dir, out_dir, name, metric,
                            ['-h', 'hillclimbing', '--neighbor', neighbour, '--descent', descent])
                r['_optimum'] = opt
                results.append(r)
                log(f'HC {descent:8s} {name:10s} {neighbour:6s} '
                    f'delta={r["TourLength"]:9.1f} t={r["RunTime"]:7.3f}')
    return results


def simulated_annealing(exe_dir, out_dir, log, repeats=10):
    results = []
    for phase in [1, 5, 10, 20, 40, 80]:
        for name, metric, opt in TSPLIB:
            for neighbour in ['Swap', 'TwoOpt']:
                for rep in range(repeats):
                    r = run_one(exe_dir, out_dir, name, metric,
                                ['-h', 'annealing', '--neighbor', neighbour, '--phase', str(phase),
                                 '--start', '100', '--min', '0.000000001', '--alpha', '0.98'],
                                number=rep)
                    r['_optimum'] = opt
                    r['_phase'] = phase
                    results.append(r)
                log(f'SA phase={phase:3d} {name:10s} {neighbour:6s} done ({repeats} reps)')
    return results


def ant_system(exe_dir, out_dir, log):
    results = []
    for ants in [5, 10]:
        for name, metric, opt in TSPLIB:
            for iters in [5, 10, 20]:
                r = run_one(exe_dir, out_dir, name, metric,
                            ['-h', 'antsystem', '--ants', str(ants), '--iterations', str(iters),
                             '--alpha', '0.1', '--beta', '2', '--selection', 'greedy'])
                r['_optimum'] = opt
                results.append(r)
                log(f'AS ants={ants:2d} iters={iters:2d} {name:10s} '
                    f'delta={r["TourLength"]:9.1f} t={r["RunTime"]:8.3f}')
    return results


def ant_system_selection(exe_dir, out_dir, log):
    """Greedy against random proportional selection over a growing budget.

    The thesis ran the greedy rule, whose tour is fixed by the ant's starting node - which is
    why a bigger colony and more iterations changed nothing in table 7.8. The proportional rule
    keeps improving, so the two are compared over budgets rather than at one setting.
    """
    results = []
    for ants, iters in [(10, 20), (25, 50), (50, 100), (50, 300)]:
        for name, metric, opt in TSPLIB:
            for rule in ['greedy', 'proportional']:
                # The proportional rule is stochastic, so it is averaged over repeats.
                repeats = 1 if rule == 'greedy' else 5
                lengths, seconds = [], []
                for r in range(repeats):
                    res = run_one(exe_dir, out_dir, name, metric,
                                  ['-h', 'antsystem', '--ants', str(ants),
                                   '--iterations', str(iters), '--alpha', '0.1', '--beta', '2',
                                   '--selection', rule], number=r)
                    lengths.append(res['TourLength'])
                    seconds.append(res['RunTime'])

                entry = {'instance': name, 'ants': ants, 'iterations': iters, 'rule': rule,
                         'optimum': opt, 'repeats': repeats,
                         'mean': sum(lengths) / len(lengths), 'best': min(lengths),
                         'seconds': sum(seconds) / len(seconds)}
                results.append(entry)
                log(f'AS {rule:12s} ants={ants:3d} iters={iters:3d} {name:10s} '
                    f'mean={entry["mean"]:9.1f} ({100 * (entry["mean"] / opt - 1):5.1f}%) '
                    f'best={entry["best"]:9.1f} t={entry["seconds"]:7.3f}')
    return results


def script_timings(exe_dir, out_dir, log):
    """Runtime on the random instances, for the web-vs-script comparison."""
    rows = []
    for name in RANDOM:
        entry = {'instance': name}
        r = run_one(exe_dir, out_dir, name, 'EUCLIDEAN',
                    ['-h', 'hillclimbing', '--neighbor', 'TwoOpt', '--descent', 'Steepest'])
        entry['hc'] = r['RunTime']

        r = run_one(exe_dir, out_dir, name, 'EUCLIDEAN',
                    ['-h', 'annealing', '--neighbor', 'Swap', '--phase', '10',
                     '--start', '100', '--min', '0.000000001', '--alpha', '0.98'])
        entry['sa'] = r['RunTime']

        r = run_one(exe_dir, out_dir, name, 'EUCLIDEAN',
                    ['-h', 'antsystem', '--ants', '5', '--iterations', '5',
                     '--alpha', '0.1', '--beta', '2', '--selection', 'greedy'])
        entry['as'] = r['RunTime']

        rows.append(entry)
        log(f'script {name:10s} hc={entry["hc"]:.3f} sa={entry["sa"]:.3f} as={entry["as"]:.3f}')
    return rows


def main():
    exe_dir = sys.argv[1]
    label = sys.argv[2]
    which = sys.argv[3] if len(sys.argv) > 3 else 'all'

    out_dir = os.path.join(HERE, 'results', label, 'raw')
    os.makedirs(out_dir, exist_ok=True)
    log_path = os.path.join(HERE, 'results', label, 'run.log')

    logfile = open(log_path, 'a')

    def log(msg):
        line = f'[{time.strftime("%H:%M:%S")}] {msg}'
        print(line, flush=True)
        logfile.write(line + '\n')
        logfile.flush()

    started = time.time()
    bundle = {}
    if which in ('all', 'hc'):
        bundle['hill_climbing'] = hill_climbing(exe_dir, out_dir, log)
    if which in ('all', 'sa'):
        bundle['simulated_annealing'] = simulated_annealing(exe_dir, out_dir, log)
    if which in ('all', 'as'):
        bundle['ant_system'] = ant_system(exe_dir, out_dir, log)
    if which in ('all', 'selection'):
        bundle['ant_system_selection'] = ant_system_selection(exe_dir, out_dir, log)
    if which in ('all', 'timings'):
        bundle['script_timings'] = script_timings(exe_dir, out_dir, log)

    path = os.path.join(HERE, 'results', label, f'{which}.json')
    with open(path, 'w') as f:
        json.dump(bundle, f, indent=1)

    log(f'{label}/{which} finished in {time.time() - started:.1f}s -> {path}')


if __name__ == '__main__':
    main()
