---
target: as telas do Folguinha
total_score: 23
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 4
target_identity: "file:C:\\Users\\Isaias\\source\\repos\\Folguinha\\src\\Folguinha.Web\\Pages"
timestamp: 2026-09-21T00-03-33Z
slug: src-folguinha-web-pages
---
Method: dual-agent (A: design review · B: detector + evidência renderizada — isolados, em paralelo)

**Cobertura, ao contrário da primeira rodada:** A renderizou e olhou 28 telas em 390×844 e 1280×900; B renderizou 10 rotas e provou a identidade de cada uma (`location.pathname`, `<title>`, `<h1>`) para não repetir o erro da rodada 1, quando cinco URLs viraram a mesma tela de onboarding sem ninguém notar. A extensão do Chrome continua desconectada; os dois usaram o `tools/navegador.mjs` do próprio repo. Só ficaram fora: dark mode (julgado por código — `app.css` não declara `color-scheme` nem `prefers-color-scheme`), os passos 2–3 do remanejamento de ocorrência, e a escala em estado Encerrada.

## Design Health Score

| # | Heurística | Nota | Problema principal |
|---|-----------|------|--------------------|
| 1 | Visibilidade do estado | 2 | `Formatos.cs:126-133` computa o rótulo de `Bloqueios == 0`, então Crítico e Atenção não contam. Na tela, um chip "Rascunho validado" fica quatro linhas acima de "Avisos · 5 crítico". E o chip é `chip-ambar` — amarelo de cautela embaixo de uma palavra de tranquilidade. |
| 2 | Correspondência com o mundo real | 3 | Vocabulário excelente, quebrado por plurais de máquina em todo canto: "5 aviso(s)" (`Painel.razor:55`), "2 turnos · 1 funções" (`Mais.razor:48`), "1 escalas" (`Backup.razor:15`), "5 crítico" (`EscalaPagina.razor:215`). E "9/6" na cobertura lê como fração quebrada. |
| 3 | Controle e liberdade | 2 | `EscalaPagina.razor:156` → `:329` encerra a escala para sempre **sem nenhuma confirmação**, enquanto remover uma pessoa abre folha de confirmação completa. `FolhaCelula.razor:153-154` grava o ajuste no toque, sem desfazer. |
| 4 | Consistência e padrões | 2 | `.segmentado` é seleção única em `EscalaPagina.razor:33` e **múltipla** em `EditorDemanda.razor:20-27`, com estilo idêntico e sem estado pressionado legível. E `app.css:136` é `.item a.cobre` (descendente) enquanto `Equipe.razor:38`, `Mais.razor:17` e `Inicio.razor:57` põem as duas classes no mesmo elemento — o reset nunca aplica. |
| 5 | Prevenção de erro | 2 | `FolhaPublicar.razor:21-24` põe "Mesma justificativa para todos" como o primeiro e mais largo controle da folha de publicação. `Inicio.razor:109` diz "Tudo pronto" com duas linhas "Falta" na tela, e o CTA não depende delas. |
| 6 | Reconhecimento vs. memória | 2 | Sáb e Dom ficam fora da tela em 390px. Filtros de setor/função foram para dentro da folha Opções e a legenda desceu para depois da grade — as duas coisas trocam espaço vertical por memória. |
| 7 | Flexibilidade e eficiência | 2 | `FolhaGerar.razor:61-77` renderiza um campo de data por pessoa sem histórico de escala — dez campos empilhados entre o período e o botão Gerar. `SemHistorico` (`:152`) olha histórico de *escala*, não `Situacao`, então a loja de exemplo, cujo checklist acabou de dizer "Última folga de todos informada · Ok", ainda pede os dez. |
| 8 | Estética e minimalismo | 2 | `EditorFuncoesTurnos.razor:20` usa `.grade-3`, que não tem variante `.responsiva` (`app.css:130` vs `:131`): em 390px os três campos de horário cortam os próprios valores — a tela lê "08:0", "16:2(", "13:4(", "22:0(". |
| 9 | Diagnóstico e recuperação | 3 | `AvisoRegra` + o "Ajustar" que move a janela, limpa os filtros e abre a célula exata continua sendo o melhor componente do app. Segurado em 3 porque `EscalaPagina.razor:76` lista as violações na ordem crua do validador, sem agrupar e sem ordenar por severidade: cinco cartões verbatim idênticos para um fato só, e um Bloqueio pode ficar abaixo de vinte Atenções. |
| 10 | Ajuda e documentação | 3 | `Sobre.razor` e `RegrasPagina` citam os artigos de verdade. Segurado em 3 por um vocabulário duplo não reconciliado: o validador fala Bloqueio/Crítico/Atenção/Informativo (`Formatos.cs:108-114`), a tela de regras fala Obrigatória/Alerta/Preferência (`:101-106`), e nenhuma tela mapeia um no outro. Quem olha "Crítico" não tem como saber se aquilo impede publicar. (Não impede.) |
| **Total** | | **23/40** | **Autoral no núcleo, genérico e frágil onde o trabalho acontece** |

