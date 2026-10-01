// Gera os artboards .dc.html dos mockups do Folguinha (node build-mockups.mjs)
import { writeFileSync, mkdirSync } from 'node:fs';

const OUT = new URL('./mockups/', import.meta.url);
mkdirSync(OUT, { recursive: true });

// Tokens
const T = {
  bg: '#FBF7F2', surface: '#FFFFFF', surface2: '#F4EEE6', line: '#E9E1D6',
  ink: '#2B2622', ink2: '#6B625A', ink3: '#9A9087',
  coral: '#E4704F', coralSoft: '#FCE9E2', coralInk: '#A9442A',
  teal: '#1F9E86', tealSoft: '#DDF3EE', tealInk: '#1E6E5E',
  red: '#C8374A', redSoft: '#FCE1E4', orange: '#D9731F', orangeSoft: '#FDEBDB',
  amber: '#8A5A00', amberSoft: '#FFF1CC', blue: '#3D6FC2', blueSoft: '#E3ECFA',
  night: '#4A479A', nightSoft: '#E4E4FA',
};
const DISPLAY = "'Fredoka', 'Trebuchet MS', system-ui, sans-serif";
const BODY = "'Figtree', 'Segoe UI', system-ui, sans-serif";

const SHIFT = {
  M: { bg: T.amberSoft, fg: T.amber, label: 'Manhã' },
  T: { bg: T.tealSoft, fg: T.tealInk, label: 'Tarde' },
  N: { bg: T.nightSoft, fg: T.night, label: 'Noite' },
  F: { bg: T.surface2, fg: T.ink3, label: 'Folga' },
  O: { bg: T.redSoft, fg: T.red, label: 'Ocorrência' },
};

const icon = (d, size = 24, color = 'currentColor', sw = 1.8) =>
  `<svg width="${size}" height="${size}" viewBox="0 0 24 24" fill="none" stroke="${color}" stroke-width="${sw}" stroke-linecap="round" stroke-linejoin="round">${d}</svg>`;
const I = {
  home: '<path d="M4 11l8-7 8 7v8.5a1.5 1.5 0 0 1-1.5 1.5H14v-6h-4v6H5.5A1.5 1.5 0 0 1 4 19.5z"></path>',
  cal: '<rect x="3.5" y="5" width="17" height="15.5" rx="3"></rect><path d="M3.5 10h17M8 3v4M16 3v4"></path>',
  people: '<circle cx="9" cy="8.5" r="3.5"></circle><path d="M2.5 20c.8-3.6 3.3-5.5 6.5-5.5s5.7 1.9 6.5 5.5"></path><path d="M15.5 5.2a3.5 3.5 0 0 1 0 6.6M18 14.8c1.8.7 3 2.4 3.5 5.2"></path>',
  clip: '<rect x="5" y="4.5" width="14" height="16.5" rx="3"></rect><path d="M9 3h6v3H9zM9 11h6M9 15h4"></path>',
  more: '<circle cx="5" cy="12" r="1.3"></circle><circle cx="12" cy="12" r="1.3"></circle><circle cx="19" cy="12" r="1.3"></circle>',
  back: '<path d="M15 5l-7 7 7 7"></path>',
  left: '<path d="M14 6l-6 6 6 6"></path>',
  right: '<path d="M10 6l6 6-6 6"></path>',
  check: '<path d="M5 12.5l4.5 4.5L19 7.5"></path>',
  plus: '<path d="M12 5v14M5 12h14"></path>',
  refresh: '<path d="M20 11a8 8 0 0 0-14.6-4.5L4 8M4 4v4h4M4 13a8 8 0 0 0 14.6 4.5L20 16M20 20v-4h-4"></path>',
  lock: '<rect x="5" y="10.5" width="14" height="10" rx="2.5"></rect><path d="M8.5 10.5V8a3.5 3.5 0 0 1 7 0v2.5"></path>',
  alert: '<path d="M12 4l9 16H3z"></path><path d="M12 10v4M12 17.2v.1"></path>',
  sun: '<circle cx="12" cy="12" r="4"></circle><path d="M12 2.5v2M12 19.5v2M2.5 12h2M19.5 12h2M5.3 5.3l1.4 1.4M17.3 17.3l1.4 1.4M5.3 18.7l1.4-1.4M17.3 6.7l1.4-1.4"></path>',
  share: '<path d="M12 15V4M8 8l4-4 4 4"></path><path d="M5 13v5.5A1.5 1.5 0 0 0 6.5 20h11a1.5 1.5 0 0 0 1.5-1.5V13"></path>',
  sheet: '<rect x="4" y="3.5" width="16" height="17" rx="2.5"></rect><path d="M4 9h16M4 14.5h16M10 3.5v17"></path>',
  spark: '<path d="M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8z"></path>',
  swap: '<path d="M7 7h12l-3-3M17 17H5l3 3"></path>',
};

const page = ({ body, script = '' }) => `<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <script src="./support.js"></script>
</head>
<body>
<x-dc>
<helmet>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Figtree:wght@400;500;600;700&amp;family=Fredoka:wght@500;600&amp;display=swap">
  <style>
    body { margin: 0; background: ${T.bg}; font-family: ${BODY}; color: ${T.ink}; -webkit-font-smoothing: antialiased; }
    a { color: ${T.coralInk}; } a:hover { color: ${T.coral}; }
    * { box-sizing: border-box; }
    p, h1, h2, h3 { margin: 0; text-wrap: pretty; }
  </style>
</helmet>
<div style="position: relative; width: 390px; height: 844px; overflow: hidden; background: ${T.bg}; display: flex; flex-direction: column;">
${body}
</div>
</x-dc>
${script}
</body>
</html>
`;

