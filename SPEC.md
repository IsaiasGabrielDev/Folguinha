# Folguinha: gerenciador de folgas e escalas (iOS + Android)

## Contexto
O gestor precisa montar escalas de trabalho e folga que respeitem a CLT e a operação da empresa. Hoje isso é feito na mão e dá erro: domingo sem folga, descanso entre jornadas desrespeitado, turno sem cobertura. O **Folguinha** é um app gerencial (SaaS, foco inicial no comércio/varejo) que gera, valida, versiona e exporta escalas.

Fonte funcional: [`docs/especificacao.md`](docs/especificacao.md) (doravante **ESPEC**). Este plano define **o que entra no beta** e **como construir**.

### Decisões tomadas
| Tema | Decisão |
|---|---|
| Linguagem / UI | C# (.NET 10 LTS) + Avalonia (o usuário já domina); visual próprio igual em iOS e Android |
| Backend | Arquitetura de backend C# **desde já** (domínio + casos de uso + repositórios). No **beta** ela roda **dentro do app**, com SQLite local e só o gerente usando. Depois a mesma camada vira uma API ASP.NET Core + PostgreSQL online |
| Público | SaaS multiempresa: `TenantId` (empresa) em todas as entidades desde o início |
| Unidades | Modelo com Unidade/Setor, mas o beta opera **uma unidade** |
| Demanda | Por **dia × turno × função** (mínimo e ideal). Faixas de 30/60 min ficam para depois |
| Regimes | 6x1, 5x2, 12x36, meio período/personalizado |
| Período | Semana, quinzena ou mês |
| Saída | .xlsx individual + consolidado + relatório de validação |
| Feriados | BrasilAPI + cache + editar/ignorar/adicionar. Rodízio: quem trabalhou no último feriado tem prioridade de folga |
| iOS | Só o iPhone não basta: é preciso macOS para compilar. Solução: GitHub Actions (runner macOS) + conta Apple Developer (US$ 99/ano) + TestFlight no seu iPhone. O desenvolvimento roda em Desktop + Android |

## Arquitetura (pronta para virar online)
```
Folguinha.Domain        → entidades, value objects, regras CLT, gerador, validador (C# puro, sem I/O)
Folguinha.Application   → casos de uso (GerarEscala, ValidarEscala, RegistrarOcorrencia,
                          SugerirSubstitutos, PublicarVersao, Exportar), interfaces de repositório,
                          IFeriadoProvider, IClock, IUsuarioAtual
Folguinha.Infrastructure→ EF Core (SQLite no beta; PostgreSQL no servidor), BrasilApiFeriadoProvider,
                          exportação .xlsx (ClosedXML)
Folguinha.App           → UI Avalonia (MVVM, CommunityToolkit.Mvvm) chamando Application
                          diretamente no beta (depois: cliente HTTP da API com a mesma interface)
Folguinha.App.Desktop / .Android / .iOS → cabeças de plataforma (compartilhar arquivo nativo)
Folguinha.Api           → (fase online) ASP.NET Core reaproveitando Application + Infrastructure
tests/Folguinha.Domain.Tests, tests/Folguinha.Application.Tests → xUnit
```
**Como o app troca de "local" para "online":** a UI depende de interfaces de serviço (`IEscalaService` etc.). No beta a implementação é local (in-process). Depois vira uma implementação HTTP. As telas não mudam.

**Riscos a validar cedo no iOS (AOT):**
- **EF Core SQLite:** plano B é sqlite-net.
- **ClosedXML:** plano B é MiniExcel.

Local do repositório: `C:\Users\Isaias\source\repos\Folguinha`.