Modo: **Operate**. Nenhuma heurística `n/a`; máximo aplicável = 40.

**Sobre a nota ter caído de 26 para 23:** não é regressão, é evidência melhor. A rodada 1 julgou Equipe, Escala, Configurações, Painel e Mais só por código, porque nenhuma ferramenta conseguiu renderizá-las. Esta rodada olhou 28 telas e achou coisas que leitura de código não pega — campos de horário cortados, chips sem estado pressionado, linhas sublinhadas, o toast por cima das folhas. Os consertos dos P0/P1 anteriores continuam de pé; o que mudou foi o que dá para ver.

## Design Specificity Verdict

**Avaliação do review (não ancorada):** a divisão é nítida e corre exatamente na linha núcleo/cadastro.

O artefato central é autoral. Ninguém montando um CRUD genérico chega numa tabela funcionário×dia com coluna de nome fixa carregando as horas-em-vista da pessoa (`GradeEscala.razor:32`), linha de cobertura no `tfoot` (`:53-75`), sigla de prefixo único para que "Manhã" e "Meio-dia" virem Ma e Me (`Formatos.cs:175-182`), colunas de feriado tingidas e um ponto `.travada` marcando ajuste manual. `RegrasPagina.razor:23` imprimindo `CLT art. 66`, `OJ 410 SDI-1 TST` e `Lei 10.101/2000 art. 6º` sob cada regra, com um nível editável que o piso da CLT não deixa enfraquecer, é pensamento de domínio.

A metade de cadastro é um app de configurações genérico com a mesma tinta. `EditorFuncionamento` são sete blocos idênticos de pares de horário — ~1900px de rolagem para dizer "abrimos 8h–22h todo dia". `FichaFuncionario` são cinco cartões de formulário empilhados, sem nenhuma composição que saiba que está descrevendo alguém que precisa estar em algum lugar às 8h. `EditorDemanda` é uma planilha. **É a tela onde o dono decide quantas pessoas a loja precisa, e ela entrega uma grade de campos numéricos em vez de um dia.**

Duas coisas deixam o *núcleo* menos específico do que ele pensa. Em 390px a visão de semana corta sábado e domingo: `7rem` de coluna de nome mais sete células de `2.75rem` dão ~448px numa caixa de 358px. A regra legal de manchete do app é *domingo de folga*, e a coluna do domingo é justamente a que exige um gesto horizontal. E a lista de avisos — o produto final de uma ferramenta de defensabilidade — é um log plano: cinco cartões consecutivos, "Gabi Rocha está na escala no feriado Independência do Brasil (seg 07/09)", "Hugo Alves está na escala…", "Iara Freitas está na escala…", cada um repetindo a mesma sugestão de duas linhas. Um fato renderizado cinco vezes.

**Varredura determinística:** `impeccable detect` nos 33 `.razor` → **0 achados, exit 0**, e a config foi conferida (nada nos quatro diretórios está suprimido). O limite é o de sempre: `.razor` cai em modo regex e a varredura de markup nunca abre `app.css`.

A cobertura real veio de outro caminho. B descobriu que o detector **não tem flag para estado de armazenamento** (`detect --help`), então serializou o `<body>` renderizado de cada rota mais todas as regras de `document.styleSheets` em HTML autocontido e passou isso pelo detector — que aí rodou em **modo HTML com CSS computado de verdade**. 10 rotas, identidade provada, zero quedas para `/inicio`.

**Achados por regra** (deduplicados por snippet): `low-contrast` 30, `cramped-padding` 13, `undersized-ui-text` 6, `gpt-thin-border-wide-shadow` 8, `overused-font` 10, `cream-palette` 10.

