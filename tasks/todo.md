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
- [ ] (Fase 4, junto do gerador) Disponibilidade por dia/faixa, Setor/Unidade, regras de preferência (fechamento→abertura, alternância, preferências)

## Fase 4 — Gerador
- [ ] 6x1 / 5x2 com rodízio de domingo
- [ ] 12x36
- [ ] Personalizado
- [ ] Cenário varejo 8 pessoas com demanda menor no fim de semana

## Fase 5 — Feriados e remanejamento
- [ ] Feriados considerar/ignorar + rodízio (quem trabalhou no último folga no próximo)
- [ ] Ocorrências, alocações travadas, substitutos ranqueados

## Fase 6–10
Ver tasks/plan.md — detalhar ao chegar.
