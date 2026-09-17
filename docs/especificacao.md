# Especificação funcional — Gerenciador de Folgas e Escalas

## 1. Visão do produto

Aplicativo gerencial para iPhone e Android destinado à criação, validação, publicação e manutenção de escalas de trabalho e folgas.

O sistema deverá considerar:

- regras trabalhistas aplicáveis;
- regras operacionais da empresa;
- convenções ou acordos coletivos cadastrados;
- demanda mínima de funcionários por dia, horário e função;
- disponibilidade e restrições dos funcionários;
- distribuição equilibrada de turnos, domingos, feriados e folgas;
- alterações motivadas por atestados, licenças, faltas ou compromissos aprovados.

Na primeira versão, o resultado principal será a geração de planilhas individuais e consolidadas. PDF, WhatsApp, integração com ponto e folha de pagamento poderão ser incorporados posteriormente.

> **Aviso:** o aplicativo deverá auxiliar na validação da escala, mas não substituir análise do Departamento Pessoal, contador ou assessoria jurídica. As regras podem variar por categoria profissional, localidade, convenção coletiva e tipo de contrato.

---

## 2. Tipos de regras

O motor de escala deverá separar as regras em três categorias.

### 2.1. Regras legais obrigatórias

Regras que não podem ser violadas durante a geração ou publicação da escala. Quando houver conflito, o sistema deverá impedir a publicação ou exigir tratamento administrativo autorizado.

Exemplos:

- limite de jornada diária e semanal;
- intervalo durante a jornada;
- descanso mínimo entre duas jornadas;
- repouso semanal remunerado;
- regras para domingos e feriados;
- limites de horas extras;
- regras do trabalho noturno;
- regras específicas de escalas como 12×36;
- disposições de convenções e acordos coletivos.

### 2.2. Regras operacionais da empresa

Regras necessárias para manter a unidade funcionando.

Exemplos:

- quantidade mínima de funcionários por faixa de horário;
- presença obrigatória de determinadas funções;
- necessidade de gerente ou supervisor no local;
- horários de abertura, preparação e fechamento;
- qualificações necessárias para determinados postos;
- limite de custo ou de horas extras por período.

### 2.3. Preferências dos funcionários

Condições que o sistema deverá tentar atender, sem tratá-las automaticamente como obrigações legais.

Exemplos:

- preferência pelo turno da manhã;
- preferência de folga em determinado dia;
- preferência por não trabalhar em determinado fim de semana;
- interesse em realizar horas extras;
- preferência de unidade ou setor.

Cada regra configurável deverá ter um nível:

| Nível | Comportamento |
|---|---|
| Obrigatória | Impede geração ou publicação com conflito |
| Alerta | Permite continuar somente com justificativa e permissão adequada |
| Preferência | O gerador tenta respeitar, mas pode flexibilizar |

---

## 3. Cadastro da empresa

O cadastro inicial deverá solicitar:

- razão social e nome fantasia;
- CNPJ;
- ramo de atividade;
- estado e município;
- fuso horário;
- sindicato ou convenção coletiva aplicável;
- horário padrão de funcionamento;
- horários especiais para feriados e datas específicas;
- dias em que a empresa não funciona;
- duração padrão dos intervalos;
- políticas internas de troca, atraso e ausência;
- regras de banco de horas, quando aplicável.

### 3.1. Unidades e setores

O sistema deverá suportar uma ou mais unidades. Cada unidade poderá possuir:

- nome e endereço;
- horário próprio de funcionamento;
- setores;
- funções disponíveis;
- gestores responsáveis;
- feriados locais;
- parâmetros próprios de demanda.

---

## 4. Horário de funcionamento

A empresa deverá informar os horários por dia da semana.

| Dia | Funciona | Abertura | Fechamento |
|---|---:|---:|---:|
| Segunda-feira | Sim | 08:00 | 22:00 |
| Sábado | Sim | 09:00 | 20:00 |
| Domingo | Sim | 10:00 | 18:00 |

Também deverá ser possível cadastrar:

- múltiplos períodos no mesmo dia;
- operação após a meia-noite;
- tempo de preparação antes da abertura;
- tempo de fechamento após o atendimento;
- exceções por data;
- feriados nacionais, estaduais e municipais;
- datas de movimento especial.

---

## 5. Cadastro de funcionários

Cada funcionário deverá possuir:

