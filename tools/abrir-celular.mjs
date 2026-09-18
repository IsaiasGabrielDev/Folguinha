// Abre o app numa janela do Edge simulando um celular (tela, toque e user agent). Uso:
//   node tools/abrir-celular.mjs [url] [largura] [altura]
// A simulação fica ativa enquanto este processo estiver rodando; fechar a janela encerra.
import { spawn } from 'node:child_process';
import { existsSync, mkdirSync } from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';

const url = process.argv[2] ?? 'http://localhost:5180/';
const largura = Number(process.argv[3] ?? 390);
const altura = Number(process.argv[4] ?? 844);
const exe = [process.env.NAVEGADOR, 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Google/Chrome/Application/chrome.exe']
    .filter(Boolean).find(existsSync);
if (!exe) throw new Error('Edge/Chrome não encontrado.');

const perfil = join(homedir(), '.folguinha-celular'); // perfil próprio: os dados do teste ficam guardados entre aberturas
mkdirSync(perfil, { recursive: true });
const porta = 9222 + Math.floor(Math.random() * 300);
const navegador = spawn(exe, [
    `--remote-debugging-port=${porta}`, `--user-data-dir=${perfil}`, '--no-first-run',
    `--window-size=${largura + 16},${altura + 88}`, `--app=${url}`,
], { stdio: 'ignore' });
navegador.on('exit', () => process.exit(0));

const dormir = ms => new Promise(r => setTimeout(r, ms));
let alvo;
for (let i = 0; i < 60 && !alvo; i++) {
    try { alvo = (await (await fetch(`http://127.0.0.1:${porta}/json`)).json()).find(t => t.type === 'page'); }
    catch { /* ainda abrindo */ }
    if (!alvo) await dormir(250);
}
if (!alvo) throw new Error('Não consegui conectar à janela.');

const ws = new WebSocket(alvo.webSocketDebuggerUrl);
await new Promise(r => ws.addEventListener('open', r, { once: true }));
let id = 0;
const cdp = (method, params = {}) => ws.send(JSON.stringify({ id: ++id, method, params }));

cdp('Emulation.setDeviceMetricsOverride', { width: largura, height: altura, deviceScaleFactor: 3, mobile: true });
cdp('Emulation.setTouchEmulationEnabled', { enabled: true, maxTouchPoints: 5 });
cdp('Emulation.setEmitTouchEventsForMouse', { enabled: true, configuration: 'mobile' });
cdp('Network.setUserAgentOverride', {
    userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1',
    platform: 'iPhone',
});
cdp('Page.reload', {});
console.log(`Janela de celular aberta (${largura}×${altura}) em ${url}. Feche a janela para encerrar.`);
ws.addEventListener('close', () => process.exit(0));