## Escopo do beta (a partir da ESPEC §17, ajustado)
**Entra:**
1. **Empresa e unidade:** nome, CNPJ, ramo, UF/município, fuso, convenção (texto). Horário de funcionamento por dia com **múltiplos períodos**, virada da meia-noite, preparação/fechamento e exceções por data (ESPEC §3–4).
2. **Setores e funções.** **Funcionários** (ESPEC §5): matrícula, função(ões), contrato, admissão/término, carga diária/semanal/mensal, regime, turno principal, **disponibilidade por dia e faixa** com tipo (contratual / restrição permanente / preferência / temporária aprovada / afastamento, §5.1), permissão e limite de horas extras.
   - **Situação inicial** (pedida ao cadastrar e ao montar a primeira escala): data da última folga, último domingo de folga e se trabalhou no último feriado. Sem histórico no app, o gerador continua a sequência a partir disso.
   - **Folga extra periódica** (ex.: +1 a cada 2 semanas), com posição: qualquer dia, sábado + domingo, ou colada na folga normal. Pode ser aplicada a um funcionário ou a todos. Folga extra numa data específica é lançada como ocorrência "folga extra".
3. **Turnos** e **demanda por dia × turno × função** (mínimo/ideal), com demanda especial por data.
4. **Regras configuráveis** com nível **Obrigatória / Alerta / Preferência** (§2) e **versionadas** (fonte, vigência, versão, responsável; §7.2). Mudar uma regra não altera escalas encerradas.
5. **Geração automática** (semana/quinzena/mês) na ordem de prioridade da §8.
6. **Ajuste manual** com revalidação imediata e impacto (cobertura, jornada, descanso, horas extras, afetados). Arrastar e soltar na grade, se o Avalonia mobile permitir bem; senão, tocar + escolher.
7. **Alertas** com severidade Bloqueio / Crítico / Atenção / Informativo e **mensagem explicativa com sugestão** (§7.3).
8. **Ocorrências e remanejamento** (§9): tipos da §9.1, turnos afetados, **substitutos ranqueados** (§9.2), simulação de impacto, aprovação e nova versão.
9. **Ciclo de vida** Rascunho → Em validação → Validada → Publicada → Alterada → Encerrada. Uma escala publicada nunca é sobrescrita: cada alteração gera uma versão com diff, autor, motivo e afetados (§14).
10. **Exportação .xlsx** (§12):
    - planilha **individual** com todas as colunas da §12.1;
    - planilha **consolidada** em calendário, com filtros automáticos do Excel;
    - **relatório de validação** (§12.3).
    Tudo é compartilhado pelo menu nativo.
11. **Painel gerencial** (§11): turnos sem cobertura, funções descobertas, excesso/falta de horas, horas extras previstas, descanso irregular, domingos/feriados, distribuição desigual.
12. **Feriados** (BrasilAPI) e **rodízio de feriado** (detalhes abaixo).
13. **LGPD no beta:**
    - a ocorrência guarda só o **tipo/motivo administrativo**, nunca o diagnóstico;
    - o app pode ser bloqueado com biometria do aparelho;
    - exportar/excluir dados de um funcionário.

**Fica para a fase online:**
- login e perfis (Admin/RH/Gerente/Supervisor/Funcionário/Auditor; o modelo já tem `UsuarioId` e responsável);
- funcionário consultando a própria escala, notificações e confirmação de leitura;
- troca de turnos, multiunidade, banco de horas completo, custo/folha, ponto, PDF, WhatsApp (§10, §16, §20).

## Motor de regras (Domain)
Toda regra implementa `IRegra { Validar(contexto) → Violacao[] }` e traz nível, fundamento, vigência e versão. A mensagem de cada violação cita pessoa, dia e horário e sugere uma correção.