const tabBar = (active) => {
  const tabs = [['home', 'Painel'], ['cal', 'Escala'], ['people', 'Equipe'], ['clip', 'Ocorrências'], ['more', 'Mais']];
  return `<nav style="position: absolute; left: 0; right: 0; bottom: 0; height: 84px; padding: 8px 12px 26px; background: ${T.surface}; border-top: 1px solid ${T.line}; display: flex; justify-content: space-between; align-items: stretch;">
${tabs.map(([ic, label], i) => {
    const on = i === active;
    return `  <div style="flex: 1 1 0; min-height: 48px; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 3px; color: ${on ? T.coral : T.ink3};">
    <div style="width: 52px; height: 28px; border-radius: 14px; display: flex; align-items: center; justify-content: center; background: ${on ? T.coralSoft : 'transparent'};">${icon(I[ic], 22)}</div>
    <span style="font-size: 11px; font-weight: ${on ? 700 : 500};">${label}</span>
  </div>`;
  }).join('\n')}
</nav>`;
};

const h1 = (t, extra = '') => `<h1 style="font-family: ${DISPLAY}; font-weight: 600; font-size: 28px; line-height: 1.15; letter-spacing: -0.2px; ${extra}">${t}</h1>`;
const h2 = (t) => `<h2 style="font-family: ${DISPLAY}; font-weight: 500; font-size: 18px; line-height: 1.25;">${t}</h2>`;
const chip = (t, bg, fg, extra = '') => `<span style="display: inline-flex; align-items: center; gap: 6px; height: 26px; padding: 0 10px; border-radius: 13px; background: ${bg}; color: ${fg}; font-size: 12px; font-weight: 600; white-space: nowrap; ${extra}">${t}</span>`;
const btn = (t, primary = true, extra = '') => `<div style="flex: 1 1 0; height: 52px; border-radius: 16px; display: flex; align-items: center; justify-content: center; gap: 8px; font-weight: 700; font-size: 15px; ${primary ? `background: ${T.coral}; color: #FFFFFF; box-shadow: 0 6px 16px rgba(228, 112, 79, 0.28);` : `background: ${T.surface}; color: ${T.ink}; border: 1.5px solid ${T.line};`} ${extra}">${t}</div>`;
const card = (inner, extra = '') => `<div style="background: ${T.surface}; border-radius: 22px; padding: 16px; box-shadow: 0 1px 2px rgba(60, 40, 20, 0.05), 0 8px 24px rgba(60, 40, 20, 0.05); ${extra}">${inner}</div>`;
const avatar = (ini, bg, fg, size = 32) => `<div style="width: ${size}px; height: ${size}px; flex: none; border-radius: ${size / 2}px; background: ${bg}; color: ${fg}; display: flex; align-items: center; justify-content: center; font-family: ${DISPLAY}; font-weight: 600; font-size: ${Math.round(size * 0.4)}px;">${ini}</div>`;
const toggle = (on) => `<div style="width: 46px; height: 28px; flex: none; border-radius: 14px; padding: 3px; background: ${on ? T.teal : T.line}; display: flex; justify-content: ${on ? 'flex-end' : 'flex-start'};"><div style="width: 22px; height: 22px; border-radius: 11px; background: #FFFFFF; box-shadow: 0 1px 3px rgba(0,0,0,0.18);"></div></div>`;
const scroll = (inner, pb = 100) => `<div style="flex: 1 1 auto; padding: 56px 16px ${pb}px; display: flex; flex-direction: column; gap: 16px;">${inner}</div>`;

const PEOPLE = [
  ['AS', 'Ana', '#FCE9E2', T.coralInk], ['BL', 'Bruno', T.tealSoft, T.tealInk], ['CM', 'Carla', T.amberSoft, T.amber],
  ['DS', 'Diego', T.nightSoft, T.night], ['EP', 'Elisa', '#F1E6F7', '#7A3F99'], ['FN', 'Felipe', T.blueSoft, T.blue],
  ['GR', 'Gabi', '#E6F2DC', '#4C7A2A'], ['HA', 'Hugo', T.surface2, T.ink2],
];

// ---------- Painel (Main) ----------
const sev = (label, bg, fg, title, text, action) => card(`
  <div style="display: flex; flex-direction: column; gap: 8px;">
    <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px;">
      ${chip(`${icon(I.alert, 14, fg, 2)}${label}`, bg, fg)}
      <span style="font-size: 13px; font-weight: 700; color: ${T.coralInk};">${action}</span>
    </div>
    <p style="font-weight: 700; font-size: 15px; line-height: 1.3;">${title}</p>
    <p style="font-size: 13px; line-height: 1.45; color: ${T.ink2};">${text}</p>
  </div>`, 'padding: 14px 16px;');

