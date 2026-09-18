# Tarefas

## Fase 1 — Estrutura
- [x] Solution (`Folguinha.slnx`), projetos Domain/Application/Infrastructure/App/Desktop/Android/iOS, testes xUnit
  - Verify: `dotnet build Folguinha.slnx`
- [x] `docs/especificacao.md`, `SPEC.md`, `tasks/`
- [x] Commit inicial

## Fase 2 — Spike web (substitui o spike mobile; beta virou PWA em 16/09)
- [x] Remover projetos Avalonia; criar `src/Folguinha.Web` (Blazor WASM PWA) referenciando Application/Infrastructure
  - Verify: `dotnet build Folguinha.slnx`; `dotnet run --project src/Folguinha.Web` responde e serve `manifest.webmanifest`
- [x] IndexedDB via JS interop (`wwwroot/js/folguinha.js`, `Servicos/Navegador.cs`); `navigator.storage.persist()`; backup exportar/importar (`RepositorioDados`, JSON gerado em compilação)
- [x] .xlsx sem dependências (`Infrastructure/Planilhas/PlanilhaXlsx.cs`), validado contra o schema Open XML (0 erros); entrega por Web Share API no celular ou download
- [x] Página `/diagnostico` com autoteste; `tools/navegador.mjs` (Edge headless via DevTools) → "AUTOTESTE OK"
- [ ] Testar no Safari do iPhone e no Chrome do Android (depende do site publicado — Fase 9)

## Fase 3 — Domain: modelo + regras
- [x] Entidades: Empresa, Funcao, Turno, Funcionario, Demanda, Feriado, Ocorrencia, Alocacao (TenantId/Unidade ficam na persistência)
- [x] `Regra` + `DefinicaoRegra` (nível, severidade, versão, vigência), `Violacao`, `ContextoValidacao`, `Validador`, `CatalogoRegras`
- [x] Regras: jornada diária (5x2/12x36/HE), semanal (+tempo parcial), intrajornada, interjornada, 7º dia, domingo 3/7 semanas, feriado, hora noturna, menor, afastamento (sem expor motivo), cobertura
  - Verify: `dotnet test Folguinha.slnx` → 48 testes
- [x] Disponibilidade por dia/faixa (contratual/permanente/temporária/preferência) + regra de preferência
- [ ] Setor/Unidade no modelo (Fase 6, com a persistência)

## Fase 4 — Gerador (`Folguinha.Domain/Geracao`)
- [x] Planejamento semanal de folgas (capacidade por dia/função, prazo de 6 dias, domingo obrigatório e preferencial)
- [x] Distribuição diária: mínimo → ideal → turno principal; valida cada tentativa com as regras obrigatórias
- [x] 6x1 / 5x2 / personalizado (cota proporcional em semanas parciais) e 12x36 (alternância, 36h)
- [x] Funções, disponibilidade, funcionamento/dias fechados, afastamentos, alocações travadas, histórico entre meses
- [x] Rodízio de feriados (derivado do histórico) + aviso quando não dá
  - Verify: `dotnet test Folguinha.slnx` → 65 testes; estresse manual 20 pessoas × 3 meses ≈ 0,2–0,9 s/mês, sem bloqueios
  - Limitação conhecida: semana antes da folga dominical obrigatória pode ter 5 dias (limite de 6 seguidos)

## Fase 4b — Pedidos do usuário (16/09)
- [x] Situação inicial no cadastro (última folga, último domingo de folga, último feriado) → histórico presumido
- [x] Folga extra periódica (qualquer dia / sábado+domingo / colada na normal) e avulsa (ocorrência `FolgaExtra`)
  - Verify: `dotnet test Folguinha.slnx` → 75 testes
- [ ] UI (Fase 8): perguntar esses campos no cadastro do funcionário e ao gerar a primeira escala; ação "aplicar a todos"

## Fase 5 — Remanejamento
- [x] Feriados considerar/ignorar + rodízio (feito na Fase 4)
- [x] Ocorrência → turnos afetados → substitutos ranqueados (§9.2) → simulação de impacto (`Domain/Remanejo`)
  - Substituto: função, turno, folga no dia; valida regras obrigatórias; tenta mover a folga na semana sem piorar cobertura; calcula hora extra
  - Aplicar: ausência + mudanças travadas, violações e impacto (cobertura, horas extras, afetados)
  - Verify: `dotnet test Folguinha.slnx` → 84 testes
- [ ] Aprovação, nova versão e histórico (Fase 6, com a persistência)