- nome completo;
- matrícula ou identificador interno;
- unidade e setor;
- cargo e funções que pode exercer;
- habilidades, certificações ou postos autorizados;
- tipo de contrato;
- data de admissão e término, quando aplicável;
- carga horária diária, semanal e mensal;
- modelo de escala: 5×2, 6×1, 12×36 ou personalizado;
- turno principal;
- disponibilidade por dia e faixa de horário;
- disponibilidade para finais de semana e feriados;
- restrições permanentes ou temporárias;
- permissão para horas extras;
- limite configurado de horas extras;
- saldo de banco de horas;
- férias, licenças e afastamentos programados.

### 5.1. Disponibilidade detalhada

A disponibilidade deverá ser cadastrada por dia, em vez de apenas como “disponibilidade total no fim de semana”.

| Dia | Disponível | Horário inicial | Horário final | Preferência |
|---|---:|---:|---:|---|
| Sábado | Sim | 08:00 | 18:00 | Manhã |
| Domingo | Sim | 12:00 | 20:00 | Sem preferência |

O sistema deverá distinguir:

- indisponibilidade contratual;
- restrição permanente;
- preferência pessoal;
- indisponibilidade temporária aprovada;
- afastamento documentado.

---

## 6. Cadastro da demanda operacional

A demanda deverá ser informada por unidade, setor, dia, faixa de horário e função.

| Dia e horário | Total mínimo | Gerentes | Atendentes | Caixas |
|---|---:|---:|---:|---:|
| Segunda, 08h–12h | 5 | 1 | 3 | 1 |
| Sexta, 18h–22h | 8 | 1 | 5 | 2 |
| Domingo, 10h–18h | 3 | 1 | 1 | 1 |

O cadastro deverá permitir:

- número mínimo de funcionários;
- número ideal de funcionários;
- cobertura em intervalos de 30 ou 60 minutos;
- quantidade mínima por função;
- períodos de pico;
- redução de demanda em finais de semana;
- demanda especial por data;
- exigência de qualificações específicas;
- regras de abertura e fechamento;
- custo máximo estimado da escala.

---

## 7. Motor de validação trabalhista

O sistema deverá validar as regras configuradas antes de publicar uma escala.

### 7.1. Validações mínimas

- jornada diária;
- jornada semanal;
- quantidade de dias consecutivos trabalhados;
- intervalo intrajornada;
- intervalo entre jornadas;
- repouso semanal remunerado;
- domingos trabalhados e folgas dominicais;
- feriados trabalhados e compensações;
- horas extras previstas;
- banco de horas;
- trabalho noturno;
- escala 12×36;
- férias e afastamentos;
- regras específicas da convenção coletiva;
- restrições para aprendizes, menores ou categorias especiais, quando aplicável.

### 7.2. Regras versionadas

As regras deverão possuir:

- nome;
- descrição;
- fonte ou fundamento;
- data inicial de vigência;
- data final de vigência, quando houver;
- categoria, localidade ou unidade a que se aplicam;
- severidade;
- versão;
- responsável pela configuração.

Uma alteração futura de regra não deverá modificar retroativamente escalas já encerradas.

### 7.3. Mensagens explicativas

Todo conflito deverá explicar o problema e sugerir uma correção.

Exemplo:

> João terminaria o turno às 23h de terça-feira e começaria às 7h de quarta-feira. O intervalo entre jornadas ficaria abaixo do mínimo configurado.

---

## 8. Geração automática da escala

O gerente deverá selecionar o período desejado — semana, quinzena ou mês — e solicitar a geração automática.

O algoritmo deverá priorizar, nesta ordem:

1. cumprimento das regras obrigatórias;
2. cobertura da demanda mínima;
3. presença das funções necessárias;
4. respeito aos contratos e indisponibilidades;
5. redução de horas extras e custos;
6. distribuição justa de domingos, feriados e turnos menos desejados;
7. atendimento das preferências individuais;
8. estabilidade da escala, evitando alterações desnecessárias.

O sistema deverá evitar, quando possível:

- fechamento seguido de abertura;
- alternância excessiva entre manhã e noite;
- concentração de domingos e feriados nas mesmas pessoas;
- excesso ou déficit de horas;
- dependência de um único funcionário qualificado;
- muitas alterações depois da publicação.

### 8.1. Ajuste manual

O gerente poderá ajustar a escala manualmente, preferencialmente por uma interface de calendário com arrastar e soltar.

Todo ajuste manual deverá ser revalidado imediatamente. O sistema deverá mostrar o impacto sobre:

- cobertura;
- jornada;
- descanso;
- horas extras;
- custo;
- saldo de horas;
- demais funcionários afetados.

---

## 9. Ausências, atestados e remanejamentos

O fluxo de alteração deverá ser:

1. registro da ocorrência;
2. identificação dos turnos afetados;
3. busca de substitutos compatíveis;
4. simulação do impacto legal e operacional;
5. aprovação pelo gestor;
6. criação de nova versão da escala;
7. notificação dos funcionários afetados;
8. registro da confirmação de leitura.