const days = [['Seg', '12', 4, 4, true], ['Ter', '13', 5, 5], ['Qua', '14', 5, 5], ['Qui', '15', 5, 5], ['Sex', '16', 6, 6], ['Sáb', '17', 3, 4], ['Dom', '18', 3, 3]];
const main = page({ body: `
${scroll(`
  <div style="display: flex; align-items: center; justify-content: space-between; gap: 12px;">
    <div style="display: flex; flex-direction: column; gap: 4px;">
      <span style="font-size: 13px; font-weight: 600; color: ${T.ink2};">Loja Centro · Olá, Ana</span>
      ${h1('Outubro 2026')}
    </div>
    ${avatar('IS', T.coral, '#FFFFFF', 44)}
  </div>
  <div style="display: flex; gap: 8px;">
    ${chip(`${icon(I.check, 14, T.tealInk, 2.2)}Publicada · v3`, T.tealSoft, T.tealInk)}
  </div>
  ${card(`
    <div style="display: flex; flex-direction: column; gap: 12px;">
      <div style="display: flex; justify-content: space-between; align-items: baseline;">
        ${h2('Cobertura da semana')}
        <span style="font-size: 12px; color: ${T.ink3}; font-weight: 600;">12–18 out</span>
      </div>
      <div style="display: grid; grid-template-columns: repeat(7, minmax(0, 1fr)); gap: 6px;">
        ${days.map(([d, n, e, m, hol]) => {
          const ok = e >= m;
          return `<div style="display: flex; flex-direction: column; align-items: center; gap: 4px; padding: 8px 0; border-radius: 14px; background: ${ok ? T.surface2 : T.redSoft};">
          <span style="font-size: 11px; font-weight: 600; color: ${hol ? T.coral : T.ink3};">${d}</span>
          <span style="font-family: ${DISPLAY}; font-weight: 600; font-size: 17px; color: ${ok ? T.ink : T.red};">${n}</span>
          <span style="font-size: 11px; font-weight: 700; color: ${ok ? T.tealInk : T.red};">${e}/${m}</span>
        </div>`;
        }).join('')}
      </div>
      <span style="font-size: 12px; color: ${T.ink3};">Escalados / mínimo · segunda é feriado</span>
    </div>`)}
  <div style="display: flex; justify-content: space-between; align-items: baseline;">
    ${h2('Precisa de atenção')}
    <span style="font-size: 13px; font-weight: 600; color: ${T.ink3};">3 alertas</span>
  </div>
  ${sev('Bloqueio', T.redSoft, T.red, 'Diego descansaria só 9h entre jornadas', 'Sai 23h na sex, entra 8h no sáb (mín. 11h). Sugestão: tarde no sábado.', 'Resolver')}
  ${sev('Crítico', T.orangeSoft, T.orange, 'Sáb 17 · Tarde: falta 1 caixa', 'Gabi e Hugo estão disponíveis e não geram hora extra.', 'Ver opções')}
  ${sev('Atenção', T.amberSoft, T.amber, 'Carla trabalhou no feriado de 7/9', 'Tem prioridade de folga em 12/10, mas está escalada.', 'Trocar')}
`, 110)}
<div style="position: absolute; right: 16px; bottom: 100px; height: 56px; padding: 0 22px; border-radius: 28px; background: ${T.coral}; color: #FFFFFF; display: flex; align-items: center; gap: 8px; font-weight: 700; font-size: 15px; box-shadow: 0 10px 24px rgba(228, 112, 79, 0.38);">${icon(I.spark, 20, '#FFFFFF', 2)}Gerar novembro</div>
${tabBar(0)}` });

