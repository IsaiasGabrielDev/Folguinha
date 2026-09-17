// Armazenamento local (IndexedDB) e entrega de arquivos para o Folguinha.
const BANCO = 'folguinha';
const TABELA = 'chave-valor';

let conexao;
function abrir() {
    conexao ??= new Promise((ok, falha) => {
        const pedido = indexedDB.open(BANCO, 1);
        pedido.onupgradeneeded = () => pedido.result.createObjectStore(TABELA);
        pedido.onsuccess = () => ok(pedido.result);
        pedido.onerror = () => falha(pedido.error);
    });
    return conexao;
}

async function transacao(modo, operacao) {
    const banco = await abrir();
    return new Promise((ok, falha) => {
        const t = banco.transaction(TABELA, modo);
        const pedido = operacao(t.objectStore(TABELA));
        t.oncomplete = () => ok(pedido.result);
        t.onerror = () => falha(t.error);
        t.onabort = () => falha(t.error);
    });
}

export const ler = async chave => (await transacao('readonly', t => t.get(chave))) ?? null;
export const gravar = (chave, valor) => transacao('readwrite', t => t.put(valor, chave));
export const remover = chave => transacao('readwrite', t => t.delete(chave));

/// Pede ao navegador para não apagar os dados sozinho (importante no Safari do iPhone).
export async function pedirPersistencia() {
    if (!navigator.storage?.persist) return 'indisponivel';
    if (await navigator.storage.persisted()) return 'persistente';
    return (await navigator.storage.persist()) ? 'persistente' : 'temporario';
}

export async function espacoUsado() {
    const e = await navigator.storage?.estimate?.();
    return e ? Math.round(e.usage / 1024) : -1;
}

export const instalado = () =>
    matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;

/// No celular abre o menu de compartilhar do sistema; no computador baixa o arquivo.
export async function entregarArquivo(nome, tipo, stream) {
    const blob = new Blob([await stream.arrayBuffer()], { type: tipo });
    const arquivo = new File([blob], nome, { type: tipo });
    const toque = matchMedia('(pointer: coarse)').matches;
    if (toque && navigator.canShare?.({ files: [arquivo] })) {
        try {
            await navigator.share({ files: [arquivo], title: nome });
            return 'compartilhado';
        } catch (erro) {
            if (erro.name === 'AbortError') return 'cancelado';
        }
    }
    const url = URL.createObjectURL(blob);
    const link = Object.assign(document.createElement('a'), { href: url, download: nome });
    document.body.append(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10_000);
    return 'baixado';
}
