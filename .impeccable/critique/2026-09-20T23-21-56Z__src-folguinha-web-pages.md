---
target: as telas do Folguinha
total_score: 26
max_score: 40
na_heuristics: 
p0_count: 2
p1_count: 2
target_identity: "file:C:\\Users\\Isaias\\source\\repos\\Folguinha\\src\\Folguinha.Web\\Pages"
timestamp: 2026-09-20T23-21-56Z
slug: src-folguinha-web-pages
---
Method: dual-agent (A: design review, isolada · B: detector + evidência de browser, isolada)

**Cobertura limitada:** a extensão do Chrome não estava conectada em nenhum dos dois assessments (`list_connected_browsers` → `[]`). A não tirou screenshot; B caiu para o modo URL headless do próprio detector. Além disso, com localStorage vazio o `MainLayout.razor:73` redireciona toda rota fora de `["inicio","diagnostico","backup","sobre"]` para `/inicio`, então **Equipe, Escala, Configurações, Painel e Mais nunca foram renderizadas por nenhuma ferramenta**. Tudo que diz respeito a essas telas vem de leitura de código.

## Design Health Score

| # | Heurística | Nota | Problema principal |
|---|-----------|------|--------------------|
| 1 | Visibilidade do estado | 3 | `EscalaPagina.razor:19` renderiza `Formatos.Estado(_escala, Bloqueios == 0)` antes de `Validar()` terminar (`:223-232`) → chip diz "Rascunho validado" sem regra nenhuma ter rodado. `Painel.razor:23` idem. |
| 2 | Correspondência com o mundo real | 3 | A mesma tela tem três nomes: "Quantas pessoas" (`Mais.razor:49`), "Quantas pessoas por turno?" (`Inicio.razor:108`), "Horários de mais fluxo" (`EditorDemanda.razor:5`). |
| 3 | Controle e liberdade | 2 | Mutações em massa sem confirmação e sem desfazer: `FichaFuncionario.razor:179`, `FolhaGerar.razor:23` (Revezar), `EditorFeriados.razor:199` (salva a cada toggle). |
| 4 | Consistência e padrões | 2 | A escolha destrutiva é o botão primário: `Backup.razor:54`, `Backup.razor:46`, `FichaFuncionario.razor:215`, `OcorrenciasPagina.razor:53` — todos `botao primario`. `.botao.perigo` (`app.css:147`) só é usado nos gatilhos. |
| 5 | Prevenção de erro | 3 | `FolhaGerar.razor:150-173` é exemplar; anulado pelas mutações em massa sem confirmar e pela ausência de dirty guard. |
| 6 | Reconhecimento vs. memória | 2 | `Formatos.Sigla` (`Formatos.cs:161`) devolve `nome[..1]` → "Manhã" e "Meio-dia" viram **M**. `ClasseTurno` (`Formatos.cs:147,151`) faz `% 5` → turno 6 repete a cor do turno 1. Isso está em toda célula de `GradeEscala.razor:46`. |
| 7 | Flexibilidade e eficiência | 3 | Existem atalhos reais ("Replicar p/ todos", Duplicar pessoa, `?volta=`), mas a grade não tem navegação por setas nem edição em lote: 7×12 = 84 tab stops por semana. |
| 8 | Estética e minimalismo | 2 | `EscalaPagina.razor:16-98` empilha até 12 controles; `<GradeEscala>` só aparece em `:100`. `Exportar.razor:64-67` coloca chips "em breve" dentro da área de ação. |
| 9 | Diagnóstico e recuperação | 3 | O loop `AvisoRegra` → "Ajustar" → "Resolvido: …" (`EscalaPagina.razor:119`, `FolhaCelula.razor:74`) é excelente. Mas `Loja.Substituir` (`Loja.cs:42`) não tem try/catch, ao contrário de `Alterar` (`:26-40`). |
| 10 | Ajuda e documentação | 3 | `Sobre.razor` cita os artigos da CLT e `RegrasPagina.razor:23` mostra o fundamento por regra. Mas o checklist de `Inicio.razor:140-150` fica inalcançável depois do passo 7. |
| **Total** | | **26/40** | **Mediano — sólido no núcleo, genérico e arriscado nas bordas** |

Modo da superfície: **Operate**. Nenhuma heurística marcada `n/a`; máximo aplicável = 40.

## Design Specificity Verdict