| Regra (padrão) | Base | Nível padrão |
|---|---|---|
| Jornada ≤ 8h/dia (+ até 2h extras) e ≤ 44h/semana | CF art. 7º XIII; CLT arts. 58–59 | Obrigatória |
| Intrajornada: >6h → 1h a 2h; 4h a 6h → 15 min | art. 71 | Obrigatória |
| Interjornada ≥ 11h | art. 66 | Obrigatória |
| DSR de 24h por semana; no máximo 6 dias seguidos | art. 67; Lei 605/49; OJ 410 SDI-1 | Obrigatória |
| Domingo de folga ≥ 1 a cada 3 semanas (comércio) / 7 semanas (demais) | Lei 10.101/2000 art. 6º; Portaria MTP 671/2021 | Obrigatória |
| Feriado trabalhado → folga compensatória ou pagamento em dobro | Lei 605/49 art. 9º | Alerta |
| 12x36: exige acordo; domingos e feriados compensados | art. 59-A | Informativo |
| Tempo parcial: ≤ 30h, ou ≤ 26h + 6h extras | art. 58-A | Obrigatória |
| Hora noturna 22h–5h = 52min30s | art. 73 | Cálculo |
| Menor/aprendiz: sem trabalho noturno | art. 404 | Obrigatória |
| Escalado durante férias/afastamento | operacional | Obrigatória |
| Demanda mínima / função obrigatória não coberta | operacional | Crítico |
| Fechamento seguido de abertura; alternância manhã↔noite | §8 | Preferência |
| Preferências do funcionário | §2.3 | Preferência |

O app mostra sempre o aviso da ESPEC §1: o app não substitui o DP, o contador nem o jurídico, e a convenção coletiva pode alterar as regras.

## Gerador (Domain)
Heurística determinística em C# puro (sem solver nativo, para portar bem no mobile), seguindo as prioridades da §8:
1. **Base de folgas por regime:**
   - **12x36:** alternância em grupos defasados;
   - **6x1 e 5x2:** rodízio de folgas nos dias de menor demanda, garantindo o domingo no prazo legal;
   - **personalizado:** segue a disponibilidade informada.
2. **Cobertura:** para cada dia × turno × função, escolhe candidatos elegíveis:
   - tem a função;
   - está disponível;
   - não tem ocorrência;
   - não viola regra Obrigatória;
   - candidatos que geram menos horas extras vêm primeiro.
3. **Equidade:** equilibra domingos, feriados e turnos menos desejados; atende preferências; evita dependência de uma única pessoa qualificada.
4. **Estabilidade:** ao regerar, mantém alocações **travadas** (manuais, já publicadas ou passadas) e minimiza mudanças.
5. **Validação final:** se algo não fecha, mostra o déficit legível, por exemplo "Sábado / Tarde: falta 1 caixa".

**Rodízio de feriados:** vale apenas nos feriados considerados.
- A prioridade de folga vai para quem **trabalhou no último feriado**.
- Desempate: mais feriados trabalhados no ano, depois menos horas.
- Guarda o `HistoricoFeriado` por funcionário, gravado na publicação.
- Nunca derruba cobertura mínima nem regra Obrigatória. Se não for possível, avisa: "Fulano trabalhou no último feriado e não pôde folgar neste".

**Feriados:**
- **Fonte:** `GET https://brasilapi.com.br/api/feriados/v1/{ano}` (gratuita, sem chave), com cache por ano no banco.
- **Edição:** cada feriado pode ser **Considerado/Ignorado**; o usuário adiciona municipais/estaduais e edita nome e data.
- **Sem internet e sem cache:** cadastro manual.

## Telas (fazer mockups antes de codar; o usuário quer um app bonito)
1. **Onboarding:** Empresa → Funcionamento → Feriados → Setores/Funções → Turnos → Funcionários → Demanda → Regras.
2. **Painel:** alertas por severidade, cobertura e horas.
3. **Escala:** grade funcionário × dia (cores por turno/folga/ocorrência), filtro por setor/função, estado e versão, ajuste manual com impacto.
4. **Funcionário:** calendário individual, horas, domingos e feriados.
5. **Ocorrências:** registrar → afetados → substitutos → simular → aprovar.
6. **Histórico de versões** e **Exportar** (individual / consolidado / relatório).

