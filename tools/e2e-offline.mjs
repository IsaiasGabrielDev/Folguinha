// Verifica se o PWA publicado abre sem internet depois da primeira visita. Uso:
//   node tools/navegador.mjs http://localhost:5190/ --script tools/e2e-offline.mjs
export default async function ({ avaliar, texto, esperarTexto, cdp, dormir }) {
    await esperarTexto('Boas-vindas');
    const registrado = await avaliar(`navigator.serviceWorker.ready.then(r => !!r.active)`);
    if (!registrado) throw new Error('Service worker não ficou ativo');
    // espera o cache inicial do service worker terminar
    for (let i = 0; i < 60; i++) {
        const itens = await avaliar(`caches.keys().then(ks => Promise.all(ks.map(k => caches.open(k).then(c => c.keys())))).then(l => l.flat().length)`);
        if (itens > 20) break;
        await dormir(500);
    }
    console.error('  itens em cache:', await avaliar(`caches.keys().then(ks => Promise.all(ks.map(k => caches.open(k).then(c => c.keys())))).then(l => l.flat().length)`));

    await cdp('Network.enable');
    await cdp('Network.emulateNetworkConditions', { offline: true, latency: 0, downloadThroughput: -1, uploadThroughput: -1 });
    await cdp('Page.reload', { ignoreCache: false });
    await dormir(1000);
    await esperarTexto('Boas-vindas', 60000);
    const controlado = await avaliar(`!!navigator.serviceWorker.controller`);
    if (!controlado) throw new Error('A página offline não veio do service worker');
    console.error('✔ abriu offline pelo service worker');
}