**Avaliação do review (não ancorada):** autoria real, mas concentrada em ~4 superfícies. `GradeEscala.razor` é um instrumento de escala, não uma tabela: coluna de nome fixa (`app.css:239`), chips por turno, horas-em-vista por pessoa (`:32`), linha de cobertura `escalados/mínimo` no `<tfoot>` (`:53-76`). `FolhaGerar.razor:15-25` detecta a equipe inteira folgando na mesma semana, explica em termos do dono ("a loja fica vazia nesse fim de semana") e oferece **Revezar** num toque — isso é conhecimento de domínio virando interface. `Painel.razor:35-49` e `FolhaCelula.razor` seguem a mesma linha. O texto acompanha: "Coloque menos gente nos dias mais tranquilos", "A escala planejada não comprova a jornada realizada".

Contra isso: `Cabecalho`, `.cartao`, `.lista`/`.item`, `.botao`, `Folha` formam um kit intercambiável. `Equipe`, `Mais`, `Configuracoes`, `Historico` e `Backup` sobreviveriam a um copy-paste em qualquer PWA Blazor trocando só as strings. **A metade de cadastro — onde o primeiro usuário passa os primeiros 40 minutos — é genérica.**

Tensão não resolvida: a direção visual (creme `#FBF7F2`, coral, **Fredoka** arredondada, raios de 22px) é consistente e agradável, mas a promessa do produto é defensabilidade jurídica sob a CLT. Uma display arredondada define o registro de um habit tracker numa tela cujo trabalho é dizer "essa escala se sustenta".

**Varredura determinística:** `impeccable detect` nos 33 `.razor` de Pages/Componentes/Editores/Layout → **0 achados, exit 0**. O assessment B não aceitou isso de cara e provou com três sondas que (a) o detector funciona, (b) `.razor` é reconhecido, (c) não há supressão por config. Mas provou também o alcance: `.razor` cai no **modo regex**, não no modo HTML — um `Probe.razor` com fonte de 9px e `#777`-sobre-`#fff` passou limpo, enquanto o `.html` idêntico acusou os dois. E a varredura de markup nunca abre `app.css`, que é onde vive todo o estilo do app. **Logo: 0 achados não significa "limpo em contraste, escala tipográfica ou espaçamento".**

No modo URL headless (1280×900 e 390×844, idêntico nos dois): **7 achados por viewport** nas quatro rotas alcançáveis.
- `cream-palette` ×4 (slop, warning) — uma por página, rastreada a `app.css:4` `--bg: #FBF7F2`. Token de marca deliberado e aplicado com consistência; **não é bug, mas é a mesma dúvida de registro que o review levantou por conta própria, por outro caminho.** Duas leituras independentes chegando no creme merece atenção.
- `low-contrast` ×4 — `/inicio` ×2, `/diagnostico` ×3, `/backup` ×1 (8 instâncias no total entre as duas passadas).

**Convergência que importa — o "falso positivo" que não é:** as 8 instâncias de `low-contrast` são 8/8 botões `disabled`, todas produzidas por uma regra só, `app.css:151` `.botao[disabled] { opacity: .5 }`. Pela WCAG 2.2 SC 1.4.3, componentes inativos são explicitamente isentos do mínimo de contraste, então o detector está medindo um estado que a norma dispensa — falso positivo de alta confiança, como B classificou. **Só que o review chegou no mesmo `app.css:151` por outro lado:** `.botao.primario` desabilitado (branco sobre `#B84E2E` a 50%) fica perto de 2.3:1, e esse é exatamente o estado em que o primeiro usuário vê **Gerar** e **Publicar** — os dois botões mais importantes do app. Isento da norma, problema de UX real.

**Overlays visuais:** não há. A extensão do Chrome não conectou, o preflight de injeção nunca pôde ser tentado, o `live-server` não foi iniciado e o `detect.js` não foi injetado em página nenhuma. Não existe overlay visível no seu browser.

**Alvos de toque abaixo de 44px** (valores declarados em `app.css`, não medidos em tela): `.toast button` 32px (`:205`), `.botao.pequeno` 40px (`:149`), `.segmentado button` 40px (`:186`), `.calendario .dia` 40px (`:255`). Acima da linha: `.botao` 48px, `.item` 52px, `.campo` 48px, `.flutuante` 56px, `.marcador` 44px.

## Overall Impression