**Convergência que importa — os contrastes agora são reais.** Na rodada 1 as 8 flags de `low-contrast` eram todas botões `disabled`, que a WCAG 2.2 SC 1.4.3 isenta. Desta vez são texto ativo, e batem exatamente com o que o review achou por conta própria:

| Razão | Cor | Fundo | Onde | Impacto |
|---|---|---|---|---|
| **4.08:1** | `--tinta-3` #7C7268 | `--superficie-2` #F4EEE6 | `app.css:241` `.cel-folga`, `:243` `.cel-vazia` | 17 ocorrências só em `/escala`. É o glifo do estado mais lido da grade. |
| **4.29:1** | `--coral-forte` #B84E2E | `--coral-suave` #FCE9E2 | `app.css:92-93` `.barra a.active` | Item ativo da navegação, em toda página. |
| **4.41:1** | `--tinta-3` #7C7268 | `--bg` #FBF7F2 | `app.css:89` `.barra a` | Rótulo inativo da nav, 12px. |
| **4.49:1** | `--coral-forte` | `--ambar-suave` | `:61` `a:hover` sobre `:170` | Limítrofe, praticamente passando. |

O cabeçalho do próprio `app.css` diz "Cores de texto e botões ajustadas para contraste AA sobre o fundo creme". Três pares não estão. B varreu a matriz 13×10 de tokens e excluiu os pares que falham no número mas nunca ocorrem (`--coral` e `--verde` só aparecem em `border-color`, `outline`, `accent-color` e fundo — nunca como `color:` de texto), o que é a diferença entre um relatório e um despejo de números.

**`undersized-ui-text`:** 9px de texto funcional ("folga") na ficha — `app.css:272` `.calendario .dia span { font-size: .5625rem }`.

**Alvos de toque medidos em tela** (não declarados): `.botao.pequeno` 40px — que é "Publicar", "Opções", "Adicionar", "Registrar", os paginadores e todo "Ver dia"/"Resolver"; `.segmentado button` 40px; `.toast button` 32×32; `.chip` 26px. Acima da linha: `.item` 52, `.campo` 48, `.barra a` 52, `.flutuante` 56.

**Boas notícias medidas:** **nenhum scroll horizontal de página** em nenhuma das 10 rotas, nos dois viewports. **Console completamente silencioso** — zero erro, zero warning, zero rejeição não tratada, em todas as rotas, com os hooks instalados antes da primeira navegação.

**Falsos positivos que B descartou com razão:** os 25 elementos "transbordando" em `/escala` são a região `overflow-x: auto` com `role="region"` e `tabindex="0"` funcionando como projetada; os alvos de 22×22 e 24×24 são os `<input>` dentro de `<label class="marcador">`, que tem `min-height: 44px` — medir o input sozinho subestima o alvo; e os "clipes" de `.sr-only` são exatamente o que um clip de leitor de tela deve produzir.

**Um achado que eu não repasso:** `overused-font: fraunces`, 1 por página inclusive na página 404. Fraunces está só em h1/h2/h3, no wordmark e num `<strong>` do paginador. Não acredito nesse número — cheira a artefato do método de snapshot, e não vou tratá-lo como problema sem evidência melhor.

**Overlays visuais:** não há. A extensão não conectou, o preflight de injeção nunca pôde ser tentado, o `live-server` não foi iniciado. Nada visível no seu browser.

## Overall Impression

O núcleo ficou melhor do que estava: a grade cabe na dobra, a voz é de documento, a confirmação destrutiva avisa antes. Mas ver 28 telas em vez de 4 mostrou que os problemas graves nunca estiveram no núcleo — estão no anel de cadastro e nos detalhes de renderização que leitura de código não alcança. **A maior descoberta desta rodada:** o app diz "validado" em cima de cinco Críticos não resolvidos, e diz "Tudo pronto" em cima de duas linhas "Falta" — na loja de exemplo, o único caminho que todo usuário novo percorre. Numa ferramenta cuja proposta de valor é uma palavra ("defensável"), afirmações que a própria tela desmente são o problema central, não um detalhe.

## What's Working