## Fase 6 — Application + Infrastructure
- [x] `Escala` com versões imutáveis, rascunho e estados (§14); `Escalas.Gerar/Ajustar/Publicar/DescartarRascunho/Encerrar/Diferencas`
- [x] Publicação exige zero bloqueios e justificativa para alertas Crítico/Atenção; versão guarda autor, motivo, afetados, regras verificadas e avisos (§12.3)
- [x] `Ocorrencias`: afetados → substitutos → simular → aprovar (nova versão; motivo sem revelar o tipo de afastamento)
- [x] `ConfiguracaoDeRegras`: nível por regra com vigência e versão; regras CLT só ficam iguais ou mais rígidas
- [x] `Feriados` + `BrasilApiFeriados` (Carnaval = ponto facultativo desligado); mescla preserva edições; sem rede → aviso
- [x] Situação inicial entra também na validação (`SituacaoInicialExtensoes.HistoricoPresumido`)
  - Verify: `dotnet test Folguinha.slnx` → 127 testes

## Fase 7 — Exportação
- [x] `ExportacaoEscala`: Calendário (grade colorida com filtro), Lista (formato longo com filtro), uma aba por funcionário (§12.1: identificação, entrada/intervalo sugerido/saída, totais por semana e período, horas extras previstas, domingos/feriados, observações, versão) e Validação (§12.3)
- [x] Exporta rascunho (identificado) ou qualquer versão; ausências sem motivo (LGPD)
  - Verify: 135 testes; exportação completa validada com o OpenXmlValidator (0 erros)

## Fase 8 — Telas (Blazor)
- [x] Sistema visual dos mockups em `wwwroot/css/app.css` (contraste AA, alvos ≥ 44px, barra inferior no celular / lateral no computador)
- [x] Cadastro inicial em 8 passos (`/inicio`) com aviso legal e loja de exemplo; editores reaproveitados em Mais
- [x] Painel, Escala (grade semana/quinzena/mês, filtros, ajuste por célula com revalidação, gerar, publicar com justificativas, descartar, regerar, encerrar)
- [x] Equipe + ficha (resumo do mês, cadastro, disponibilidade, situação inicial, folga extra com "aplicar a todos")
- [x] Ocorrências: lista e fluxo registro → substitutos → impacto → aprovar
- [x] Mais: feriados (BrasilAPI), regras, histórico com diferenças, exportar, backup/importar/apagar, sobre, diagnóstico isolado
  - Verify: `node tools/navegador.mjs http://localhost:5180/ --script tools/e2e-beta.mjs` (390×844 e 1280×900) → roteiro completo sem erros no console

## Fase 9 — Publicação
- [x] Ícones do app (`docs/design/icone.html` → `wwwroot/icon-*.png`, `apple-touch-icon.png`), manifest em pt-BR
- [x] `dotnet publish -c Release` sem avisos; ~3,2 MB com Brotli; gerar um mês (8 pessoas) ≈ 0,4 s no navegador
- [x] Build Release testado: roteiro completo, abertura offline pelo service worker, subpasta estilo GitHub Pages e link direto via 404.html (`tools/servidor-pages.mjs`)
- [x] Workflow `.github/workflows/publicar.yml` (testes + GitHub Pages) e README
- [ ] **Pendente (precisa de conta):** criar o repositório no GitHub, ativar Pages (Source: GitHub Actions) e fazer o push
- [ ] **Pendente (precisa de aparelho):** instalar e testar no iPhone (Safari) e no Android (Chrome)

## Fase 4c — Demanda por faixa de horário (pedido do usuário, 17/09)
- [x] `Demanda` com `TurnoId` nulo = faixa da unidade (`Inicio`/`Fim`): exige N pessoas presentes em
      todo instante do intervalo, somando quem estiver em qualquer turno que cubra aquele instante
  - Motivo: o mínimo por turno obriga a escolher um número para o turno inteiro; quem escolhe o do
    pico cria buraco falso no horário morto. Empresa pequena precisa dizer "3 no pico, 2 na abertura".
- [x] `CoberturaMinima` aponta a hora exata em que a loja fica descoberta ("falta 1 pessoa às 16:20")
- [x] Gerador: `Preencher` reforça o turno que cobre o instante mais vazio; quem sobra vai para onde
      o movimento é maior (`FaltaNoTurno`) e, com a demanda atendida, equilibra os turnos
- [x] `Demanda.PessoasNoDia` — o mínimo do dia estava duplicado no gerador, na grade e no painel
- [x] Tela "Quantas pessoas": card *Horários de mais fluxo* (dias da semana + das/até + nº), agrupado
      por faixa; entra também no passo 6 do cadastro inicial e no checklist
  - Verify: `dotnet test Folguinha.slnx` → 141 testes; loja de exemplo (10 pessoas) cai de 37 para 5
    avisos críticos no mês, todos no fim de semana
- [ ] Alguém que "conta como mais uma" na cobertura sem entrar no rodízio de turnos (supervisora)
- [ ] Avisar quando a folga extra pedida não cabe na equipe, em vez de só listar os críticos:
      com fim de semana inteiro a cada 2 semanas, os 6 dias consecutivos empurram todo mundo para
      folgar sáb ou dom, e sáb+dom juntos comportam no máximo metade da equipe