O núcleo — gerar, validar, ajustar — é genuinamente bem desenhado, em alguns pontos melhor do que a maioria dos produtos pagos dessa categoria. O anel externo — cadastro, configuração, gestão de dados — é um CRUD mobile genérico com três armadilhas sérias: um chip que afirma "validado" antes de validar, confirmações destrutivas que destacam a ação irreversível, e editores que perdem 80 inputs num toque errado. **A maior oportunidade:** o app sabe coisas que não mostra. Ele sabe que o Safari apaga os dados em 7 dias, sabe qual pessoa está sem a última folga, sabe que a escala ficou sem cobertura — e enterra cada um desses saberes três toques longe de onde o usuário está.

## What's Working

**1. O pré-voo do `FolhaGerar.razor` é design de botão desabilitado como se deve fazer.** `Bloqueado => Pendencias.Count > 0` (`:150`), e cada pendência (`:159-173`) vira um alerta com o link que resolve, voltando por `?volta=escala`. O bloco é renderizado **acima** do formulário (`:6-13`) porque o rodapé da sheet cai fora da tela num celular — o comentário em `:5` diz isso com todas as letras. É alguém que testou no aparelho.

**2. O loop violação → conserto → confirmação fecha no lugar.** "Ajustar" (`EscalaPagina.razor:119`) rola a viewport até a semana **e** abre a célula exata; a sheet revalida e imprime **"Resolvido: <a mensagem que sumiu>"** (`FolhaCelula.razor:74`). Nomear o que foi resolvido, em vez de só apagar o aviso, é a diferença entre uma ferramenta que valida e uma que ensina.

**3. O aviso de colisão de folga extra é o app sabendo da falha antes do usuário.** `FolhaGerar.razor:15-25`: detecta, explica como resultado de negócio e oferece **Revezar** num toque.

## Priority Issues

### [P0] O chip diz "Rascunho validado" antes de qualquer regra rodar
- **Por que importa:** "validado" é a única palavra deste app com peso jurídico — é a proposta de valor inteira. `EscalaPagina.razor:19` passa `Bloqueios == 0`, e `_violacoes` é `[]` até `Validar()` terminar (`:223-232`), então `Formatos.cs:126` devolve "Rascunho validado". Pior: `EscalaPagina.razor:36` e `Historico.razor:18` passam `semBloqueio: true` **hardcoded**, então toda escala das duas listas fica permanentemente rotulada como validada, independente do estado real. Um gestor pode exportar e distribuir confiando nesse chip.
- **Correção:** mudar a assinatura para `Estado(Escala e, bool? semBloqueio)` em `Formatos.cs:124`, com `null` renderizando "Rascunho · verificando…". Passar `null` de `EscalaPagina.razor:36` e `Historico.razor:18` (contextos de lista, onde nada foi validado) e de `:19` / `Painel.razor:23` enquanto `_validando`.
- **Comando sugerido:** `/impeccable harden`

### [P0] A confirmação destrutiva usa o botão primário na ação irreversível
- **Por que importa:** `Backup.razor:54` (Apagar tudo), `Backup.razor:46` (Substituir todos os dados), `FichaFuncionario.razor:215` (Remover pessoa) e `OcorrenciasPagina.razor:53` (Excluir) são todos `botao primario` — preenchimento coral com sombra `0 6px 16px` (`app.css:145`) — ao lado de um "Cancelar" de borda simples. Os dados existem só neste aparelho, sem undo e sem cópia no servidor. Num celular, o botão mais chamativo, maior e com memória muscular de "continuar" é o irreversível. Toda convenção que o app ensinou ("coral = seguir") passa a trabalhar contra o usuário exatamente no momento em que não pode.
- **Correção:** inverter. A confirmação destrutiva recebe `.botao perigo` com preenchimento sólido `--rosa-tinta`; "Cancelar" vira `.botao primario` ou no mínimo ganha peso visual igual. Em `Backup.razor:50-56`, acrescentar "Exportar backup antes" como terceira ação dentro do diálogo e colocar o resumo dos dados (`N pessoas · N escalas`, já calculado em `:15`) no corpo.
- **Comando sugerido:** `/impeccable harden`