**1. `AvisoRegra` + o round-trip do Ajustar.** Cada violação tem chip de severidade, mensagem nomeando pessoa e data em português claro, remédio concreto ("Estenda um turno ou traga alguém que cubra esse horário") e ação. `EscalaPagina.razor:265-276` faz o Ajustar funcionar de verdade: move a janela para aquela semana, **limpa os filtros ativos** e abre a folha na célula exata. É a interação mais difícil do app e é a que está pronta — porque o aviso não é notificação, é alça da grade.

**2. O calendário do mês na ficha.** `ResumoFuncionario.razor:37-55` desenha a escala da pessoa como um mês de 7 colunas, com "folga" escrito por extenso e o dia de hoje contornado. É o único lugar do app onde o *ritmo de uma pessoa* aparece como forma — dá para ver de relance que a Ana trabalha seis e folga um. É o que o gestor e o funcionário realmente querem checar, e não precisa de legenda.

**3. `Exportar`.** Três checkboxes viram três cartões com descrição real do que vai dentro de cada arquivo, os formatos ausentes aparecem como "PDF e WhatsApp em breve" em vez de serem escondidos, e a tela fecha com "A escala planejada não comprova a jornada realizada" — uma ressalva legal colocada exatamente onde a pessoa vai entregar um documento a alguém. É a tela mais honesta do produto.

## Priority Issues

### [P0] "Rascunho validado" impresso por cima de cinco Críticos
- **Por que importa:** `Formatos.cs:126-133` computa o rótulo de `Bloqueios == 0`, então Crítico e Atenção não entram na conta. Na tela, o chip "Rascunho validado" fica quatro linhas acima de "Avisos · 5 crítico". "Validado" é a palavra do app para solidez jurídica — o comentário em `Formatos.cs:125` diz isso — e ela está sendo impressa sobre cinco violações que o próprio validador levantou. O chip ainda é `chip-ambar`: amarelo de cautela sob uma palavra de tranquilidade.
- **Correção:** `Estado` passa a receber a contagem por severidade, não um booleano. Enquanto sobrar qualquer coisa acima de Informativo, devolve "Rascunho · 5 pontos a revisar". "Validado" fica reservado para uma passagem limpa de verdade — e aí ganha `chip-verde`.
- **Comando sugerido:** `/impeccable harden`

### [P1] Três telas renderizam toda linha de lista sublinhada
- **Por que importa:** `app.css:136` é `.item a.cobre` — combinador descendente — mas `Equipe.razor:38`, `Mais.razor:17` e `Inicio.razor:57` põem as duas classes no **mesmo** elemento. O reset nunca aplica. Dez membros da equipe e onze itens de menu renderizam como texto coral sublinhado, e a linha secundária `.detalhe` fica sublinhada tão alto quanto o nome. Destrói a hierarquia nas duas telas de navegação mais usadas e faz o app parecer inacabado.
- **Correção:** `app.css:136` vira `.item.cobre, .item a.cobre { color: inherit; text-decoration: none; }`. Uma linha, três telas.
- **Comando sugerido:** `/impeccable polish`

### [P1] O toast pinta por cima das folhas e dos dados que ele comenta
- **Por que importa:** `app.css:207-213` dá `z-index: 50` ao `.toast`; `.folha` (`:285`) é 41 e `.fundo` é 40. O toast renderiza **em cima do bottom sheet** — enterra o controle "Horário personalizado" na folha da célula e atravessa o formulário de publicação. Em tela cheia cobre duas linhas da escala, e no onboarding cobre o botão "Ir para o painel", porque `--barra: 76px` continua no cálculo de `bottom` mesmo quando a nav está escondida.
- **Correção:** subir `.folha`/`.fundo` acima do toast (ou baixar o toast para 35), e calcular o `bottom` a partir de a barra existir ou não — `MainLayout.razor:6` já sabe disso via `MostrarBarra`, então a regra é `.app.sem-barra .toast`.
- **Comando sugerido:** `/impeccable layout`

### [P1] Sábado e domingo ficam fora da tela na visão padrão
- **Por que importa:** coluna de nome de `7rem` mais sete colunas de `2.75rem` (`app.css:248`, `:252`) pedem ~448px; a caixa de conteúdo em 390px tem 358px. A tela mostra Seg–Sex e metade de Sáb. A regra de manchete do produto — *domingo de folga, 1 a cada 3 semanas no comércio* — é sobre a coluna que não aparece, e a "falta" de cobertura que dispara no domingo fica igualmente escondida. É um gesto horizontal numa tabela que talvez nem pareça rolável.
- **Correção:** baixar a coluna de nome para `5rem` (já é só o primeiro nome, `GradeEscala.razor:110`) e deixar as colunas de dia encolherem abaixo de 2.75rem especificamente na visão Semana — sete células de 40px mais 5rem de nome dão 352px e cabem.
- **Comando sugerido:** `/impeccable adapt`