## Fase 4d — Caso real (recepção de clínica, conversa de 17/09)
Dados reais: aberta **07:00–23:00**, 3 turnos (07:00–15:20, 13:40–22:00, 14:40–23:00),
**6 pessoas** (3 manhã, 3 tarde), mínimo **2 ao mesmo tempo**, pico em seg/qua/qui/sáb
("mas não é regra"), **folga extra só em dia de semana** — o sábado+domingo inteiro vem do
descanso obrigatório, não da folga extra.

- [x] Corrigido: `AplicarATodos` copiava o mesmo `APartirDe` para todos, então a equipe inteira
      folgava no mesmo fim de semana. Agora reveza em `ACadaSemanas` grupos, intercalado por turno
- [x] Corrigido: o planejador de folgas só olhava a sobra **do dia** e esvaziava o turno que cobre
      a abertura ou o fechamento (10 pessoas, 7 folgas num domingo, 1 na noite). `PessoasNoTurno`
      + penalidade no `Pontuar`, espelhando a proteção que já existia para função
  - Verify: clínica com 6 pessoas cai de 32 para 16 críticos em 3 meses; com 10, de 9 para 6
- [x] `PosicaoFolgaExtra.DiaUtil` ("Só de segunda a sexta"): filtra os candidatos no laço de folgas,
      junto de `JuntoDaFolgaNormal`. Pré-alocar antes do laço não funcionava — o domingo obrigatório
      já tinha consumido a folga e a conta `falta - 1` zerava
  - Verify: 8 pessoas, out/2026 — sábado cai de 16 folgas para 0 e o domingo obrigatório se mantém (15)
- [ ] Com `DiaUtil` ninguém pega mais **sábado + domingo inteiro**: o app só força o domingo
      obrigatório, nunca o par. O fim de semana inteiro dela vem de uma regra que o app não tem —
      falta um "fim de semana inteiro a cada N semanas" como descanso obrigatório, separado da
      folga extra (hoje só dá para simular isso com `FimDeSemana`, que é a folga extra)
- [ ] Ainda sobram 16 críticos com 6 pessoas, todos no fim de semana/domingo. A conta: a manhã fica
      sozinha das 07:00 às 13:40 e a noite das 22:00 às 23:00, então cada uma precisa de 2 pessoas
      todo dia = 14 pessoa-dias/semana; a 6 dias por pessoa, 3 pessoas por turno é o limite exato e
      qualquer folga que colida quebra. O turno 13:40–22:00 nunca cobre janela sozinha — não ajuda
      o mínimo. Só fecha com mais gente ou aceitando 1 pessoa nas pontas
- [ ] Baixar o mínimo nas pontas (1 às 07–09 e 22–23) **piorou** (26 críticos): o gerador vê mais
      sobra e distribui mais folga, que depois colide. O planejador ainda é guloso demais
- [ ] A gerente não sabe os horários de pico ("não tem como saber") — o app precisa funcionar bem
      sem esse dado, não só pedir

## Fase 4e — Feriados estaduais (17/09)
- [x] `FeriadosEstaduais`: tabela embutida no Domain com os 27 estados. **Não** usa API: a única
      aberta e sem token (dadosbr.github.io) cobre 5 dos 27 estados e as datas móveis pararam em
      2020; as que cobrem tudo pedem token. Feriado estadual muda por lei e o app é offline-first
- [x] Duas fontes públicas foram cruzadas ([Wikipédia](https://pt.wikipedia.org/wiki/Feriados_no_Brasil)
      e [feriados.com.br](https://blog.feriados.com.br/feriados-estaduais/)) e elas divergem em
      ES, GO, PI, PR, RN e AP. O que as duas confirmam entra ligado; o que só uma lista entra
      desligado, como o Carnaval já fazia
- [x] `Feriados.AplicarEstaduais` pula data já ocupada (não pisa em feriado nacional nem no que o
      gerente cadastrou) e pode ser chamado de novo sem duplicar; `DadosDaUnidade.Uf` guarda o estado
- [x] Tela de Feriados pergunta o estado e mostra quantos estão confirmados e quantos em dúvida
  - Verify: `dotnet test Folguinha.slnx` → 148 testes
- [ ] Feriado municipal continua manual: 5.571 cidades, sem lista pública confiável e de graça
- [ ] Datas móveis estaduais de fora (N. S. da Penha no ES); o Carnaval do RJ já vem do nacional

## Fase 10 — Online (fora do beta)
- [ ] API ASP.NET Core + PostgreSQL reaproveitando Application/Infrastructure; login e perfis; funcionário consultando; notificações; PDF; WhatsApp