### [P1] A identidade do turno é uma letra e uma cor que repete a cada 5
- **Por que importa:** `Formatos.Sigla` (`Formatos.cs:161`) devolve `nome[..1]` — "Manhã" e "Meio-dia" renderizam **M** iguais. `ClasseTurno` (`Formatos.cs:147,151`) faz `% 5`, então o turno 6 reusa a cor do turno 1. Os dois alimentam `GradeEscala.razor:46`, ou seja, **toda célula do artefato principal**, mais a legenda (`EscalaPagina.razor:94`) e `FolhaCelula.razor:33`. O `aria-label` (`GradeEscala.razor:44`) está correto, então leitor de tela passa; quem enxerga e quem é daltônico não têm nada. A cor é o único discriminador, sem codificação redundante.
- **Correção:** campo `Sigla` editável (2–3 caracteres) na linha do turno em `EditorFuncoesTurnos.razor:12-18`, default nas duas primeiras letras e desduplicado no save (`:137-156`). Segundo canal não-cromático em `.cel-turno-*` (`app.css:223-227`) — peso de borda inferior ou tratamento de canto por índice. Estender a paleta além de 5 ou limitar a quantidade de turnos com mensagem.
- **Comando sugerido:** `/impeccable colorize`

### [P1] Os editores seguram estado não salvo sem nenhum dirty guard
- **Por que importa:** `EditorDemanda.razor` pode segurar 80+ inputs numéricos de trabalho cuidadoso. `EditorFuncoesTurnos.razor:92` e `EditorDemanda.razor:176` até calculam `Alterado`, mas só para renderizar um botão flutuante de salvar (`:71-77`, `:159-165`); `EditorFuncionamento.razor` não tem equivalente e o objeto `Edicao` de `FichaFuncionario.razor:259` é local ao componente. A nav inferior (`MainLayout.razor:9-15`) é fixa e sempre visível: um toque descarta tudo, em silêncio, e o usuário só descobre quando a escala sai errada.
- **Correção:** o estado já existe em dois dos três. Calcular `Alterado` em `EditorFuncionamento` e `FichaFuncionario` e trancar a navegação com o `<NavigationLock OnBeforeInternalNavigation="..." ConfirmExternalNavigation="true" />` que o próprio Blazor já traz. Zero UI nova, zero componente novo.
- **Comando sugerido:** `/impeccable harden`

### [P2] Até 12 controles ficam acima da grade num celular
- **Por que importa:** `EscalaPagina.razor:16-98` empilha Cabecalho + chip de estado + Publicar (`16-26`), select de escala + Nova + Opções (`28-45`), segmentado de 3 vias (`47-53`), paginador ant/label/próx (`55-59`), selects de Setor e Função (`61-89`) e legenda (`91-98`). `<GradeEscala>` começa em `:100`. Com `.conteudo` a `1.25rem` de topo e `1rem` de gap (`app.css:64,67`), `.botao` a 48px e `.campo input` a 48px, dá algo entre 400 e 480px de cromo — mais da metade de uma viewport de 844px antes do artefato aparecer. Toda visita começa rolando por coisas que o usuário não veio buscar.
- **Correção:** mover Setor, Função e a legenda para a sheet "Opções da escala" (`:140-161`), que já existe e já tem o affordance certo. Dobrar o select de escala dentro do título do Cabecalho como disclosure tocável. Deixar acima da grade só o segmentado de visão e o paginador de data. Seis linhas viram duas.
- **Comando sugerido:** `/impeccable layout`

## Persona Red Flags

**Gestor / dono de loja — abre uma vez por mês, no celular**
- Vê dado velho rotulado como atual: `Painel.razor:131-133` cai para `Dados.Escalas.MaxBy(e => e.Fim)` e `Semana` (`:110-117`) mostra a primeira semana *daquela* escala. O cartão "Cobertura da semana" apresenta a segunda-a-domingo do mês passado sob um título que diz "da semana". A única desambiguação é um `.contagem` cinza de 0.8125rem (`:33`).
- Os dois rótulos do FAB são a mesma ação: `Painel.razor:100` — "Gerar próxima" e "Nova escala" ambos vão para `escala?gerar=1`. A distinção (`ProximaNaoExiste`, `:127`) é real internamente e invisível para ele.
- **Nunca vai ver o aviso de backup.** `Backup.razor:21` diz que o Safari pode apagar tudo depois de 7 dias sem uso. Ele abre o app uma vez por mês. O aviso está em Mais → Aparelho → Backup, três toques de qualquer lugar onde ele passa. O Painel, que ele *vê*, nunca menciona. Ele é exatamente o usuário que perde um ano de dados, e o app sabe disso — o texto existe, só está onde ele não está.
- Mês pulado gera para o passado em silêncio: `FolhaGerar.Sugerir()` (`:104-117`) faz `_inicio = ultima.Fim.AddDays(1)`; pule outubro, abra em novembro e ele propõe uma escala de outubro sem comentar. A nota `Existente` (`:40-58`) só dispara em sobreposição, não em data de início no passado.