### 9.1. Tipos de ocorrência

- atestado;
- falta justificada;
- falta injustificada;
- férias;
- licença;
- acidente;
- atraso;
- saída antecipada;
- ausência parcial;
- compromisso previamente aprovado;
- convocação extraordinária;
- troca voluntária de turno.

### 9.2. Sugestão de substitutos

Ao buscar um substituto, o sistema deverá avaliar:

- disponibilidade;
- função e qualificação;
- limite de jornada;
- descanso entre jornadas;
- horas extras geradas;
- saldo de banco de horas;
- unidade de trabalho;
- custo;
- equilíbrio na distribuição de turnos.

---

## 10. Troca de turno entre funcionários

Funcionalidade prevista para uma etapa posterior ao MVP:

1. funcionário solicita ou oferece um turno;
2. somente pessoas elegíveis podem se candidatar;
3. o sistema valida jornada, descanso e qualificação;
4. o gestor aprova ou rejeita;
5. uma nova versão da escala é publicada;
6. todo o processo permanece registrado.

Funcionários não deverão alterar diretamente a escala publicada.

---

## 11. Painel gerencial e alertas

O painel deverá apresentar:

- turnos sem cobertura;
- períodos abaixo da demanda mínima;
- funções obrigatórias sem cobertura;
- funcionários com excesso ou falta de horas;
- horas extras previstas;
- descanso insuficiente;
- repouso semanal irregular;
- conflitos em domingos ou feriados;
- funcionário escalado durante férias ou afastamento;
- distribuição desigual de folgas;
- solicitações pendentes;
- mudanças ainda não confirmadas;
- custo estimado da escala;
- comparativo entre demanda mínima, ideal e escala atual.

Os alertas deverão ser classificados por severidade:

- bloqueio;
- crítico;
- atenção;
- informativo.

---

## 12. Planilhas e relatórios

### 12.1. Planilha individual

Deverá conter:

- funcionário;
- matrícula;
- unidade e setor;
- período da escala;
- data e dia da semana;
- horário de entrada;
- início e fim do intervalo;
- horário de saída;
- total diário;
- folgas;
- domingos e feriados trabalhados;
- total semanal e mensal;
- horas extras previstas;
- observações;
- versão e data de publicação.

### 12.2. Planilha consolidada

Deverá apresentar todos os funcionários da unidade em uma visão de calendário, com filtros por:

- período;
- setor;
- função;
- turno;
- funcionário;
- situação da escala.

### 12.3. Relatório de validação

Deverá registrar:

- regras verificadas;
- conflitos encontrados;
- alertas aceitos;
- exceções e justificativas;
- responsável pela geração;
- responsável pela aprovação;
- versão da escala;
- data e hora da publicação.

### 12.4. Escala planejada versus jornada realizada

O aplicativo deverá distinguir claramente:

- **escala planejada:** horário previsto;
- **jornada realizada:** horário efetivamente trabalhado, obtido futuramente por integração com controle de ponto.

A escala planejada não deverá ser apresentada como prova automática da jornada realizada.

---

## 13. Perfis e permissões

| Perfil | Principais permissões |
|---|---|
| Administrador | Configurar empresa, unidades, regras e usuários |
| RH/Departamento Pessoal | Gerenciar contratos, afastamentos e regras trabalhistas |
| Gerente | Gerar, ajustar, aprovar e publicar escalas |
| Supervisor | Consultar e propor ajustes conforme permissão |
| Funcionário | Consultar a própria escala e enviar solicitações |
| Auditor/contador | Consultar relatórios e históricos sem editar |

As permissões deverão poder ser limitadas por unidade e setor.

---

## 14. Ciclo de vida e histórico da escala

A escala deverá seguir os estados:

`Rascunho → Em validação → Validada → Publicada → Alterada → Encerrada`

Uma escala publicada não deverá ser sobrescrita. Toda alteração deverá criar uma nova versão e registrar:

- valor anterior e novo valor;
- autor da alteração;
- data e hora;
- motivo;
- funcionários afetados;
- aprovador;
- alertas existentes;
- notificações enviadas;
- confirmação de leitura.

---

## 15. Segurança, privacidade e LGPD

O sistema tratará dados pessoais e poderá tratar dados sensíveis relacionados à saúde. Deverá incluir:

- controle de acesso por perfil;
- autenticação segura;
- criptografia em trânsito e em repouso;
- registro de acessos e alterações;
- acesso restrito a atestados e documentos médicos;
- coleta somente dos dados necessários;
- política de retenção e exclusão;
- separação entre motivo administrativo e diagnóstico médico;
- possibilidade de atender solicitações dos titulares;
- backups e recuperação de dados;
- encerramento de sessões e proteção contra acesso indevido.