// ---------- Escala (grade semanal) ----------
const grid = [
  ['F', 'M', 'M', 'M', 'M', 'F', 'M'],
  ['T', 'O', 'O', 'T', 'T', 'T', 'F'],
  ['M', 'T', 'T', 'F', 'T', 'M', 'T'],
  ['F', 'N', 'N', 'N', 'N', 'M', 'F'],
  ['M', 'M', 'F', 'M', 'M', 'T', 'T'],
  ['T', 'F', 'M', 'T', 'M', 'F', 'M'],
  ['F', 'T', 'T', 'M', 'F', 'T', 'M'],
  ['T', 'M', 'M', 'F', 'T', 'M', 'F'],
];
const hours = ['44h', '28h', '44h', '44h', '44h', '36h', '36h', '44h'];
const escalaScript = `<script data-dc-script>
class Component extends DCLogic {
  renderVals() {
    const S = ${JSON.stringify(SHIFT)};
    const P = ${JSON.stringify(PEOPLE)};
    const G = ${JSON.stringify(grid)};
    const H = ${JSON.stringify(hours)};
    return {
      rows: G.map((r, i) => ({
        ini: P[i][0], name: P[i][1], abg: P[i][2], afg: P[i][3], hours: H[i],
        cells: r.map((c, j) => ({
          code: c, bg: S[c].bg, fg: S[c].fg,
          ring: (i === 3 && j === 5) ? '2px solid ${T.red}' : '2px solid transparent',
        })),
      })),
    };
  }
}
</script>`;
const dayHead = days.map(([d, n, , , hol]) => `<div style="display: flex; flex-direction: column; align-items: center; gap: 1px; padding: 4px 0; border-radius: 10px; background: ${hol ? T.coralSoft : 'transparent'};">
  <span style="font-size: 10px; font-weight: 600; color: ${hol ? T.coralInk : T.ink3};">${d}</span>
  <span style="font-size: 14px; font-weight: 700; color: ${hol ? T.coralInk : T.ink};">${n}</span>
</div>`).join('');
const cover = ['4/4', '5/5', '5/5', '5/5', '6/6', '3/4', '3/3'];
const escala = page({ script: escalaScript, body: `
<div style="flex: 1 1 auto; padding: 56px 16px 0; display: flex; flex-direction: column; gap: 14px;">
  <div style="display: flex; align-items: center; justify-content: space-between;">
    ${h1('Escala')}
    ${chip('Rascunho · v4', T.amberSoft, T.amber)}
  </div>
  <div style="display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 4px; padding: 4px; border-radius: 16px; background: ${T.surface2};">
    <div style="height: 36px; border-radius: 12px; background: ${T.surface}; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 14px; box-shadow: 0 1px 3px rgba(60,40,20,0.08);">Semana</div>
    <div style="height: 36px; display: flex; align-items: center; justify-content: center; font-weight: 600; font-size: 14px; color: ${T.ink2};">Quinzena</div>
    <div style="height: 36px; display: flex; align-items: center; justify-content: center; font-weight: 600; font-size: 14px; color: ${T.ink2};">Mês</div>
  </div>
  <div style="display: flex; align-items: center; justify-content: space-between;">
    <div style="width: 44px; height: 44px; border-radius: 22px; background: ${T.surface}; display: flex; align-items: center; justify-content: center; color: ${T.ink2};">${icon(I.left, 20)}</div>
    <span style="font-family: ${DISPLAY}; font-weight: 500; font-size: 17px;">12 – 18 de outubro</span>
    <div style="width: 44px; height: 44px; border-radius: 22px; background: ${T.surface}; display: flex; align-items: center; justify-content: center; color: ${T.ink2};">${icon(I.right, 20)}</div>
  </div>
  <div style="display: flex; gap: 6px; flex-wrap: wrap;">
    ${['M', 'T', 'N', 'F', 'O'].map((k) => chip(`<span style="width: 8px; height: 8px; border-radius: 4px; background: ${SHIFT[k].fg};"></span>${SHIFT[k].label}`, SHIFT[k].bg, SHIFT[k].fg, 'height: 24px; font-size: 11px;')).join('')}
  </div>
  ${card(`
    <div style="display: flex; flex-direction: column; gap: 6px;">
      <div style="display: grid; grid-template-columns: 96px repeat(7, minmax(0, 1fr)); gap: 4px; align-items: center;">
        <span style="font-size: 11px; font-weight: 600; color: ${T.ink3};">Equipe</span>
        ${dayHead}
      </div>
      <sc-for list="{{ rows }}" as="r" hint-placeholder-count="8">
        <div style="display: grid; grid-template-columns: 96px repeat(7, minmax(0, 1fr)); gap: 4px; align-items: center;">
          <div style="display: flex; align-items: center; gap: 6px; min-width: 0;">
            <div style="width: 26px; height: 26px; flex: none; border-radius: 13px; background: {{ r.abg }}; color: {{ r.afg }}; display: flex; align-items: center; justify-content: center; font-size: 10px; font-weight: 700;">{{ r.ini }}</div>
            <div style="display: flex; flex-direction: column; min-width: 0;">
              <span style="font-size: 13px; font-weight: 600; line-height: 1.1;">{{ r.name }}</span>
              <span style="font-size: 10px; color: ${T.ink3};">{{ r.hours }}</span>
            </div>
          </div>
          <sc-for list="{{ r.cells }}" as="c" hint-placeholder-count="7">
            <div style="height: 34px; border-radius: 10px; background: {{ c.bg }}; color: {{ c.fg }}; outline: {{ c.ring }}; outline-offset: -2px; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 13px;">{{ c.code }}</div>
          </sc-for>
        </div>
      </sc-for>
      <div style="display: grid; grid-template-columns: 96px repeat(7, minmax(0, 1fr)); gap: 4px; align-items: center; padding-top: 6px; border-top: 1px solid ${T.line};">
        <span style="font-size: 11px; font-weight: 600; color: ${T.ink3};">Cobertura</span>
        ${cover.map((c) => `<span style="text-align: center; font-size: 11px; font-weight: 700; color: ${c === '3/4' ? T.red : T.tealInk};">${c}</span>`).join('')}
      </div>
    </div>`, 'padding: 12px 10px;')}
</div>
<div style="position: absolute; left: 0; right: 0; bottom: 84px; padding: 16px 16px 14px; background: ${T.surface}; border-radius: 26px 26px 0 0; box-shadow: 0 -10px 30px rgba(60, 40, 20, 0.12); display: flex; flex-direction: column; gap: 10px;">
  <div style="width: 40px; height: 4px; border-radius: 2px; background: ${T.line}; align-self: center;"></div>
  <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px;">
    <div style="display: flex; flex-direction: column; gap: 2px;">
      <span style="font-weight: 700; font-size: 16px;">Diego · Sáb 17</span>
      <span style="font-size: 13px; color: ${T.ink2};">Manhã · 08:00–16:20 · Atendente</span>
    </div>
    ${chip('Bloqueio', T.redSoft, T.red)}
  </div>
  <p style="font-size: 13px; line-height: 1.4; color: ${T.ink2};">Descanso de 9h desde sexta (mínimo 11h). Passando para a tarde, a cobertura fica completa e ninguém faz hora extra.</p>
  <div style="display: flex; gap: 8px;">
    ${btn(`${icon(I.swap, 18, '#FFFFFF', 2)}Mudar para tarde`, true, 'height: 46px; font-size: 14px;')}
    ${btn('Dar folga', false, 'height: 46px; font-size: 14px;')}
  </div>
</div>
${tabBar(1)}` });

// ---------- Ocorrência / remanejamento ----------
const steps = [['Registro', 'done'], ['Afetados', 'done'], ['Substitutos', 'on'], ['Aprovar', '']];
const sub = (ini, name, abg, afg, line, tag, disabled = false, selected = false) => `
  <div style="display: flex; align-items: center; gap: 12px; padding: 12px; border-radius: 18px; border: 1.5px solid ${selected ? T.teal : T.line}; background: ${selected ? '#F3FBF9' : T.surface}; opacity: ${disabled ? 0.55 : 1};">
    ${avatar(ini, abg, afg, 40)}
    <div style="flex: 1 1 auto; display: flex; flex-direction: column; gap: 3px; min-width: 0;">
      <span style="font-weight: 700; font-size: 15px;">${name}</span>
      <span style="font-size: 12px; color: ${T.ink2}; line-height: 1.35;">${line}</span>
    </div>
    ${tag}
  </div>`;