**Primeiro uso — montando a loja do zero**
- **A rota de fuga está trancada.** `Inicio.razor:32` — "Explorar com uma loja de exemplo" é `disabled="@(!_entendi)"`. A única ação que permitiria avaliar o produto sem compromisso exige aceitar o aviso legal antes. Não dá para olhar sem assinar. (É também um dos botões que o detector pegou em `low-contrast`: desabilitado a `opacity:.5`, some visualmente na mesma hora em que é a coisa mais importante da tela.)
- Passo 4 quebra com sinal fraco: `EditorFeriados.razor:176` chama `Buscar()` no mount. A falha vira toast de erro (`:191`) e o texto em `:20` então manda tocar "Atualizar nacionais" — que é justamente o que acabou de falhar. Não há caminho "seguir sem feriados".
- **Passo 6 faz uma pergunta que ele não sabe responder.** `EditorDemanda.razor:195` rotula as linhas "Mínimo" e "Ideal" e a tela nunca diz o que cada uma significa nem o que o gerador faz de diferente com elas. O help da tela (`:6-7`) explica só as faixas, não a tabela abaixo. Ele vai chutar, e o chute se propaga para toda escala que ele gerar.
- O checklist manda ele para o lugar errado: `Inicio.razor:147` — "Última folga de todos informada" aponta para `Link(5)`, a lista da equipe, não para quem está faltando. Ele tem que abrir cada ficha e rolar até `<section id="situacao">` (`FichaFuncionario.razor:138`). A âncora existe; **nada no código linka para ela.**
- O checklist some para sempre depois do passo 7. `Inicio` só é alcançável digitando a URL; `Mais.razor` não tem rota de volta.

**Power user — refaz a escala toda semana**
- Não existe "repetir a última". Toda regeração passa pela mesma sheet, embora `Sugerir()` (`:104-117`) já infira "mesma duração, período seguinte" corretamente — a inferência existe e está escondida atrás de três interações.
- A grade é hostil ao teclado: `GradeEscala.razor:3` põe `tabindex="0"` na região de scroll e cada célula (`:42`) é um `<button>`. 7 dias × 12 pessoas = 84 tab stops sequenciais, sem seta, sem pular linha ou coluna. Visão de mês: 372.
- Sem operações em lote — não dá para selecionar a semana de uma pessoa, um dia inteiro, nem trocar duas pessoas de lugar.
- O seletor de escala degrada linearmente: `EscalaPagina.razor:33` renderiza toda escala já criada num `<select>` nativo. 52 escalas semanais viram uma roda de 52 itens no iOS.
- Avisos paginam em 20 (`EscalaPagina.razor:181`, `:130`) sem agrupar por regra ou pessoa e sem filtro por severidade. Ele quer "só os bloqueios" e não tem como pedir.

## Minor Observations