Sempre que possível, o gestor operacional deverá enxergar apenas que existe um afastamento aprovado, sem acessar detalhes médicos desnecessários.

---

## 16. Notificações

Na primeira versão, o sistema poderá utilizar notificações internas. Evoluções futuras poderão incluir:

- push no aplicativo;
- e-mail;
- WhatsApp;
- confirmação de leitura;
- lembrete de turno;
- aviso de alteração;
- solicitação de troca;
- aviso de escala publicada;
- aviso ao gerente sobre ausência ou falta de cobertura.

---

## 17. Escopo recomendado para o MVP

### Incluído

1. Cadastro da empresa, unidade e horário de funcionamento.
2. Cadastro de setores, funções e funcionários.
3. Carga horária, disponibilidade e restrições.
4. Demanda mínima por dia, horário e função.
5. Configuração das principais regras.
6. Geração automática de escala semanal, quinzenal ou mensal.
7. Ajuste manual com validação imediata.
8. Alertas de conflitos.
9. Registro de ausências e remanejamento.
10. Publicação e histórico de versões.
11. Visualização individual e consolidada.
12. Exportação para Excel.
13. Perfis de administrador, gerente e funcionário.

### Fora do primeiro MVP

- PDF;
- integração com WhatsApp;
- integração com relógio de ponto;
- integração com folha de pagamento;
- troca direta entre funcionários;
- previsão automática de demanda;
- cálculo completo de folha e adicionais;
- assinatura eletrônica;
- múltiplas convenções coletivas complexas na mesma unidade.

---

## 18. Critérios básicos de aceite do MVP

O MVP estará funcional quando:

- o gerente conseguir cadastrar uma empresa e seus horários;
- for possível cadastrar funcionários, funções e disponibilidades;
- a demanda mínima puder ser definida por faixa horária;
- o sistema gerar uma escala completa para o período escolhido;
- conflitos obrigatórios forem identificados antes da publicação;
- alterações manuais forem revalidadas imediatamente;
- uma ausência permitir a sugestão de substitutos elegíveis;
- cada publicação criar uma versão preservada no histórico;
- o funcionário visualizar apenas sua própria escala;
- o gerente exportar uma planilha individual e uma consolidada;
- o relatório indicar regras atendidas, alertas e exceções justificadas.

---

## 19. Decisões necessárias antes do desenvolvimento

Antes de desenhar telas, banco de dados e algoritmo, deverão ser definidos:

1. Qual será o primeiro ramo atendido?
2. O produto atenderá uma empresa específica ou será SaaS para várias empresas?
3. Quais modelos de escala entrarão no MVP?
4. A escala será semanal, quinzenal, mensal ou todas essas opções?
5. Quais regras serão fixas e quais serão configuráveis?
6. Quem poderá aceitar exceções e quais exigirão bloqueio absoluto?
7. A empresa já possui sistema de ponto ou folha para futura integração?
8. O funcionário utilizará o aplicativo desde o MVP ou apenas o gerente?
9. Qual será o grau de automação esperado do primeiro gerador?
10. Haverá múltiplas unidades e funcionários compartilhados entre elas?

---

## 20. Evoluções futuras

- exportação em PDF;
- envio de escala por WhatsApp;
- notificações push;
- aceite e confirmação de leitura;
- troca de turnos pelo aplicativo;
- integração com ponto eletrônico;
- integração com folha de pagamento;
- importação de funcionários por planilha;
- previsão de demanda baseada no histórico;
- comparação entre horas previstas e realizadas;
- indicadores de absenteísmo;
- análise de custo da escala;
- sugestões para reduzir horas extras;
- portal web gerencial;
- modo offline para consulta;
- suporte a múltiplas convenções coletivas;
- API para integrações externas.

---

## 21. Referências iniciais

- [Consolidação das Leis do Trabalho — CLT](https://www.planalto.gov.br/ccivil_03/decreto-lei/del5452compilado.htm)
- [Lei nº 605/1949 — repouso semanal remunerado e feriados](https://www.planalto.gov.br/ccivil_03/leis/l0605.htm)
- [Tribunal Superior do Trabalho — jornada de trabalho](https://www.tst.jus.br/jornada-de-trabalho/-/asset_publisher/89Dk/)
- [Lei nº 13.709/2018 — Lei Geral de Proteção de Dados](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm)

As referências deverão ser revisadas durante a implementação e complementadas com as convenções coletivas, normas setoriais e orientações profissionais aplicáveis ao público-alvo do aplicativo.