const ocorrencia = page({ body: `
${scroll(`
  <div style="display: flex; align-items: center; gap: 8px; margin-left: -8px;">
    <div style="width: 44px; height: 44px; display: flex; align-items: center; justify-content: center;">${icon(I.back, 24)}</div>
    ${h1('Remanejar', 'font-size: 24px;')}
  </div>
  <div style="display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 6px;">
    ${steps.map(([s, st]) => `<div style="display: flex; flex-direction: column; gap: 6px;">
      <div style="height: 5px; border-radius: 3px; background: ${st ? T.coral : T.line};"></div>
      <span style="font-size: 11px; font-weight: ${st === 'on' ? 700 : 500}; color: ${st === 'on' ? T.ink : T.ink3};">${s}</span>
    </div>`).join('')}
  </div>
  ${card(`
    <div style="display: flex; flex-direction: column; gap: 12px;">
      <div style="display: flex; align-items: center; gap: 12px;">
        ${avatar('BL', T.tealSoft, T.tealInk, 44)}
        <div style="flex: 1 1 auto; display: flex; flex-direction: column; gap: 2px;">
          <span style="font-weight: 700; font-size: 16px;">Bruno Lima</span>
          <span style="font-size: 13px; color: ${T.ink2};">Atestado · 14 e 15 de outubro</span>
        </div>
        ${chip('Aprovado', T.tealSoft, T.tealInk)}
      </div>
      <div style="display: flex; align-items: center; gap: 8px; padding: 10px 12px; border-radius: 14px; background: ${T.surface2}; color: ${T.ink2}; font-size: 12px;">
        ${icon(I.lock, 16, T.ink2)}<span>Só o motivo administrativo fica salvo; detalhes médicos não.</span>
      </div>
      <div style="display: flex; flex-direction: column; gap: 8px;">
        <div style="display: flex; justify-content: space-between; font-size: 14px;"><span>Qua 14 · Manhã · Caixa</span><span style="font-weight: 700; color: ${T.tealInk};">Gabi</span></div>
        <div style="display: flex; justify-content: space-between; font-size: 14px;"><span style="font-weight: 700;">Qui 15 · Manhã · Caixa</span><span style="font-weight: 700; color: ${T.coralInk};">Escolhendo…</span></div>
      </div>
    </div>`)}
  <div style="display: flex; justify-content: space-between; align-items: baseline;">
    ${h2('Quem pode cobrir qui 15')}
    <span style="font-size: 12px; color: ${T.ink3}; font-weight: 600;">3 pessoas</span>
  </div>
  <div style="display: flex; flex-direction: column; gap: 10px;">
    ${sub('HA', 'Hugo Alves', T.surface2, T.ink2, 'Caixa · 36h de 44h na semana · sem hora extra', chip('Melhor opção', T.tealSoft, T.tealInk), false, true)}
    ${sub('GR', 'Gabi Rocha', '#E6F2DC', '#4C7A2A', 'Caixa · já cobre qua 14 · gera 2h extras', chip('+2h', T.amberSoft, T.amber))}
    ${sub('EP', 'Elisa Prado', '#F1E6F7', '#7A3F99', 'Sai às 22h na quarta: descanso de 10h', chip('Bloqueio', T.redSoft, T.red), true)}
  </div>
  <div style="display: flex; align-items: center; justify-content: space-between; padding: 12px 14px; border-radius: 16px; background: ${T.tealSoft}; color: ${T.tealInk}; font-size: 13px; font-weight: 600;">
    <span>Cobertura 100%</span><span>0h extras</span><span>2 pessoas afetadas</span>
  </div>
  <div style="display: flex; gap: 10px;">
    ${btn('Aprovar e criar v4')}
  </div>
`, 24)}` });