### [P1] Os campos de horário cortam o valor que a pessoa está digitando
- **Por que importa:** `EditorFuncoesTurnos.razor:20` usa `.grade-3`, que não tem variante `.responsiva` (`app.css:130` contra `:131` do `.grade-2`). Em 390px os três campos cortam o próprio conteúdo: a tela lê literalmente "08:0", "16:2(", "13:4(", "22:0(". **A pessoa não consegue conferir os turnos que está definindo** — e turno errado contamina toda escala que o gerador produzir depois.
- **Correção:** dar ao `.grade-3` o mesmo tratamento que o `.grade-2` já tem, ou empilhar os três campos abaixo de 480px.
- **Comando sugerido:** `/impeccable adapt`

### [P2] "Mesma justificativa para todos" industrializa o que o produto existe para evitar
- **Por que importa:** `FolhaPublicar.razor:21-24` é o primeiro, mais largo e mais alcançável controle da folha de publicação, e sua única função é carimbar uma string sobre todo alerta da CLT. O histórico mostra o resultado: "5 alertas justificados", todos lendo "Combinado com a equipe". Um relatório de validação onde toda justificativa é idêntica não vale nada na fiscalização contra a qual este app vende proteção.
- **Correção:** não apagar — justificar em lote é legítimo quando cinco alertas têm mesmo a mesma causa. Rebaixar para abaixo da lista de alertas, renomear para "Mesma justificativa para os N alertas de *[regra]*" e escopar por regra, não por folha, para que uma string só cubra um tipo de violação.
- **Comando sugerido:** `/impeccable clarify`

## Persona Red Flags

**Gestor — uma vez por mês, no celular**
- Abre a Escala e vê Seg–Sex. O fim de semana, que é onde ele se preocupa com cobertura, exige um gesto horizontal numa tabela que pode nem parecer rolável.
- Toca Publicar esperando uma confirmação; recebe um formulário de campos livres. Um mês entre sessões significa que ele não lembra para que serve uma justificativa — e nada na folha diz que aquilo vai parar num relatório de validação.
- Publica, vê um toast, e o Painel então informa que ele tem 5 avisos: exatamente as violações que ele acabou de justificar, re-renderizadas como problemas crus, porque `Painel.razor:61` mostra `Violacao` sem saber de `AlertasAceitos`. O app parabeniza e em seguida diz que ele falhou.
- Não tem caminho para mandar a escala para a equipe a partir da tela de escala — "Exportar planilhas" está dentro da folha Opções.
- `Backup.razor` diz em texto preto comum que o armazenamento é "temporário" e que o app não está instalado. Entre visitas mensais, essa é exatamente a janela em que o iOS limpa os dados.

**Primeiro uso**
- Os dois CTAs da tela 1 renderizam a `opacity: .5` até "Entendi" ser marcado. "Começar cadastro" fica coral pálido sobre branco a ~2.3:1 — lê como decoração, não como ação primária desabilitada.
- Passo 4 abre disparando chamada de rede, pede um Estado e avisa que feriados municipais "não têm lista pública confiável": três conceitos novos antes de existir um turno.
- Passo 7 diz **"Tudo pronto"** com duas linhas **"Falta"** na tela. Na loja de exemplo. A primeira coisa que ela aprende sobre o rigor do produto é que as afirmações dele não precisam ser verdade.
- Os horários que ela digita aparecem cortados como "08:0" e "16:2(". Ela não consegue revisar o que escreveu.
- Em "Em quais dias", sete chips cinza sem estado pressionado visível. Ela não tem como saber quais dias selecionou.

**Power user — refaz toda semana**
- Toda semana `FolhaGerar.razor:61-77` pede de novo a última folga de cada pessoa sem escala anterior, e `SemHistorico` (`:152`) olha `e.Inicio < _inicio`, então a cada período o conjunto se desloca em vez de esvaziar. Dez campos de data entre ela e o botão Gerar, semanalmente.
- Não existe "duplicar a escala anterior".
- Os filtros de setor e função resetam a cada Ajustar (`EscalaPagina.razor:274-275`) e moram atrás de uma folha — ela reescolhe o tempo todo.
- "Encerrar escala" está na mesma pilha plana de botões que "Exportar planilhas", mesmo tamanho, mesmo peso, **sem confirmação**. Memória muscular semanal mais um botão irreversível sem confirmação numa lista é questão de tempo.