- `app.css:151` — `.botao[disabled] { opacity: .5 }`: o primário desabilitado fica perto de 2.3:1. Isento pela WCAG (componente inativo), mas é o estado em que o primeiro usuário encontra **Gerar** e **Publicar**. Foi o que gerou as 8 flags de `low-contrast` do detector.
- Alvos de toque abaixo de 44px: `.toast button` 32px (`app.css:205`), `.botao.pequeno` 40px (`:149`), `.segmentado button` 40px (`:186`), `.calendario .dia` 40px (`:255`).
- `Formatos.cs:174` — `ClasseAvatar` usa `id.GetHashCode()`. Hash de string/Guid não é estável entre versões do runtime: a cor de uma pessoa pode mudar entre releases.
- `Icone.razor:4` — `Caminhos.GetValueOrDefault(Nome) ?? ""` renderiza um `<svg>` 22×22 vazio para qualquer nome com typo. Falha silenciosa em 30+ pontos de chamada.
- `Loja.cs:42` — `Substituir` sem try/catch, ao contrário de `Alterar` (`:26-40`). Todo caminho de gerar e publicar passa por ele; uma falha de storage no meio derruba no `ErrorBoundary` genérico e leva a escala recém-gerada junto.
- `MainLayout.razor:35` — o único controle de dispensa do toast é rotulado "Ok", inclusive quando `m.Erro` é true e o toast está vermelho.
- `NotFound.razor` — sem `Cabecalho`, sem volta, e `/not-found` não está em `SemCadastro` (`MainLayout.razor:44`), então quem não tem empresa e acerta uma URL ruim é jogado para `/inicio` em vez de ver o 404.
- `EditorFeriados.razor:84-87` — o nome do feriado é um `<button>` com `background:none; border:0; padding:0`. Zero affordance de que abre um editor; lê como texto estático ao lado de um toggle que *parece* interativo.
- `EditorDemanda.razor:195` — `static IEnumerable<Linha> Linhas(Guid _)` recebe e descarta o parâmetro.
- `.botao.perigo` (`app.css:147`) é só contorno com texto `--rosa-tinta`; ao lado do primário coral preenchido, lê como a opção *secundária*. Semântica certa, ênfase invertida.
- `Historico.razor:50`, `FolhaCelula.razor:46`, `EditorDemanda.razor:88` — `<summary style="display: inline-flex">` suprime o marcador nativo de disclosure em alguns engines; o estado aberto/fechado fica sem indicador visual.
- `Exportar.razor:64-67` — chips "PDF" e "WhatsApp" marcados "em breve" dentro da área de ação. `Exportar.razor:55-59` usa 400+ caracteres de estilo inline para o que uma classe `.selecionado` faria numa linha.
- `Formatos.Estado` (`Formatos.cs:124-132`) devolve cinco strings quase sinônimas, e "Publicada · vN" cobre tanto `Publicada` quanto `Alterada` — dois estados de domínio distintos renderizam igual.
- `Mais.razor:55` expõe **Diagnóstico** a todo usuário: 7 botões de desenvolvedor incluindo "Apagar loja de teste" e "Gerar outubro/2026" (`Diagnostico.razor:23-31`).

## Questions to Consider

1. Existem **três formas de gerar uma escala** — o FAB do Painel (`Painel.razor:100`), "Nova" (`EscalaPagina.razor:42`) e "Gerar de novo" (`EscalaPagina.razor:148`) — e a diferença entre elas é invisível no ponto da escolha. Se houvesse exatamente um botão de gerar, o que ele faria, e no que os outros dois virariam?
2. Você pede 84 números antes de produzir qualquer coisa (`EditorDemanda`). E se a primeira escala saísse de uma pergunta só — "quantas pessoas numa terça normal? e num domingo?" — e o modelo de demanda fosse *inferido das correções* que o gestor faz na grade? A grade já é uma superfície de manipulação direta; por que a demanda é digitada em outro lugar?
3. A promessa inteira é "essa escala se defende". Em que momento do fluxo o gestor **vê** essa defesa? O "Relatório de validação" é um checkbox em `Exportar.razor:89` e existe só dentro de um .xlsx. E se publicar produzisse um registro de uma tela — regras verificadas, alertas aceitos, justificativas, quem publicou, quando — que ele pudesse mostrar para alguém?
4. Publicar termina num toast de 4 segundos (`FolhaPublicar.razor:82`, `MainLayout.razor:83`) que devolve o usuário para a mesma grade. O propósito da escala é chegar na equipe, e `Exportar` já faz handoff de share sheet no celular (`folguinha.js:47-58`). Por que publicar não termina lá?
5. Se os dados só vivem neste celular, por que o app nunca pede para protegê-los? Sem prompt depois da primeira escala, sem prompt antes de "Apagar tudo", sem menção no Painel. E se backup fosse um passo do fluxo de publicação em vez de um item de menu?
6. A grade é o produto e é a quarta coisa na própria tela, atrás de uma aba de navegação. Como fica esse app se a grade **for** a home, e a faixa de cobertura do Painel virar uma linha dentro dela?
7. O onboarding são 8 passos lineares e a loja de exemplo está trancada atrás de um checkbox legal. E se o novo usuário caísse *dentro* da loja de exemplo com uma escala já gerada, e o cadastro fosse a segunda coisa — trocar os nomes falsos pelos reais? Ele aprenderia o modelo lendo, em vez de preenchendo.
8. O app detecta a equipe toda folgando na mesma semana e oferece conserto (`FolhaGerar.razor:15-25`). É a coisa mais inteligente da interface. Quais são os outros quatro modos de falha que o domínio conhece, e por que só esse aparece?