// ---------- Funcionário ----------
const oct = []; // Outubro 2026: dia 1 = quinta
const pattern = ['T', 'T', 'F', 'T', 'M', 'T', 'T', 'T', 'M', 'F']; // ilustrativo
for (let i = 0; i < 3; i++) oct.push(null);
for (let d = 1; d <= 31; d++) {
  let c = pattern[(d - 1) % pattern.length];
  if (d === 12) c = 'F';
  if (d === 18 || d === 4) c = 'F';
  oct.push({ d, c, hol: d === 12 });
}
while (oct.length % 7) oct.push(null);
const funcionario = page({ body: `
${scroll(`
  <div style="display: flex; align-items: center; justify-content: space-between; margin-left: -8px;">
    <div style="width: 44px; height: 44px; display: flex; align-items: center; justify-content: center;">${icon(I.back, 24)}</div>
    <span style="font-size: 14px; font-weight: 700; color: ${T.coralInk};">Editar</span>
  </div>
  <div style="display: flex; align-items: center; gap: 14px;">
    ${avatar('CM', T.amberSoft, T.amber, 64)}
    <div style="display: flex; flex-direction: column; gap: 4px;">
      ${h1('Carla Mendes', 'font-size: 24px;')}
      <span style="font-size: 14px; color: ${T.ink2};">Atendente · 6x1 · 44h por semana</span>
    </div>
  </div>
  <div style="display: flex; gap: 6px; flex-wrap: wrap;">
    ${chip('Turno principal: tarde', T.tealSoft, T.tealInk)}
    ${chip('Fim de semana: sim', T.surface2, T.ink2)}
  </div>
  <div style="display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px;">
    ${[['176h', 'no mês'], ['2', 'domingos de folga'], ['1', 'feriado trabalhado']].map(([v, l]) => card(`<div style="display: flex; flex-direction: column; gap: 2px;"><span style="font-family: ${DISPLAY}; font-weight: 600; font-size: 22px;">${v}</span><span style="font-size: 12px; color: ${T.ink2}; line-height: 1.25;">${l}</span></div>`, 'padding: 12px; border-radius: 18px;')).join('')}
  </div>
  ${card(`
    <div style="display: flex; flex-direction: column; gap: 10px;">
      <div style="display: flex; justify-content: space-between; align-items: center;">
        ${h2('Outubro')}
        <div style="display: flex; gap: 4px; color: ${T.ink2};">${icon(I.left, 20)}${icon(I.right, 20)}</div>
      </div>
      <div style="display: grid; grid-template-columns: repeat(7, minmax(0, 1fr)); gap: 5px;">
        ${['S', 'T', 'Q', 'Q', 'S', 'S', 'D'].map((d) => `<span style="text-align: center; font-size: 11px; font-weight: 600; color: ${T.ink3};">${d}</span>`).join('')}
        ${oct.map((x) => x ? `<div style="height: 34px; border-radius: 10px; background: ${SHIFT[x.c].bg}; color: ${SHIFT[x.c].fg}; outline: ${x.hol ? `2px solid ${T.coral}` : 'none'}; outline-offset: -2px; display: flex; flex-direction: column; align-items: center; justify-content: center; line-height: 1.05;">
          <span style="font-size: 13px; font-weight: 700;">${x.d}</span><span style="font-size: 9px; font-weight: 600;">${x.c === 'F' ? 'folga' : x.c}</span></div>` : '<div></div>').join('')}
      </div>
      <span style="font-size: 12px; color: ${T.ink2};">Folga no feriado de 12/10: trabalhou em 7/9.</span>
    </div>`)}
  ${card(`
    <div style="display: flex; flex-direction: column; gap: 10px;">
      ${h2('Disponibilidade')}
      ${[['Seg a sex', '12:00 – 22:00', 'Contrato'], ['Sábado', '08:00 – 18:00', 'Prefere manhã'], ['Domingo', '12:00 – 20:00', 'Sem preferência']].map(([d, h, t]) => `<div style="display: flex; align-items: center; justify-content: space-between; gap: 8px; min-height: 28px;">
        <span style="font-size: 14px; font-weight: 600; width: 84px;">${d}</span>
        <span style="font-size: 14px; color: ${T.ink2}; flex: 1 1 auto;">${h}</span>
        <span style="font-size: 12px; color: ${T.ink3};">${t}</span>
      </div>`).join('')}
    </div>`)}
`, 24)}` });

// ---------- Onboarding: funcionamento ----------
const hoursRows = [
  ['Segunda', true, ['08:00 – 22:00']], ['Terça', true, ['08:00 – 22:00']], ['Quarta', true, ['08:00 – 22:00']],
  ['Quinta', true, ['08:00 – 22:00']], ['Sexta', true, ['08:00 – 12:00', '14:00 – 23:30']], ['Sábado', true, ['09:00 – 20:00']], ['Domingo', true, ['10:00 – 18:00']],
];
const stepper = (label, val) => `<div style="display: flex; align-items: center; justify-content: space-between; gap: 12px;">
  <span style="font-size: 14px; font-weight: 600;">${label}</span>
  <div style="display: flex; align-items: center; gap: 4px; padding: 3px; border-radius: 14px; background: ${T.surface2};">
    <div style="width: 36px; height: 36px; border-radius: 11px; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 18px; color: ${T.ink2};">–</div>
    <span style="min-width: 54px; text-align: center; font-weight: 700; font-size: 14px;">${val}</span>
    <div style="width: 36px; height: 36px; border-radius: 11px; background: ${T.surface}; display: flex; align-items: center; justify-content: center; color: ${T.ink};">${icon(I.plus, 16)}</div>
  </div>
</div>`;
const onboarding = page({ body: `
${scroll(`
  <div style="display: flex; flex-direction: column; gap: 10px;">
    <div style="display: flex; justify-content: space-between; font-size: 13px; font-weight: 600; color: ${T.ink2};"><span>Passo 2 de 8</span><span>Pular por agora</span></div>
    <div style="height: 6px; border-radius: 3px; background: ${T.line};"><div style="width: 25%; height: 6px; border-radius: 3px; background: ${T.coral};"></div></div>
  </div>
  <div style="display: flex; flex-direction: column; gap: 6px;">
    ${h1('Quando a loja abre?')}
    <p style="font-size: 14px; line-height: 1.45; color: ${T.ink2};">Usamos esses horários para montar os turnos e saber quantas pessoas cada dia precisa.</p>
  </div>
  ${card(`
    <div style="display: flex; flex-direction: column;">
      ${hoursRows.map(([d, on, ps], i) => `<div style="display: flex; align-items: center; gap: 12px; min-height: 46px; padding: 5px 0; ${i ? `border-top: 1px solid ${T.line};` : ''}">
        <span style="width: 74px; font-weight: 600; font-size: 15px;">${d}</span>
        <div style="flex: 1 1 auto; display: flex; flex-direction: column; gap: 4px; align-items: flex-start;">
          ${ps.map((p) => `<span style="padding: 4px 10px; border-radius: 10px; background: ${T.surface2}; font-size: 13px; font-weight: 600;">${p}</span>`).join('')}
        </div>
        ${toggle(on)}
      </div>`).join('')}
      <div style="display: flex; align-items: center; gap: 6px; padding-top: 10px; color: ${T.coralInk}; font-size: 14px; font-weight: 700;">${icon(I.plus, 16, T.coralInk, 2.2)}Adicionar exceção por data</div>
    </div>`, 'padding: 8px 16px 14px;')}
  ${card(`<div style="display: flex; flex-direction: column; gap: 12px;">
    ${stepper('Preparação antes de abrir', '30 min')}
    ${stepper('Fechamento depois de atender', '30 min')}
  </div>`)}
  <div style="display: flex; gap: 10px; margin-top: auto;">
    ${btn('Voltar', false, 'flex: 0 0 110px;')}
    ${btn('Continuar')}
  </div>
`, 34)}` });