## Ordem de implementação (fatias verificáveis)
1. Solution, projetos, git, `docs/especificacao.md`, `SPEC.md`, `tasks/plan.md`, `tasks/todo.md`. A cabeça Desktop abre.
2. **Spike iOS/Android:** EF Core SQLite + ClosedXML rodando no Android e no build iOS da CI. Decide os planos B cedo.
3. Domain: entidades (com TenantId) + motor de regras com xUnit (um teste por regra e mensagem).
4. Domain: gerador 6x1/5x2 → 12x36 → personalizado, com cenários (varejo com 8 pessoas, fim de semana com demanda menor, funções).
5. Domain: feriados + rodízio de feriado + ocorrências + substitutos + alocações travadas.
6. Application + Infrastructure: casos de uso, EF Core SQLite, versionamento/ciclo de vida, BrasilAPI com cache.
7. Export: individual, consolidado e relatório de validação.
8. UI: mockups aprovados → onboarding → painel → grade/ajuste → ocorrências → histórico → exportar/compartilhar.
9. Android no aparelho; iOS via GitHub Actions → TestFlight.
10. (Fase online) Folguinha.Api + PostgreSQL + login/perfis + funcionário consultando.

## Skills usadas em cada etapa
| Etapa | Skill |
|---|---|
| Fechar SPEC.md e o capability map dos módulos | `spec-driven-development` |
| Quebra em tarefas (`tasks/plan.md`, `tasks/todo.md`) | `planning-and-task-breakdown` |
| Arquitetura: contratos Domain ↔ Application ↔ UI/API, `IEscalaService`, futura API | `api-and-interface-design` + ADRs com `documentation-and-adrs` |
| Decisões de alto risco (motor CLT, versionamento, migração local → online) | `doubt-driven-development` |
| Barra de qualidade (cobertura das regras, sem testes pulados) | `constraint-driven-development` |
| Layout: mockups de todas as telas (canvas editável) antes de codar | `design` |
| Implementação das telas Avalonia (acessibilidade, responsivo, visual) | `frontend-ui-engineering` |
| Código em fatias com testes | `incremental-implementation` + `test-driven-development` |
| LGPD, dados de saúde, chamada à BrasilAPI | `security-and-hardening` |
| Evitar over-engineering | `ponytail` |
| Git, commits e CI iOS | `git-workflow-and-versioning` + `ci-cd-and-automation` |
| Revisão antes de cada merge | `code-review-and-quality` |

## Verificação
- `dotnet test`: regras CLT, gerador, rodízio de feriado, remanejamento e versionamento passam.
- `dotnet run --project src/Folguinha.App.Desktop`: onboarding completo → gerar o mês → sem violação Obrigatória → registrar atestado → substituto sugerido → publicar v2 → histórico mostra o diff.
- Feriados: carregar o ano da BrasilAPI, ignorar um, adicionar um municipal; gerar dois meses seguidos e conferir que quem trabalhou no feriado anterior folga no seguinte.
- Abrir os .xlsx no Excel: colunas da §12.1, consolidado com filtros, relatório de validação.
- Android (emulador/aparelho) e iOS (TestFlight): gerar e compartilhar a planilha.

## Pontos em aberto (não bloqueiam)
- Art. 386 (revezamento quinzenal com domingo para mulheres): regra opcional?
- Identificador do app: `com.<seu-dominio>.folguinha`?
- Convenção coletiva: no beta, só regras editáveis manualmente (sem biblioteca de CCTs).

## Comandos
```
Build (Windows, sem mobile):  dotnet build Folguinha.Core.slnf
Testes:                       dotnet test Folguinha.Core.slnf
Rodar desktop:                dotnet run --project src/Folguinha.App.Desktop
Android (requer workload):    dotnet workload install android
                              dotnet build src/Folguinha.App.Android -t:Run
iOS:                          só na CI macOS (GitHub Actions) → TestFlight
```

## Estilo de código
- Nomes de domínio em português (`Funcionario`, `Escala`, `Alocacao`); termos técnicos em inglês (`Repository`, `Service`).
- `record`/`sealed class`, `Nullable` ligado, sem exceções para fluxo de regra (violações são dados).
- Domain não referencia nada de I/O; datas via `IClock`.

## Limites
- **Sempre:** rodar `dotnet test` antes de commit; cada regra CLT com teste e fundamento.
- **Perguntar antes:** novas dependências NuGet, mudança de schema já publicado, mudança de nível padrão de regra legal.
- **Nunca:** guardar diagnóstico médico; pular/remover teste falhando; commitar segredos.
