// Measures the in-browser runtime the way the thesis did: read the "Execution Time" the page
// itself reports (AppState.Stopwatch around the FindPath call), for the same configurations as
// tables 7.4, 7.7 and 7.9.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const PORT = process.argv[2] || '5199';
const OUT = process.argv[3] || 'web_timings.json';
const INSTANCES = path.join(__dirname, 'instances');
const NAMES = [10, 20, 30, 40, 50, 60, 70, 80].map(n => `random${n}`);

const load = name => fs.readFileSync(path.join(INSTANCES, name + '.txt'), 'utf8')
  .split('\n').filter(l => l.startsWith('n,')).join('\n');

(async () => {
  const browser = await chromium.launch({ executablePath: '/opt/pw-browsers/chromium' });
  const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } });
  // Chart.js is CDN-hosted and blocked here; stub it so a missing chart cannot abort a run.
  await page.addInitScript(() => { window.Chart = function () { return { update() {}, destroy() {} }; }; });
  page.on('pageerror', e => console.log('  [pageerror] ' + e.message.split('\n')[0]));

  await page.goto(`http://localhost:${PORT}/`, { waitUntil: 'networkidle' });
  await page.waitForSelector('text=Algorithm Settings', { timeout: 60000 });

  // Keep the animation loop from competing with the measured run.
  await page.fill('#animationDelay', '5000').catch(() => {});

  const readSeconds = async () => {
    const text = await page.textContent('text=/Execution Time/');
    const m = text.match(/\(([\d.]+)\s*s\)/);
    if (!m) throw new Error('could not parse execution time from: ' + text);
    return parseFloat(m[1]);
  };

  // The WebAssembly run blocks the UI thread, so any read that completes afterwards already
  // sees the new value. Poll until it stops changing rather than requiring it to differ from the
  // previous instance's time, which coincidentally matched for one cell.
  const execute = async (timeoutMs) => {
    await page.click('text=Execute Algorithm');
    const deadline = Date.now() + timeoutMs;
    let last = null;
    while (Date.now() < deadline) {
      await page.waitForTimeout(400);
      const now = await readSeconds().catch(() => null);
      if (now !== null && now === last) return now;
      last = now;
    }
    if (last !== null) return last;
    throw new Error('timed out waiting for the run to finish');
  };

  const results = {};

  const configure = async (algo) => {
    await page.selectOption('#selectHeuristics', { label: algo });
    await page.waitForTimeout(300);
    if (algo === 'Hill Climbing') {
      await page.selectOption('#selectNeighbourType', 'TwoOpt');
      await page.selectOption('#selectDescentType', 'Steepest');
    } else if (algo === 'Simulated Annealing') {
      await page.selectOption('#selectNeighbourType', 'Swap');
      await page.fill('#iter1', '10');
      await page.fill('#temp1', '100');
      await page.fill('#alphaSA', '0.98');
      await page.locator('#alphaSA').blur();
    } else if (algo === 'Ant System') {
      await page.fill('#ants', '5');
      await page.fill('#iterations', '5');
      await page.fill('#alphaACS', '0.1');
      await page.fill('#beta', '2');
      await page.locator('#beta').blur();
      // Match the script: let all iterations run.
      const cb = page.locator('#breakWhenEqual');
      if (await cb.isChecked()) await cb.uncheck();
    }
    await page.waitForTimeout(400);
  };

  for (const [key, algo, budget] of [['hc', 'Hill Climbing', 60000],
                                     ['sa', 'Simulated Annealing', 180000],
                                     ['as', 'Ant System', 600000]]) {
    results[key] = {};
    await configure(algo);
    for (const name of NAMES) {
      await page.fill('textarea', load(name));
      await page.waitForTimeout(250);
      try {
        const t = await execute(budget);
        results[key][name] = t;
        console.log(`web ${key} ${name.padEnd(10)} ${t.toFixed(3)} s`);
      } catch (e) {
        results[key][name] = null;
        console.log(`web ${key} ${name.padEnd(10)} FAILED: ${e.message}`);
      }
    }
  }

  // Report the MinTemp the page actually used, since its validation floor differs from the CLI.
  await page.selectOption('#selectHeuristics', { label: 'Simulated Annealing' });
  await page.waitForTimeout(300);
  results.web_min_temp = await page.inputValue('#temp2').catch(() => null);
  console.log('web MinTemp field value: ' + results.web_min_temp);

  fs.writeFileSync(path.join(__dirname, OUT), JSON.stringify(results, null, 1));
  await browser.close();
})();