// ---------- Feriados ----------
const hol = (dd, mm, wd, name, kind, on, note) => `<div style="display: flex; align-items: center; gap: 12px; padding: 12px 0;">
  <div style="width: 50px; height: 54px; flex: none; border-radius: 16px; background: ${on ? T.coralSoft : T.surface2}; color: ${on ? T.coralInk : T.ink3}; display: flex; flex-direction: column; align-items: center; justify-content: center; line-height: 1.05;">
    <span style="font-family: ${DISPLAY}; font-weight: 600; font-size: 20px;">${dd}</span>
    <span style="font-size: 11px; font-weight: 700;">${mm}</span>
  </div>
  <div style="flex: 1 1 auto; display: flex; flex-direction: column; gap: 3px; min-width: 0;">
    <span style="font-weight: 700; font-size: 15px; line-height: 1.25; color: ${on ? T.ink : T.ink3};">${name}</span>
    <span style="font-size: 12px; color: ${T.ink3};">${wd} · ${kind}</span>
    ${note ? `<span style="font-size: 12px; color: ${T.tealInk}; font-weight: 600;">${note}</span>` : ''}
  </div>
  ${toggle(on)}
</div>`;
const feriados = page({ body: `
${scroll(`
  <div style="display: flex; align-items: center; justify-content: space-between; margin-left: -8px;">
    <div style="display: flex; align-items: center; gap: 4px;">
      <div style="width: 44px; height: 44px; display: flex; align-items: center; justify-content: center;">${icon(I.back, 24)}</div>
      ${h1('Feriados', 'font-size: 24px;')}
    </div>
    ${chip('2026', T.surface, T.ink, `height: 34px; padding: 0 14px; font-size: 14px; border: 1.5px solid ${T.line};`)}
  </div>
  <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px; font-size: 12px; color: ${T.ink2};">
    <span>Nacionais pela BrasilAPI · atualizado hoje</span>
    <span style="display: flex; align-items: center; gap: 4px; font-weight: 700; color: ${T.coralInk};">${icon(I.refresh, 14, T.coralInk, 2)}Atualizar</span>
  </div>
  ${card(`<div style="display: flex; align-items: center; gap: 12px;">
    <div style="width: 40px; height: 40px; flex: none; border-radius: 14px; background: ${T.tealSoft}; color: ${T.tealInk}; display: flex; align-items: center; justify-content: center;">${icon(I.swap, 20)}</div>
    <div style="flex: 1 1 auto; display: flex; flex-direction: column; gap: 2px;">
      <span style="font-weight: 700; font-size: 15px;">Rodízio de feriados</span>
      <span style="font-size: 12px; line-height: 1.4; color: ${T.ink2};">Quem trabalhou no último feriado tem prioridade para folgar no próximo.</span>
    </div>
    ${toggle(true)}
  </div>`, `background: #F3FBF9;`)}
  <div style="display: flex; justify-content: space-between; align-items: baseline;">
    ${h2('Próximos')}
    <span style="font-size: 12px; color: ${T.ink3}; font-weight: 600;">Desligado = dia normal</span>
  </div>
  ${card(`<div style="display: flex; flex-direction: column;">
    ${hol('12', 'OUT', 'Segunda', 'Nossa Senhora Aparecida', 'Nacional', true, 'Folgam Carla e Diego (trabalharam em 7/9)')}
    <div style="height: 1px; background: ${T.line};"></div>
    ${hol('28', 'OUT', 'Quarta', 'Dia do Servidor Público', 'Adicionado por você', false, '')}
    <div style="height: 1px; background: ${T.line};"></div>
    ${hol('2', 'NOV', 'Segunda', 'Finados', 'Nacional', true, '')}
    <div style="height: 1px; background: ${T.line};"></div>
    ${hol('20', 'NOV', 'Sexta', 'Consciência Negra', 'Nacional', true, '')}
    <div style="height: 1px; background: ${T.line};"></div>
    ${hol('8', 'DEZ', 'Terça', 'Nossa Senhora da Conceição', 'Municipal · adicionado por você', true, '')}
  </div>`, 'padding: 4px 16px;')}
  ${btn(`${icon(I.plus, 18, T.ink, 2.2)}Adicionar feriado`, false, 'flex: none;')}
`, 24)}` });

