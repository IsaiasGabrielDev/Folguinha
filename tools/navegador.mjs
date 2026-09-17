// Controla o Edge/Chrome headless via DevTools Protocol para testes de fumaça do PWA.
// Uso: node tools/navegador.mjs <url> [--esperar "texto"] [--script arquivo.mjs] [--foto saida.png] [--largura 390 --altura 844] [--escala 2] [--reduzir 0.5]
import { spawn } from 'node:child_process';
import { mkdtempSync, writeFileSync, existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const args = process.argv.slice(2);
const opcao = (nome, padrao) => {
    const i = args.indexOf(`--${nome}`);
    return i >= 0 ? args[i + 1] : padrao;
};
const url = args[0];
const esperar = opcao('esperar');
const roteiro = opcao('script');
const foto = opcao('foto');
const largura = Number(opcao('largura', 390));
const altura = Number(opcao('altura', 844));
const limite = Number(opcao('limite', 90)) * 1000;
const escala = Number(opcao('escala', 2));
const reduzir = opcao('reduzir'); // fator da captura (ex.: 0.375 para 512→192)

const candidatos = [
    process.env.NAVEGADOR,
    'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
    'C:/Program Files/Google/Chrome/Application/chrome.exe',
].filter(Boolean);
const exe = candidatos.find(existsSync);
if (!exe) throw new Error('Edge/Chrome não encontrado (defina NAVEGADOR).');

const porta = 9300 + Math.floor(Math.random() * 500);
const perfil = mkdtempSync(join(tmpdir(), 'folguinha-nav-'));
const navegador = spawn(exe, [
    '--headless=new', '--disable-gpu', '--no-first-run', `--remote-debugging-port=${porta}`,
    `--user-data-dir=${perfil}`, `--window-size=${largura},${altura}`,
    ...(process.env.NAVEGADOR_ARGS?.split('|').filter(Boolean) ?? []), 'about:blank',
], { stdio: 'ignore' });

const dormir = ms => new Promise(r => setTimeout(r, ms));
let alvo;
for (let i = 0; i < 50 && !alvo; i++) {
    try {
        const lista = await (await fetch(`http://127.0.0.1:${porta}/json`)).json();
        alvo = lista.find(t => t.type === 'page');
    } catch { await dormir(200); }
}
if (!alvo) throw new Error('Não conectou ao navegador.');

const ws = new WebSocket(alvo.webSocketDebuggerUrl);
await new Promise(r => ws.addEventListener('open', r, { once: true }));
let id = 0;
const pendentes = new Map();
const erros = [];
ws.addEventListener('message', ({ data }) => {
    const msg = JSON.parse(data);
    if (msg.id && pendentes.has(msg.id)) { pendentes.get(msg.id)(msg); pendentes.delete(msg.id); }
    if (msg.method === 'Runtime.exceptionThrown') erros.push(msg.params.exceptionDetails.exception?.description ?? msg.params.exceptionDetails.text);
    if (msg.method === 'Runtime.consoleAPICalled' && msg.params.type === 'error')
        erros.push(msg.params.args.map(a => a.value ?? a.description).join(' '));
});
const cdp = (method, params = {}) => new Promise((ok, falha) => {
    const n = ++id;
    pendentes.set(n, m => (m.error ? falha(new Error(`${method}: ${m.error.message}`)) : ok(m.result)));
    ws.send(JSON.stringify({ id: n, method, params }));
});

const avaliar = async expr => {
    const r = await cdp('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true });
    if (r.exceptionDetails) throw new Error(r.exceptionDetails.exception?.description ?? r.exceptionDetails.text);
    return r.result.value;
};
const texto = () => avaliar("document.body?.innerText ?? ''");
const esperarTexto = async (t, ms = limite) => {
    const fim = Date.now() + ms;
    while (Date.now() < fim) {
        if ((await texto()).includes(t)) return;
        await dormir(250);
    }
    throw new Error(`Tempo esgotado esperando "${t}". Tela:\n${(await texto()).slice(0, 1500)}`);
};
const clicar = async rotulo => {
    const ok = await avaliar(`(() => {
        const alvos = [...document.querySelectorAll('button, a, [role=button], label, [role=tab], summary')];
        const el = alvos.find(e => e.innerText.trim() === ${JSON.stringify(rotulo)})
              ?? alvos.find(e => e.innerText.trim().includes(${JSON.stringify(rotulo)})
                            || e.getAttribute('aria-label') === ${JSON.stringify(rotulo)});
        if (!el) return false;
        el.scrollIntoView({ block: 'center' });
        el.click();
        return true;
    })()`);
    if (!ok) throw new Error(`Não achei "${rotulo}" para clicar.`);
    await dormir(300);
};
const preencher = async (rotulo, valor) => {
    const ok = await avaliar(`(() => {
        const label = [...document.querySelectorAll('label')].find(l => l.innerText.trim().startsWith(${JSON.stringify(rotulo)}));
        const campo = label?.control ?? label?.querySelector('input,select,textarea');
        if (!campo) return false;
        const proto = campo instanceof HTMLSelectElement ? HTMLSelectElement : campo instanceof HTMLTextAreaElement ? HTMLTextAreaElement : HTMLInputElement;
        if (campo.type === 'checkbox') { if (campo.checked !== ${JSON.stringify(valor)}) campo.click(); return true; }
        Object.getOwnPropertyDescriptor(proto.prototype, 'value').set.call(campo, ${JSON.stringify(valor)});
        campo.dispatchEvent(new Event('input', { bubbles: true }));
        campo.dispatchEvent(new Event('change', { bubbles: true }));
        return true;
    })()`);
    if (!ok) throw new Error(`Não achei o campo "${rotulo}".`);
    await dormir(200);
};
const fotografar = async caminho => {
    const clip = reduzir ? { x: 0, y: 0, width: largura, height: altura, scale: Number(reduzir) } : undefined;
    const { data } = await cdp('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false, clip });
    writeFileSync(caminho, Buffer.from(data, 'base64'));
};

let codigo = 0;
try {
    await cdp('Page.enable');
    await cdp('Runtime.enable');
    await cdp('Emulation.setDeviceMetricsOverride', { width: largura, height: altura, deviceScaleFactor: escala, mobile: largura < 700 });
    await cdp('Page.navigate', { url });
    for (let i = 0; i < 40; i++) {
        try { if (await avaliar('document.readyState') === 'complete') break; } catch { /* página trocando */ }
        await dormir(250);
    }
    if (esperar) await esperarTexto(esperar);
    if (roteiro) {
        const mod = await import(pathToFileURL(resolve(roteiro)).href);
        await mod.default({ avaliar, texto, esperarTexto, clicar, preencher, fotografar, dormir, cdp, url });
    }
    if (foto) await fotografar(foto);
    console.log((await texto()).slice(0, 4000));
} catch (e) {
    console.error('FALHA:', e.message);
    codigo = 1;
} finally {
    if (erros.length) console.error('ERROS NO CONSOLE:\n' + erros.join('\n'));
    ws.close();
    navegador.kill();
}
process.exit(codigo || (erros.length ? 2 : 0));
