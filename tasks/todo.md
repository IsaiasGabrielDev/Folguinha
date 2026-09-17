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
- [ ] Publish Release + verificação offline + workflow de deploy
