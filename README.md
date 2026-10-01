# Folguinha

Gerenciador de escalas e folgas conforme a CLT, para o gerente montar, validar, publicar e exportar a escala da loja pelo celular ou computador.

- **Beta:** PWA em Blazor WebAssembly (.NET 10). Instala na tela inicial do iPhone e do Android e funciona offline. Os dados ficam só no aparelho (IndexedDB), com backup em arquivo.
- **Motor em C#:** regras da CLT, gerador automático, remanejamento, versões e exportação `.xlsx`.
- **Especificação:** [SPEC.md](SPEC.md) (decisões e escopo) e [docs/especificacao.md](docs/especificacao.md) (requisitos funcionais). O andamento está em [tasks/todo.md](tasks/todo.md).

> O Folguinha auxilia na validação da escala, mas não substitui o Departamento Pessoal, o contador ou a assessoria jurídica.

**Acesse:** https://isaiasgabrieldev.github.io/Folguinha/

## Como funciona

### Para o gerente

1. **Primeiro acesso:** um passo a passo cadastra a loja (horário de funcionamento, UF para os feriados), os turnos e a equipe. Dá para pular e explorar com uma **loja de exemplo**.
2. **Equipe:** para cada pessoa, função, regime (6x1, 5x2, 12x36, meio período), carga horária, disponibilidade e a **situação inicial** (última folga, último domingo de folga, se trabalhou no último feriado). É dela que o gerador continua a sequência.
3. **Gerar a escala:** escolha semana, quinzena ou mês. O gerador distribui turnos e folgas respeitando a CLT, a demanda mínima de cada turno e função, o rodízio de domingos e feriados e as folgas extras configuradas.
4. **Revisar:** a grade mostra alertas (Bloqueio, Crítico, Atenção, Informativo), cada um com a explicação e uma sugestão de correção. Ajustes manuais são revalidados na hora.
5. **Publicar:** a escala passa por Rascunho → Validada → Publicada. Uma escala publicada nunca é sobrescrita: cada mudança vira uma nova versão com motivo, autor e quem foi afetado (**Mais → Histórico**).
6. **Ocorrências:** atestado, falta, licença ou folga extra. O app mostra os turnos afetados, sugere substitutos em ordem de adequação e simula o impacto antes de aprovar.
7. **Exportar:** planilhas `.xlsx` individual, consolidada (calendário) e o relatório de validação. No celular, o arquivo sai pelo menu de compartilhar; no computador, é baixado.

O **Painel** resume o período: turnos sem cobertura, falta ou excesso de horas, horas extras previstas, descanso irregular e distribuição de domingos e feriados.

### Por dentro

- **Regras** (`Folguinha.Domain/Regras`): cada regra valida a escala e devolve violações com pessoa, dia e sugestão. Cada uma tem nível (**Obrigatória**, **Alerta** ou **Preferência**), fundamento legal e vigência, e é versionada: mudar uma regra não altera escalas já encerradas. Padrões: jornada diária e semanal, intervalo intrajornada, 11h entre jornadas, repouso semanal, domingos e feriados.
- **Gerador** (`Folguinha.Domain/Geracao`): heurística determinística. Primeiro marca as folgas de cada semana (nunca mais de 6 dias seguidos, rodízio de domingos, folgas extras), depois preenche os turnos de cada dia pela demanda, só com alocações que não quebram regra obrigatória. Ajustes manuais e dias já publicados ficam travados.
- **Remanejamento** (`Folguinha.Domain/Remanejo`): ranqueia substitutos para uma ocorrência sem quebrar as regras obrigatórias.
- **Dados:** no beta, tudo fica no IndexedDB do navegador. A camada de aplicação usa interfaces de serviço, então na fase online as telas passam a falar com uma API sem mudar.

## Estrutura

```
src/Folguinha.Domain          regras CLT, gerador de escala, remanejamento (C# puro, sem I/O)
src/Folguinha.Application     casos de uso: escalas versionadas, publicação, ocorrências, regras, feriados, backup
src/Folguinha.Infrastructure  BrasilAPI (feriados) e planilhas .xlsx sem dependências
src/Folguinha.Web             PWA Blazor WebAssembly (telas, IndexedDB, compartilhar arquivo)
tests/                        xUnit (Domain, Application, Infrastructure)
tools/                        testes de ponta a ponta no navegador e servidor estático de teste
docs/design/                  mockups aprovados e ícone
```

## Comandos

No Windows, `executar.bat` sobe o app e abre no navegador; `executar-exemplo.bat` já abre com uma loja de exemplo completa (6 pessoas, 3 turnos).

```bash
dotnet build Folguinha.slnx                       # compila tudo
dotnet test Folguinha.slnx                        # testes (164)
dotnet run --project src/Folguinha.Web            # app em http://localhost:5xxx
dotnet publish src/Folguinha.Web -c Release -o publicado   # site estático em publicado/wwwroot
```

Teste de ponta a ponta (precisa do Node.js e do Edge ou Chrome):

```bash
dotnet run --project src/Folguinha.Web --urls http://localhost:5180
node tools/navegador.mjs http://localhost:5180/ --script tools/e2e-beta.mjs                            # celular 390×844
node tools/navegador.mjs http://localhost:5180/ --script tools/e2e-beta.mjs --largura 1280 --altura 900
node tools/navegador.mjs http://localhost:5190/ --script tools/e2e-offline.mjs   # sobre o site publicado
```

As fotos das telas vão para `artefatos/` (ou para a pasta em `FOTOS`). A página `/diagnostico` roda um autoteste numa loja separada, sem tocar nos dados reais.

## Publicar (GitHub Pages)

O workflow [.github/workflows/publicar.yml](.github/workflows/publicar.yml) testa e publica a cada push na `main`.

1. No repositório, abra **Settings → Pages** e, em **Source**, escolha **GitHub Actions**.
2. A cada push na `main`, o site é atualizado em https://isaiasgabrieldev.github.io/Folguinha/.

A base do app se ajusta sozinha à subpasta do Pages, e o `404.html` permite abrir links diretos. Outras hospedagens estáticas com HTTPS (Cloudflare Pages, Azure Static Web Apps) também servem: basta publicar a pasta `publicado/wwwroot`.

## Instalar no celular

- **iPhone (Safari):** abra o endereço, toque em Compartilhar e depois em **Adicionar à Tela de Início**.
- **Android (Chrome):** abra o endereço, toque no menu (⋮) e depois em **Instalar app**.

Instalar é importante: sem isso, o Safari pode apagar os dados de sites que ficam 7 dias sem uso. Faça backup em **Mais → Backup e dados**.

## Limitações conhecidas do beta

- Um aparelho e um gerente. Login, perfis, funcionário consultando a escala, notificações e sincronização ficam para a fase online (API ASP.NET Core + PostgreSQL).
- PDF e envio por WhatsApp ainda não existem.
- No 6x1, a semana anterior a uma folga de domingo obrigatória pode ter 5 dias de trabalho, para não passar de 6 dias seguidos.
- O horário do intervalo na planilha é uma sugestão (meio da jornada); o app guarda só a duração.
- As fontes (Google Fonts) só aparecem com internet; offline, o app usa as fontes do sistema.

## Licença

[MIT](LICENSE)
