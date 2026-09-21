// Roteiro de crítica de design: semeia a loja de exemplo e fotografa TODAS as telas.
import { mkdirSync } from 'node:fs';
import { join } from 'node:path';

export default async function ({ avaliar, texto, esperarTexto, clicar, preencher, fotografar, dormir }) {
    const pasta = process.env.FOTOS ?? 'artefatos';
    mkdirSync(pasta, { recursive: true });
    const foto = nome => fotografar(join(pasta, `${nome}.png`));
    const ir = async caminho => { await avaliar(`Blazor.navigateTo(${JSON.stringify(caminho.replace(/^\//, ''))})`); await dormir(600); };
    const passo = nome => console.error(`> ${nome}`);

    passo('cadastro inicial');
    await esperarTexto('Boas-vindas');
    await foto('01-boas-vindas');
    await preencher('Entendi', true);
    await clicar('Explorar com uma loja de exemplo');
    await esperarTexto('Tudo pronto');
    await foto('02-tudo-pronto');

    passo('gerar');
    await clicar('Gerar a primeira escala');
    await esperarTexto('Gerar rascunho');
    await foto('03-gerar');
    await clicar('Gerar rascunho');
    await esperarTexto('Rascunho gerado', 60000);
    await esperarTexto('Avisos');
    await dormir(600);
    await foto('04-escala');

    passo('celula / ajuste');
    await avaliar(`document.querySelectorAll('.celula')[3].click()`);
    await esperarTexto('Trocar para');
    await dormir(300);
    await foto('05-celula');
    await clicar('Fechar').catch(() => {});
    await dormir(300);

    passo('publicar');
    await clicar('Publicar');
    await esperarTexto('Publicar versão 1');
    await dormir(400);
    await foto('06-publicar');
    if (await avaliar(`!!document.querySelector('input[aria-label="Justificativa para todos"]')`)) {
        await avaliar(`(() => { const i = document.querySelector('input[aria-label="Justificativa para todos"]');
            Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(i, 'Combinado com a equipe');
            i.dispatchEvent(new Event('change', { bubbles: true })); })()`);
        await dormir(300);
        await clicar('Aplicar').catch(() => {});
    }
    await clicar('Publicar versão 1').catch(() => {});
    await esperarTexto('Versão 1 publicada', 30000).catch(() => console.error('  (publicação não confirmou)'));
    await dormir(600);
    await foto('07-publicada');

    passo('demais telas');
    for (const [caminho, marca, nome, espera] of [
        ['/', 'Cobertura da semana', '08-painel', 900],
        ['/equipe', 'pessoas', '09-equipe', 400],
        ['/ocorrencias', 'Ocorrências', '10-ocorrencias', 400],
        ['/ocorrencias/nova', 'Nova ocorrência', '11-nova-ocorrencia', 400],
        ['/historico', 'Versão', '12-historico', 400],
        ['/exportar', 'Gerar e compartilhar', '13-exportar', 400],
        ['/mais', 'Backup e dados', '14-mais', 300],
        ['/regras', 'Descanso entre jornadas', '15-regras', 400],
        ['/configuracoes', 'Configurações', '16-configuracoes', 400],
        ['/configuracoes/empresa', 'empresa', '17-empresa', 400],
        ['/configuracoes/turnos', 'Funções', '18-turnos', 500],
        ['/configuracoes/funcionamento', 'não abre', '19-funcionamento', 500],
        ['/configuracoes/demanda', 'Datas especiais', '20-demanda', 500],
        ['/configuracoes/feriados', 'Rodízio de feriados', '21-feriados', 2500],
        ['/backup', 'Cópia de segurança', '22-backup', 400],
        ['/sobre', 'Privacidade', '23-sobre', 300],
        ['/diagnostico', '', '24-diagnostico', 800],
    ]) {
        await ir(caminho);
        if (marca) await esperarTexto(marca, 20000).catch(e => console.error(`  ! ${caminho}: ${e.message.slice(0, 120)}`));
        await dormir(espera);
        await foto(nome);
    }

    passo('ficha de uma pessoa');
    await ir('/equipe');
    await esperarTexto('pessoas');
    await avaliar(`document.querySelector('a.item')?.click()`);
    await esperarTexto('Como a pessoa chega', 20000).catch(e => console.error('  ! ficha: ' + e.message.slice(0, 200)));
    await dormir(500);
    await foto('25-ficha');
    // rolagem da ficha, que é longa
    await avaliar(`window.scrollTo(0, document.body.scrollHeight)`);
    await dormir(500);
    await foto('26-ficha-fim');

    passo('escala rolada + opções');
    await ir('/escala');
    await esperarTexto('Avisos');
    await dormir(500);
    await avaliar(`window.scrollTo(0, document.body.scrollHeight)`);
    await dormir(400);
    await foto('27-escala-fim');
    await clicar('Opções').catch(() => {});
    await dormir(400);
    await foto('28-opcoes');

    console.error('roteiro concluido');
}
