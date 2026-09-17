# Plano de implementação

Fonte: [SPEC.md](../SPEC.md) — seções "Ordem de implementação" e "Skills usadas em cada etapa".
Checkpoints: ao fim de cada fase, `dotnet test Folguinha.slnx` verde + revisão do usuário.

> 16/09/2026: beta mudou de Avalonia nativo para PWA Blazor WebAssembly (ver SPEC.md).

| Fase | Entrega | Depende de |
|---|---|---|
| 1 | Estrutura, docs, git | — |
| 2 | Spike web (IndexedDB + .xlsx no navegador, inclusive Safari/iPhone) | 1 |
| 3 | Domain: modelo + motor de regras | 1 |
| 4 | Domain: gerador por regime | 3 |
| 5 | Domain: feriados, rodízio, ocorrências, substitutos, travas | 4 |
| 6 | Application + Infrastructure (IndexedDB, versões, BrasilAPI) | 5 |
| 7 | Exportação xlsx | 6 |
| 8 | UI Blazor (mockups → telas) | 6 |
| 9 | Publicar PWA e testar instalado no iPhone/Android | 2, 8 |
| 10 | Fase online (API, Postgres, login) | 9 |

Fases 2, 3 e mockups (8) podem andar em paralelo.
