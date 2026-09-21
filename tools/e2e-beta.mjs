// Roteiro de ponta a ponta do beta. Uso:
//   node tools/navegador.mjs http://localhost:5180/ --script tools/e2e-beta.mjs [--largura 390 --altura 844]
// Fotos vão para a pasta indicada em FOTOS (padrão: ./artefatos).
import { mkdirSync } from 'node:fs';
import { join } from 'node:path';

export default async function ({ avaliar, texto, esperarTexto, clicar, preencher, fotografar, dormir }) {
    const pasta = process.env.FOTOS ?? 'artefatos';
    mkdirSync(pasta, { recursive: true });
    const foto = nome => fotografar(join(pasta, `${nome}.png`));
    // caminhos relativos à <base>, para funcionar também em subpasta (GitHub Pages)
    const ir = async caminho => { await avaliar(`Blazor.navigateTo(${JSON.stringify(caminho.replace(/^\//, ''))})`); await dormir(400); };
    const passo = nome => console.error(`▶ ${nome}`);
    const hoje = new Date();
    const iso = d => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

    passo('cadastro inicial com loja de exemplo');
    await esperarTexto('Boas-vindas');
    await foto('01-boas-vindas');
    await preencher('Entendi', true);
    await clicar('Explorar com uma loja de exemplo');
    await esperarTexto('Gerar a primeira escala');
    await foto('02-tudo-pronto');

    passo('gerar a primeira escala');
    await clicar('Gerar a primeira escala');
    await esperarTexto('Gerar rascunho');
    await foto('03-gerar');
    await clicar('Gerar rascunho');
    await esperarTexto('Rascunho gerado', 60000);
    await esperarTexto('Avisos');
    await dormir(500);
    await foto('04-escala');

    passo('ajuste manual numa célula');
    await avaliar(`document.querySelectorAll('.celula')[3].click()`);
    await esperarTexto('Trocar para');
    const folgaOuTurno = (await texto()).includes('Agora: Folga') ? 'Manhã 08:00–16:20' : 'Folga';
    await clicar(folgaOuTurno);
    await esperarTexto('Ajuste salvo|Resolvido|Bloqueio|Crítico|Atenção'.split('|')[0], 30000).catch(async () => {
        const t = await texto();
        if (!/Resolvido|Bloqueio|Crítico|Atenção|Informativo/.test(t)) throw new Error('Ajuste sem retorno na tela');
    });
    await foto('05-ajuste');
    await clicar('Fechar');

    passo('publicar');
    await ir('/escala');
    await esperarTexto('Avisos');
    const antes = await texto();
    if (antes.includes('Bloqueio')) {
        // resolve bloqueios gerando de novo sem o ajuste
        await clicar('Opções');
        await clicar('Apagar rascunho');
        await ir('/escala?gerar=1');
        await esperarTexto('Gerar rascunho');
        await clicar('Gerar rascunho');
        await esperarTexto('Rascunho gerado', 60000);
    }
    await clicar('Publicar');
    await esperarTexto('Publicar versão 1');
    if (await avaliar(`!!document.querySelector('input[aria-label="Justificativa para todos"]')`)) {
        await avaliar(`(() => { const i = document.querySelector('input[aria-label="Justificativa para todos"]');
            Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(i, 'Combinado com a equipe');
            i.dispatchEvent(new Event('change', { bubbles: true })); })()`);
        await dormir(300);
        await clicar('Aplicar');
    }
    await foto('06-publicar');
    await clicar('Publicar versão 1');
    await esperarTexto('Versão 1 publicada', 30000);

    passo('ocorrência com remanejamento');
    await ir('/escala');
    await esperarTexto('Avisos');
    // uma célula de manhã num dia futuro desta semana: "Nome, qui 17/09: Manhã ..."
    const rotulo = await avaliar(`(() => {
        const hoje = new Date();
        const c = [...document.querySelectorAll('.celula')].find(b => {
            const m = b.getAttribute('aria-label').match(/(\\d{2})\\/(\\d{2})/);
            return b.innerText.trim() === 'M' && m && new Date(hoje.getFullYear(), m[2] - 1, m[1]) > hoje;
        });
        return c?.getAttribute('aria-label') ?? null;
    })()`);
    if (!rotulo) throw new Error('Nenhum turno futuro na semana para testar a ocorrência');
    const nome = rotulo.split(',')[0];
    const [, dd, mm] = rotulo.match(/(\d{2})\/(\d{2})/);
    const dia = `${hoje.getFullYear()}-${mm}-${dd}`;
    console.error(`  ocorrência para ${nome} em ${dia}`);
    await ir('/ocorrencias/nova');
    await esperarTexto('Nova ocorrência');
    const valor = await avaliar(`[...document.querySelector('select').options].find(o => o.text === ${JSON.stringify(nome)}).value`);
    await preencher('Quem', valor);
    await preencher('De', dia);
    await preencher('Até', dia);
    await clicar('Continuar');
    await esperarTexto('Substitutos');
    await esperarTexto('Ver impacto');
    await esperarTexto('Deixar sem substituto', 60000);
    await dormir(500);
    if ((await texto()).includes('Melhor opção')) {
        await avaliar(`(() => { const r = [...document.querySelectorAll('input[type=radio]')].find(x => !x.disabled && x.closest('label').innerText.includes('Melhor opção')); r?.click(); })()`);
        await dormir(300);
    }
    await foto('07-substitutos');
    await clicar('Ver impacto');
    await esperarTexto('Impacto');
    await avaliar(`document.querySelectorAll('input[maxlength="200"]').forEach(i => {
        Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(i, 'Cobertura combinada');
        i.dispatchEvent(new Event('change', { bubbles: true })); })`);
    await dormir(300);
    await foto('08-impacto');
    await clicar('Aprovar');
    await esperarTexto('Ocorrência aprovada|Anteriores|Atuais'.split('|')[0], 30000).catch(async () => {
        if (!(await texto()).includes('pendência')) throw new Error('Aprovação sem retorno');
    });

    passo('histórico');
    await ir('/historico');
    await esperarTexto('Versão 1');
    await foto('09-historico');

    passo('exportar');
    await ir('/exportar');
    await esperarTexto('Gerar e compartilhar');
    await foto('10-exportar');
    await clicar('Gerar e compartilhar');
    await esperarTexto('Planilha', 30000);

    passo('painel');
    await ir('/');
    await esperarTexto('Cobertura da semana');
    await dormir(800);
    await foto('11-painel');

    passo('equipe e ficha');
    await ir('/equipe');
    await esperarTexto('pessoas');
    await foto('12-equipe');
    await avaliar(`document.querySelector('a.item').click()`);
    await esperarTexto('Como a pessoa chega');
    await foto('13-ficha');

    passo('mais, regras, feriados, backup, sobre');
    for (const [caminho, marca, nome] of [
        ['/mais', 'Backup e dados', '14-mais'],
        ['/regras', 'Descanso entre jornadas', '15-regras'],
        ['/configuracoes/feriados', 'Rodízio de feriados', '16-feriados'],
        ['/configuracoes/demanda', 'Datas especiais', '17-demanda'],
        ['/configuracoes/turnos', 'Funções', '18-turnos'],
        ['/configuracoes/funcionamento', 'não abre', '19-funcionamento'],
        ['/backup', 'Cópia de segurança', '20-backup'],
        ['/sobre', 'Privacidade', '21-sobre'],
        ['/ocorrencias', 'Ocorrências', '22-ocorrencias'],
    ]) {
        await ir(caminho);
        await esperarTexto(marca);
        await dormir(caminho.includes('feriados') ? 2500 : 300);
        await foto(nome);
    }
    console.error('✔ roteiro concluído');
}
