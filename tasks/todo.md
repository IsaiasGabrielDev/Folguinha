# Tarefas

## Fase 1 — Estrutura
- [x] Solution (`Folguinha.slnx`), projetos Domain/Application/Infrastructure/App/Desktop/Android/iOS, testes xUnit
  - Verify: `dotnet build Folguinha.Core.slnf`
- [x] `docs/especificacao.md`, `SPEC.md`, `tasks/`
- [x] Commit inicial

## Fase 2 — Spike mobile
- [ ] Instalar workload Android; app vazio roda no emulador/aparelho
- [ ] EF Core SQLite grava/lê no Android
- [ ] ClosedXML gera xlsx no Android e compartilha via Intent
- [ ] GitHub Actions macOS compila iOS (sem assinatura) — valida AOT de EF Core/ClosedXML

## Fase 3 — Domain: modelo + regras
- [x] Entidades: Empresa, Funcao, Turno, Funcionario, Demanda, Feriado, Ocorrencia, Alocacao (TenantId/Unidade ficam na persistência)
- [x] `Regra` + `DefinicaoRegra` (nível, severidade, versão, vigência), `Violacao`, `ContextoValidacao`, `Validador`, `CatalogoRegras`
- [x] Regras: jornada diária (5x2/12x36/HE), semanal (+tempo parcial), intrajornada, interjornada, 7º dia, domingo 3/7 semanas, feriado, hora noturna, menor, afastamento (sem expor motivo), cobertura
  - Verify: `dotnet test Folguinha.Core.slnf` → 48 testes
- [x] Disponibilidade por dia/faixa (contratual/permanente/temporária/preferência) + regra de preferência
- [ ] Setor/Unidade no modelo (Fase 6, com a persistência)

## Fase 4 — Gerador (`Folguinha.Domain/Geracao`)
- [x] Planejamento semanal de folgas (capacidade por dia/função, prazo de 6 dias, domingo obrigatório e preferencial)
- [x] Distribuição diária: mínimo → ideal → turno principal; valida cada tentativa com as regras obrigatórias
- [x] 6x1 / 5x2 / personalizado (cota proporcional em semanas parciais) e 12x36 (alternância, 36h)
- [x] Funções, disponibilidade, funcionamento/dias fechados, afastamentos, alocações travadas, histórico entre meses
- [x] Rodízio de feriados (derivado do histórico) + aviso quando não dá
  - Verify: `dotnet test Folguinha.Core.slnf` → 65 testes; estresse manual 20 pessoas × 3 meses ≈ 0,2–0,9 s/mês, sem bloqueios
  - Limitação conhecida: semana antes da folga dominical obrigatória pode ter 5 dias (limite de 6 seguidos)

## Fase 4b — Pedidos do usuário (16/09)
- [x] Situação inicial no cadastro (última folga, último domingo de folga, último feriado) → histórico presumido
- [x] Folga extra periódica (qualquer dia / sábado+domingo / colada na normal) e avulsa (ocorrência `FolgaExtra`)
  - Verify: `dotnet test Folguinha.Core.slnf` → 75 testes
- [ ] UI (Fase 8): perguntar esses campos no cadastro do funcionário e ao gerar a primeira escala; ação "aplicar a todos"

## Fase 5 — Remanejamento
- [x] Feriados considerar/ignorar + rodízio (feito na Fase 4)
- [x] Ocorrência → turnos afetados → substitutos ranqueados (§9.2) → simulação de impacto (`Domain/Remanejo`)
  - Substituto: função, turno, folga no dia; valida regras obrigatórias; tenta mover a folga na semana sem piorar cobertura; calcula hora extra
  - Aplicar: ausência + mudanças travadas, violações e impacto (cobertura, horas extras, afetados)
  - Verify: `dotnet test Folguinha.Core.slnf` → 84 testes
- [ ] Aprovação, nova versão e histórico (Fase 6, com a persistência)

## Fase 6–10
Ver tasks/plan.md — detalhar ao chegar.