## Minor Observations

- Plurais de máquina em todo o app: "5 aviso(s)", "1 funções", "1 escalas", "5 crítico", "alteração(ões)".
- `/configuracoes` sem seção renderiza 404 — a rota é `@page "/configuracoes/{Secao}"`. Conferi: **nada no código linka para o caminho nu**, só quem digita a URL chega lá.
- `app.css` não declara `color-scheme` nem tem bloco `prefers-color-scheme`. Num Android em modo escuro, os `<input type=date/time>` e `<select>` nativos — e este app é quase todo controle nativo — renderizam com a paleta escura do agente dentro de cartões creme.
- `cramped-padding`: 7 ocorrências no Painel e 6 na ficha (filhos encostados no fundo, sem inset).
- 9px de texto funcional ("folga") em `app.css:272` `.calendario .dia span`.
- `.toast button` mede 32×32 e `.chip` 26px de altura; `.botao.pequeno` e `.segmentado button` medem 40px — abaixo dos 44 em "Publicar", "Opções", "Adicionar", "Registrar", paginadores e todo "Ver dia".
- `Formatos.ClasseAvatar` (`:195`) usa `Guid.GetHashCode()`, não estável entre versões do runtime — e colore avatares da mesma família de seis matizes das células de turno, então um sistema de cor significativo e um decorativo ficam indistinguíveis.
- `EscalaPagina.razor:156` encerra a escala sem confirmação nenhuma, três toques de distância do "Apagar tudo", que faz tudo certo.
- `GuardaSaida.razor:15` usa `window.confirm` — diálogo de cromo do browser num app que tem `Folha` desenhada para todo o resto.
- `Diagnostico.razor` continua no menu Mais, exposto a qualquer usuário.
- No desktop, `.conteudo.largo` é 72rem mas a grade renderiza a ~860px e nunca expande: o layout de desktop é o de celular com uma barra lateral, sem mês por padrão e sem avisos ao lado da grade — que resolveria o problema de Sáb/Dom de graça.
- `FolhaGerar.razor:80` diz "Gerando… pode levar alguns segundos" sem progresso e sem cancelar, numa computação que trava a thread da UI.

## Questions to Consider

1. O portão de publicação bloqueia em **Bloqueio** e libera em **Crítico**. Quem decidiu que um Crítico — cinco pessoas escaladas num feriado nacional, ou um dia sem cobertura alguma — é coisa que se assina num campo de texto em vez de consertar? Se a resposta é "o dono conhece o negócio dele", para que existe Bloqueio?
2. Numa fiscalização, um relatório onde as cinco justificativas leem "Combinado com a equipe" é *melhor* que nenhum relatório, ou é prova de que a revisão foi encenada? Para qual dos dois resultados este produto está otimizando?
3. O Painel relevanta violações que o usuário já justificou na publicação (`Painel.razor:61`, sem saber de `AlertasAceitos`). "Precisa de atenção" é uma lista de *problemas* ou de problemas *não revisados*? São dois produtos diferentes, e hoje o app entrega o primeiro enquanto o texto promete o segundo.
4. "Tudo pronto" sobre duas linhas "Falta" está na **loja de exemplo** — a demo curada, o único caminho que todo usuário novo percorre. O que isso diz sobre o laço de revisão?
5. O núcleo ganhou display serifada, paleta defendida, grade de coluna fixa e uma tela de citação legal. O `EditorDemanda` ganhou uma planilha. Se um dono de loja abandona este app, em qual dessas duas telas você acha que ele abandona?
6. O calendário do mês na ficha é a única vista que mostra o *ritmo de uma pessoa* — o que o gestor e o funcionário querem ver. Por que ele está três toques abaixo de um formulário de edição, e por que não é isso que o "Exportar" entrega para a equipe?
7. Publicar termina num toast de 4 segundos que cobre a própria escala. Se publicar é o cume emocional do produto — o momento em que vira documento — por que ele não termina na tela de onde se envia para a equipe?
