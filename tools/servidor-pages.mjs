// Servidor estático que imita o GitHub Pages (404.html para rotas desconhecidas). Uso:
//   node tools/servidor-pages.mjs <pasta-raiz> [porta]
// Ex.: copie publicado/wwwroot para <raiz>/folguinha e abra http://localhost:5191/folguinha/
import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { extname, join, normalize, sep } from 'node:path';

const raiz = normalize(process.argv[2] ?? '.');
const porta = Number(process.argv[3] ?? 5191);
const tipos = {
    '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css',
    '.json': 'application/json', '.wasm': 'application/wasm', '.png': 'image/png', '.svg': 'image/svg+xml',
    '.webmanifest': 'application/manifest+json', '.dat': 'application/octet-stream', '.ico': 'image/x-icon',
};

createServer(async (pedido, resposta) => {
    const caminho = decodeURIComponent(new URL(pedido.url, 'http://x').pathname);
    let arquivo = normalize(join(raiz, caminho));
    if (!arquivo.startsWith(raiz)) return resposta.writeHead(403).end();
    try {
        if ((await stat(arquivo)).isDirectory()) arquivo = join(arquivo, 'index.html');
        const corpo = await readFile(arquivo);
        resposta.writeHead(200, { 'Content-Type': tipos[extname(arquivo)] ?? 'application/octet-stream' }).end(corpo);
    } catch {
        // como o GitHub Pages: 404.html da pasta do projeto
        const projeto = caminho.split('/')[1];
        try {
            const corpo = await readFile(join(raiz, projeto, '404.html'));
            resposta.writeHead(404, { 'Content-Type': tipos['.html'] }).end(corpo);
        } catch {
            resposta.writeHead(404).end('404');
        }
    }
}).listen(porta, '127.0.0.1', () => console.log(`http://127.0.0.1:${porta}/ → ${raiz}${sep}`));