// ---------- Exportar ----------
const opt = (title, desc, on, ic) => `<div style="display: flex; align-items: center; gap: 12px; padding: 14px; border-radius: 18px; border: 1.5px solid ${on ? T.coral : T.line}; background: ${on ? '#FFF8F5' : T.surface};">
  <div style="width: 40px; height: 40px; flex: none; border-radius: 14px; background: ${on ? T.coralSoft : T.surface2}; color: ${on ? T.coralInk : T.ink3}; display: flex; align-items: center; justify-content: center;">${icon(I[ic], 20)}</div>
  <div style="flex: 1 1 auto; display: flex; flex-direction: column; gap: 2px;">
    <span style="font-weight: 700; font-size: 15px;">${title}</span>
    <span style="font-size: 12px; color: ${T.ink2}; line-height: 1.35;">${desc}</span>
  </div>
  <div style="width: 26px; height: 26px; flex: none; border-radius: 8px; background: ${on ? T.coral : T.surface}; border: 1.5px solid ${on ? T.coral : T.line}; display: flex; align-items: center; justify-content: center;">${on ? icon(I.check, 16, '#FFFFFF', 2.6) : ''}</div>
</div>`;
const prevRows = [['Qui 01/10', '12:00', '16:00–17:00', '20:20', '7h20'], ['Sex 02/10', '12:00', '16:00–17:00', '20:20', '7h20'], ['Sáb 03/10', 'Folga', '', '', '—']];
const exportar = page({ body: `
${scroll(`
  <div style="display: flex; align-items: center; gap: 4px; margin-left: -8px;">
    <div style="width: 44px; height: 44px; display: flex; align-items: center; justify-content: center;">${icon(I.back, 24)}</div>
    ${h1('Exportar', 'font-size: 24px;')}
  </div>
  ${card(`<div style="display: flex; align-items: center; justify-content: space-between; gap: 8px;">
    <div style="display: flex; flex-direction: column; gap: 2px;">
      <span style="font-weight: 700; font-size: 16px;">Outubro 2026</span>
      <span style="font-size: 12px; color: ${T.ink2};">Versão 3 · publicada em 28/09</span>
    </div>
    <span style="font-size: 13px; font-weight: 700; color: ${T.coralInk};">Trocar</span>
  </div>`)}
  <div style="display: flex; flex-direction: column; gap: 10px;">
    ${opt('Planilha de cada funcionário', '8 abas, com entrada, intervalo, saída e totais', true, 'people')}
    ${opt('Planilha consolidada', 'Calendário da equipe com filtros por setor e função', true, 'sheet')}
    ${opt('Relatório de validação', 'Regras verificadas, alertas aceitos e justificativas', false, 'check')}
  </div>
  ${card(`<div style="display: flex; flex-direction: column; gap: 8px;">
    <span style="font-size: 12px; font-weight: 700; color: ${T.ink3};">PRÉVIA · CARLA MENDES</span>
    <div style="display: grid; grid-template-columns: 1.3fr 0.8fr 1.4fr 0.8fr 0.7fr; gap: 0; font-size: 11px; border-radius: 12px; overflow: hidden; border: 1px solid ${T.line};">
      ${['Data', 'Entrada', 'Intervalo', 'Saída', 'Total'].map((h) => `<span style="padding: 7px 6px; background: ${T.surface2}; font-weight: 700;">${h}</span>`).join('')}
      ${prevRows.map((r) => r.map((c) => `<span style="padding: 7px 6px; border-top: 1px solid ${T.line}; color: ${r[1] === 'Folga' ? T.ink3 : T.ink};">${c}</span>`).join('')).join('')}
    </div>
  </div>`, 'padding: 14px;')}
  <div style="display: flex; gap: 6px; flex-wrap: wrap;">
    ${chip(`${icon(I.check, 14, T.tealInk, 2.4)}Excel (.xlsx)`, T.tealSoft, T.tealInk, 'height: 32px;')}
    ${chip('PDF', T.surface2, T.ink3, 'height: 32px;')}
    ${chip('WhatsApp', T.surface2, T.ink3, 'height: 32px;')}
    <span style="align-self: center; font-size: 12px; color: ${T.ink3};">PDF e WhatsApp em breve</span>
  </div>
  ${btn(`${icon(I.share, 20, '#FFFFFF', 2)}Gerar e compartilhar`, true, 'flex: none;')}
  <p style="font-size: 12px; line-height: 1.4; color: ${T.ink3}; text-align: center;">A escala planejada não comprova a jornada realizada.</p>
`, 24)}` });

const files = { Main: main, Escala: escala, Remanejar: ocorrencia, Funcionario: funcionario, Onboarding: onboarding, Feriados: feriados, Exportar: exportar };
for (const [k, v] of Object.entries(files)) writeFileSync(new URL(`${k}.dc.html`, OUT), v);

const W = 390, H = 844, GX = 470, GY = H + 140;
const layout = [['Onboarding', 'Início · Funcionamento'], ['Main', 'Painel'], ['Escala', 'Escala da semana'], ['Remanejar', 'Ocorrência e remanejamento'],
  ['Funcionario', 'Funcionário'], ['Feriados', 'Feriados'], ['Exportar', 'Exportar']];
const canvas = {
  artboards: layout.map(([f, title], i) => ({ file: `${f}.dc.html`, title, x: (i % 4) * GX, y: Math.floor(i / 4) * GY, w: W, h: H })),
  annotations: [{ id: 'direcao', x: 0, y: -170, w: 520, text: 'Folguinha · direção "suave e amigável"\nCreme quente + coral (ação) + verde-água (ok). Cores por turno: manhã âmbar, tarde verde-água, noite lilás, folga neutra, ocorrência rosa.\nFontes: Fredoka (títulos) + Figtree (texto). Dados de exemplo.' }],
  launch: { view: 'canvas' },
};
writeFileSync(new URL('canvas.json', OUT), JSON.stringify(canvas, null, 2));
console.log('ok', Object.keys(files).length, 'artboards');
