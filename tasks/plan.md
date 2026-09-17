# Plano de implementação

Fonte: [SPEC.md](../SPEC.md) — seções "Ordem de implementação" e "Skills usadas em cada etapa".
Checkpoints: ao fim de cada fase, `dotnet test Folguinha.Core.slnf` verde + revisão do usuário.

| Fase | Entrega | Depende de |
|---|---|---|
| 1 | Estrutura, docs, git | — |
| 2 | Spike mobile (EF Core SQLite + ClosedXML em Android/iOS CI) | 1 |
| 3 | Domain: modelo + motor de regras | 1 |
| 4 | Domain: gerador por regime | 3 |
| 5 | Domain: feriados, rodízio, ocorrências, substitutos, travas | 4 |
| 6 | Application + Infrastructure (SQLite, versões, BrasilAPI) | 5 |
| 7 | Exportação xlsx | 6 |
| 8 | UI (mockups → telas) | 6 (mockups podem começar já) |
| 9 | Android aparelho + iOS TestFlight | 2, 8 |
| 10 | Fase online (API, Postgres, login) | 9 |

Fases 2, 3 e mockups (8) podem andar em paralelo.
