using Folguinha.Domain;
using Folguinha.Domain.Regras;

namespace Folguinha.Application.Tests;

/// Loja com 8 pessoas (3 caixas), manhã/tarde, outubro/2026.
public class CasosDeUsoTests
{
    static readonly DateTimeOffset Agora = new(2026, 9, 30, 18, 0, 0, TimeSpan.FromHours(-3));
    static readonly DateOnly Inicio = new(2026, 10, 1);
    static readonly DateOnly Fim = new(2026, 10, 31);

    readonly Turno _manha = new(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
    readonly Turno _tarde = new(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
    readonly Funcao _caixa = new(Guid.NewGuid(), "Caixa");
    readonly DadosDaUnidade _dados;

    public CasosDeUsoTests()
    {
        string[] nomes = ["Ana", "Bruno", "Carla", "Diego", "Elisa", "Felipe", "Gabi", "Hugo"];
        _dados = new DadosDaUnidade
        {
            NomeGestor = "Ana",
            Empresa = new Empresa(Guid.NewGuid(), "Loja Centro", RamoAtividade.Comercio),
            Funcionarios = [.. nomes.Select((n, i) => new Funcionario(Guid.NewGuid(), n, Regime.SeisPorUm, TimeSpan.FromHours(44))
            {
                Funcoes = i < 3 ? [_caixa.Id] : null,
            })],
            Turnos = [_manha, _tarde],
            Funcoes = [_caixa],
            Demandas =
            [
                .. Enum.GetValues<DayOfWeek>().SelectMany(d => d is DayOfWeek.Saturday or DayOfWeek.Sunday
                    ? new[] { new Demanda(_manha.Id, 1, 1, DiaSemana: d), new Demanda(_tarde.Id, 1, 1, DiaSemana: d) }
                    : [new Demanda(_manha.Id, 2, 2, DiaSemana: d), new Demanda(_tarde.Id, 2, 2, DiaSemana: d)]),
                .. Enum.GetValues<DayOfWeek>().Select(d => new Demanda(_manha.Id, 1, 1, _caixa.Id, d)),
            ],
        };
    }

    static IReadOnlyDictionary<string, string> JustificarTudo(IEnumerable<Violacao> vs) =>
        vs.Where(Escalas.PrecisaJustificativa).ToDictionary(Escalas.Chave, _ => "Combinado com a equipe");

    (DadosDaUnidade Dados, Escala Escala) Publicada()
    {
        var (dados, escala, violacoes) = Escalas.Gerar(_dados, Inicio, Fim);
        var r = Escalas.Publicar(dados, escala.Id, "", JustificarTudo(violacoes), Agora);
        Assert.True(r.Publicado, string.Join("\n", r.Pendencias.Select(p => p.Mensagem)));
        return (r.Dados, r.Dados.Escala(escala.Id)!);
    }

    Funcionario Pessoa(string nome) => _dados.Funcionarios.Single(f => f.Nome == nome);

    /// O Painel reapresentava como problema cru exatamente o que o gestor justificou ao
    /// publicar. A chave tem que casar entre a validação de agora e o alerta gravado na versão.
    [Fact]
    public void Alertas_justificados_na_publicacao_sao_reconhecidos_depois()
    {
        // equipe curta de propósito: a loja cheia gera escala limpa e o teste passaria à toa
        var magra = _dados with { Funcionarios = [.. _dados.Funcionarios.Take(4)] };
        var (gerados, escala0, violacoes) = Escalas.Gerar(magra, Inicio, Fim);
        var precisavam = violacoes.Where(Escalas.PrecisaJustificativa).ToList();
        Assert.NotEmpty(precisavam);

        var r = Escalas.Publicar(gerados, escala0.Id, "", JustificarTudo(violacoes), Agora);
        Assert.True(r.Publicado, string.Join(" · ", r.Pendencias.Select(p => p.Mensagem)));
        var escala = r.Dados.Escala(escala0.Id)!;

        var justificados = Escalas.Justificados(escala);
        var revalidadas = Escalas.Validar(r.Dados, escala).Where(Escalas.PrecisaJustificativa).ToList();

        Assert.NotEmpty(revalidadas);
        Assert.All(revalidadas, v => Assert.Contains(Escalas.Chave(v), justificados));
    }

    [Fact]
    public void Escala_sem_publicacao_nao_tem_alerta_justificado()
    {
        var (_, escala, _) = Escalas.Gerar(_dados, Inicio, Fim);

        Assert.Empty(Escalas.Justificados(escala));
    }

    // ---------- geração e ciclo de vida ----------

    [Fact]
    public void Gerar_cria_um_rascunho_do_periodo()
    {
        var (dados, escala, _) = Escalas.Gerar(_dados, Inicio, Fim);

        Assert.Equal(EstadoEscala.Rascunho, escala.Estado);
        Assert.Equal(31 * 8, escala.Atual.Count);
        Assert.Same(escala, dados.EscalaDoDia(new(2026, 10, 15)));
    }

    [Fact]
    public void Nao_gera_escala_sobreposta()
    {
        var (dados, _, _) = Escalas.Gerar(_dados, Inicio, Fim);

        var erro = Assert.Throws<EscalaException>(() => Escalas.Gerar(dados, new(2026, 10, 20), new(2026, 11, 20)));
        Assert.Contains("01/10/2026 a 31/10/2026", erro.Message);
    }

    [Fact]
    public void Primeira_publicacao_vira_versao_1_com_autor_e_regras_verificadas()
    {
        var (_, escala) = Publicada();

        var v = Assert.Single(escala.Versoes);
        Assert.Equal(EstadoEscala.Publicada, escala.Estado);
        Assert.Null(escala.Rascunho);
        Assert.Equal(1, v.Numero);
        Assert.Equal("Ana", v.Autor);
        Assert.Equal("Primeira publicação", v.Motivo);
        Assert.Equal(Agora, v.PublicadaEm);
        Assert.Equal(8, v.Afetados.Count);
        Assert.Equal(CatalogoRegras.Padrao().Count, v.RegrasVerificadas.Count);
    }

    [Fact]
    public void Alerta_sem_justificativa_impede_publicar()
    {
        var comFeriado = _dados with { Feriados = [new Feriado(new(2026, 10, 12), "Nossa Senhora Aparecida", AbrangenciaFeriado.Nacional)] };
        var (dados, escala, violacoes) = Escalas.Gerar(comFeriado, Inicio, Fim);
        Assert.Contains(violacoes, v => v.RegraId == FeriadoTrabalhado.Padrao.Id);

        var r = Escalas.Publicar(dados, escala.Id, "", new Dictionary<string, string>(), Agora);

        Assert.False(r.Publicado);
        Assert.All(r.Pendencias, p => Assert.True(Escalas.PrecisaJustificativa(p)));
        Assert.Same(dados, r.Dados);
    }

    [Fact]
    public void Bloqueio_impede_publicar_mesmo_com_justificativa()
    {
        var (dados, escala, _) = Escalas.Gerar(_dados, Inicio, Fim);
        var ana = Pessoa("Ana");
        // manhã logo depois de uma tarde → descanso de 10h
        dados = Escalas.Ajustar(dados, escala.Id, Alocacao.Trabalho(ana.Id, new(2026, 10, 14), _tarde.Inicio, _tarde.Fim, _tarde.Intervalo, _tarde.Id)).Dados;
        var ajuste = Escalas.Ajustar(dados, escala.Id, Alocacao.Trabalho(ana.Id, new(2026, 10, 15), _manha.Inicio, _manha.Fim, _manha.Intervalo, _manha.Id));

        var r = Escalas.Publicar(ajuste.Dados, escala.Id, "", JustificarTudo(ajuste.Violacoes), Agora);

        Assert.False(r.Publicado);
        Assert.Contains(r.Pendencias, p => p.Severidade == Severidade.Bloqueio && p.FuncionarioId == ana.Id);
    }

    [Fact]
    public void Ajuste_manual_fica_travado_e_mostra_o_que_mudou_nas_regras()
    {
        var (dados, escala, _) = Escalas.Gerar(_dados, Inicio, Fim);
        var ana = Pessoa("Ana");
        var diaDeTrabalho = escala.Atual.First(a => a.FuncionarioId == ana.Id && a.Trabalha && a.Data.Day > 7);
        var seteSeguidos = Enumerable.Range(0, 7).Select(i => diaDeTrabalho.Data.AddDays(i)).ToList();

        var r = seteSeguidos.Aggregate((ResultadoAjuste?)null, (ultimo, dia) =>
            Escalas.Ajustar(ultimo?.Dados ?? dados, escala.Id, Alocacao.Trabalho(ana.Id, dia, _manha.Inicio, _manha.Fim, _manha.Intervalo, _manha.Id)))!;

        var editada = r.Dados.Escala(escala.Id)!;
        Assert.All(seteSeguidos, d => Assert.True(editada.Atual.Single(a => a.FuncionarioId == ana.Id && a.Data == d).Travada));
        Assert.Contains(r.Violacoes, v => v.RegraId == DiasConsecutivos.Padrao.Id && v.FuncionarioId == ana.Id);
    }

    [Fact]
    public void Novas_violacoes_do_ajuste_sao_destacadas_e_as_resolvidas_tambem()
    {
        var (dados, escala, _) = Escalas.Gerar(_dados, Inicio, Fim);
        var ana = Pessoa("Ana");
        var dia = new DateOnly(2026, 10, 15);
        var noturno = Alocacao.Trabalho(ana.Id, dia, new(8, 0), new(20, 0), TimeSpan.FromHours(1), _manha.Id);

        var piora = Escalas.Ajustar(dados, escala.Id, noturno);
        var melhora = Escalas.Ajustar(piora.Dados, escala.Id, Alocacao.Folga(ana.Id, dia));

        Assert.Contains(piora.Novas, v => v.RegraId == JornadaDiaria.Padrao.Id);
        Assert.Contains(melhora.Resolvidas, v => v.RegraId == JornadaDiaria.Padrao.Id);
    }

    [Fact]
    public void Ajuste_depois_de_publicar_nao_altera_a_versao_e_gera_versao_2()
    {
        var (dados, escala) = Publicada();
        var hugo = Pessoa("Hugo");
        var dia = escala.Atual.First(a => a.FuncionarioId == hugo.Id && a.Trabalha && a.TurnoId == _manha.Id && a.Data.Day > 3).Data;

        var ajuste = Escalas.Ajustar(dados, escala.Id, Alocacao.Folga(hugo.Id, dia));
        var emAlteracao = ajuste.Dados.Escala(escala.Id)!;
        Assert.Equal(EstadoEscala.EmValidacao, emAlteracao.Estado);
        Assert.True(emAlteracao.Versoes[0].Alocacoes.Single(a => a.FuncionarioId == hugo.Id && a.Data == dia).Trabalha);

        var r = Escalas.Publicar(ajuste.Dados, escala.Id, "Hugo pediu folga", JustificarTudo(ajuste.Violacoes), Agora.AddDays(3));

        Assert.True(r.Publicado, string.Join("\n", r.Pendencias.Select(p => p.Mensagem)));
        var final = r.Dados.Escala(escala.Id)!;
        Assert.Equal(EstadoEscala.Alterada, final.Estado);
        Assert.Equal(2, final.Versoes[1].Numero);
        Assert.Equal("Hugo pediu folga", final.Versoes[1].Motivo);
        Assert.Equal([hugo.Id], final.Versoes[1].Afetados);
        var mudanca = Assert.Single(Escalas.Diferencas(final.Versoes[0].Alocacoes, final.Versoes[1].Alocacoes));
        Assert.Equal(dia, mudanca.Data);
        Assert.Equal("Folga", Escalas.Descrever(r.Dados, mudanca.Depois));
        Assert.StartsWith("Manhã 08:00", Escalas.Descrever(r.Dados, mudanca.Antes));
    }

    [Fact]
    public void Justificativas_ficam_registradas_na_versao()
    {
        var (_, escala) = Publicada();

        Assert.All(escala.Versoes[0].AlertasAceitos, a => Assert.Equal("Combinado com a equipe", a.Justificativa));
        Assert.Equal(escala.Versoes[0].Violacoes.Count(Escalas.PrecisaJustificativa), escala.Versoes[0].AlertasAceitos.Count);
    }

    [Fact]
    public void Descartar_volta_para_a_versao_publicada()
    {
        var (dados, escala) = Publicada();
        var ana = Pessoa("Ana");
        var ajustado = Escalas.Ajustar(dados, escala.Id, Alocacao.Folga(ana.Id, new(2026, 10, 20))).Dados;

        var descartado = Escalas.DescartarRascunho(ajustado, escala.Id);

        Assert.Equal(EstadoEscala.Publicada, descartado.Escala(escala.Id)!.Estado);
    }

    [Fact]
    public void Escala_encerrada_nao_aceita_mudancas()
    {
        var (dados, escala) = Publicada();
        var encerrada = Escalas.Encerrar(dados, escala.Id);

        Assert.Equal(EstadoEscala.Encerrada, encerrada.Escala(escala.Id)!.Estado);
        Assert.Throws<EscalaException>(() => Escalas.Ajustar(encerrada, escala.Id, Alocacao.Folga(Pessoa("Ana").Id, new(2026, 10, 20))));
        Assert.Throws<EscalaException>(() => Escalas.Gerar(encerrada, Inicio, Fim));
    }

    [Fact]
    public void Mes_seguinte_continua_do_mes_publicado()
    {
        var (dados, _) = Publicada();

        var (_, novembro, violacoes) = Escalas.Gerar(dados, new(2026, 11, 1), new(2026, 11, 30));

        Assert.DoesNotContain(violacoes, v => v.Severidade == Severidade.Bloqueio);
        Assert.Equal(30 * 8, novembro.Atual.Count);
    }

    [Fact]
    public void Regerar_mantem_os_ajustes_travados()
    {
        var (dados, escala, _) = Escalas.Gerar(_dados, Inicio, Fim);
        var ana = Pessoa("Ana");
        var folga = Alocacao.Folga(ana.Id, new(2026, 10, 21));
        dados = Escalas.Ajustar(dados, escala.Id, folga).Dados;

        var (_, regerada, _) = Escalas.Gerar(dados, Inicio, Fim);

        Assert.Equal(escala.Id, regerada.Id);
        Assert.Contains(folga with { Travada = true }, regerada.Atual);
    }

    [Fact]
    public void Situacao_inicial_entra_na_validacao()
    {
        var ana = Pessoa("Ana") with { Situacao = new SituacaoInicial(new DateOnly(2026, 9, 26)) };
        var dados = _dados with { Funcionarios = [.. _dados.Funcionarios.Where(f => f.Nome != "Ana"), ana] };
        var (gerado, escala, _) = Escalas.Gerar(dados, Inicio, Fim);

        // força trabalho de 01 a 03/10 depois de 27 a 30/09 → 7 dias seguidos
        var r = new[] { 1, 2, 3 }.Aggregate(gerado, (d, dia) =>
            Escalas.Ajustar(d, escala.Id, Alocacao.Trabalho(ana.Id, new(2026, 10, dia), _manha.Inicio, _manha.Fim, _manha.Intervalo, _manha.Id)).Dados);

        Assert.Contains(Escalas.Validar(r, r.Escala(escala.Id)!), v => v.RegraId == DiasConsecutivos.Padrao.Id && v.Data == new DateOnly(2026, 10, 3));
    }

    // ---------- ocorrências ----------

    [Fact]
    public void Aprovar_ocorrencia_publica_nova_versao_sem_revelar_o_motivo()
    {
        var (dados, escala) = Publicada();
        var bruno = Pessoa("Bruno");
        // primeiro dia de trabalho do Bruno (a partir do dia 8) que tem substituto possível
        var (turno, atestado, substituto) = escala.Atual
            .Where(a => a.FuncionarioId == bruno.Id && a.Trabalha && a.Data.Day >= 8)
            .Select(a =>
            {
                var o = new Ocorrencia(Guid.NewGuid(), bruno.Id, TipoOcorrencia.Atestado, a.Data, a.Data);
                var afetado = Assert.Single(Ocorrencias.TurnosAfetados(dados, o));
                return (a, o, s: Ocorrencias.Substitutos(dados, o, afetado).FirstOrDefault(s => s.Elegivel));
            })
            .First(x => x.s is not null);
        var simulacao = Ocorrencias.Simular(dados, atestado, [substituto!]);
        var r = Ocorrencias.Aprovar(dados, atestado, [substituto!], JustificarTudo(simulacao[0].Resultado.Violacoes), Agora.AddDays(10));

        Assert.Empty(r.Pendencias);
        var final = r.Dados.Escala(escala.Id)!;
        Assert.Equal(2, final.Versoes.Count);
        Assert.Contains("Ausência aprovada de Bruno", final.Versoes[1].Motivo);
        Assert.DoesNotContain("Atestado", final.Versoes[1].Motivo);
        Assert.Contains(bruno.Id, final.Versoes[1].Afetados);
        Assert.Contains(substituto!.Funcionario.Id, final.Versoes[1].Afetados);
        Assert.Contains(atestado, r.Dados.Ocorrencias);
        Assert.Equal(TipoAlocacao.Ocorrencia, final.Atual.Single(a => a.FuncionarioId == bruno.Id && a.Data == turno.Data).Tipo);
        Assert.Equal(1.0, r.Impacto.Cobertura);
    }

    [Fact]
    public void Ocorrencia_sem_turnos_afetados_so_e_registrada()
    {
        var (dados, escala) = Publicada();
        var atraso = new Ocorrencia(Guid.NewGuid(), Pessoa("Ana").Id, TipoOcorrencia.Atraso, new(2026, 10, 6), new(2026, 10, 6));

        var r = Ocorrencias.Aprovar(dados, atraso, [], new Dictionary<string, string>(), Agora);

        Assert.Empty(Ocorrencias.TurnosAfetados(dados, atraso));
        Assert.Contains(atraso, r.Dados.Ocorrencias);
        Assert.Single(r.Dados.Escala(escala.Id)!.Versoes);
    }

    [Fact]
    public void Ocorrencia_em_rascunho_so_atualiza_o_rascunho()
    {
        var (dados, escala, _) = Escalas.Gerar(_dados, Inicio, Fim);
        var ana = Pessoa("Ana");
        var ferias = new Ocorrencia(Guid.NewGuid(), ana.Id, TipoOcorrencia.Ferias, new(2026, 10, 19), new(2026, 10, 25));

        var r = Ocorrencias.Aprovar(dados, ferias, [], new Dictionary<string, string>(), Agora);

        var atual = r.Dados.Escala(escala.Id)!;
        Assert.Empty(atual.Versoes);
        Assert.All(atual.Atual.Where(a => a.FuncionarioId == ana.Id && ferias.Cobre(a.Data)), a => Assert.False(a.Trabalha));
    }

    // ---------- regras configuradas ----------

    [Fact]
    public void Configuracao_de_regra_vale_a_partir_da_vigencia()
    {
        var dados = ConfiguracaoDeRegras.Configurar(_dados, CoberturaMinima.Padrao.Id, NivelRegra.Preferencia, new(2026, 11, 1), Agora);

        var outubro = Escalas.Regras(dados, new(2026, 10, 1)).Single(r => r.Definicao.Id == CoberturaMinima.Padrao.Id);
        var novembro = Escalas.Regras(dados, new(2026, 11, 1)).Single(r => r.Definicao.Id == CoberturaMinima.Padrao.Id);

        Assert.Equal(NivelRegra.Alerta, outubro.Definicao.Nivel);
        Assert.Equal(NivelRegra.Preferencia, novembro.Definicao.Nivel);
        Assert.Equal(2, novembro.Definicao.Versao);
        Assert.Equal("Ana", novembro.Definicao.Responsavel);
    }

    [Fact]
    public void Regra_da_clt_nao_pode_ficar_mais_branda()
    {
        Assert.Throws<EscalaException>(() =>
            ConfiguracaoDeRegras.Configurar(_dados, Interjornada.Padrao.Id, NivelRegra.Alerta, Inicio, Agora));
    }

    [Fact]
    public void Regra_da_clt_pode_ficar_mais_rigida()
    {
        var dados = ConfiguracaoDeRegras.Configurar(_dados, FeriadoTrabalhado.Padrao.Id, NivelRegra.Obrigatoria, Inicio, Agora);

        Assert.Equal(NivelRegra.Obrigatoria, Escalas.Regras(dados, Inicio).Single(r => r.Definicao.Id == FeriadoTrabalhado.Padrao.Id).Definicao.Nivel);
    }

    [Fact]
    public void Mudar_regra_depois_nao_altera_a_versao_publicada()
    {
        var (dados, escala) = Publicada();

        var depois = ConfiguracaoDeRegras.Configurar(dados, CoberturaMinima.Padrao.Id, NivelRegra.Preferencia, new(2026, 11, 1), Agora);

        Assert.Equal(escala.Versoes, depois.Escala(escala.Id)!.Versoes);
        Assert.Equal(NivelRegra.Alerta, depois.Escala(escala.Id)!.Versoes[0].RegrasVerificadas.Single(r => r.Id == CoberturaMinima.Padrao.Id).Nivel);
    }

    // ---------- feriados ----------

    sealed class ApiFalsa(params Feriado[] feriados) : IFeriadoProvider
    {
        public bool Falhar { get; init; }
        public Task<IReadOnlyList<Feriado>> Buscar(int ano, CancellationToken cancelamento = default) =>
            Falhar ? throw new HttpRequestException("sem rede") : Task.FromResult<IReadOnlyList<Feriado>>(feriados);
    }

    static Feriado Nacional(int mes, int dia, string nome) => new(new(2026, mes, dia), nome, AbrangenciaFeriado.Nacional);

    [Fact]
    public async Task Atualizar_feriados_preserva_edicoes_e_manuais()
    {
        var municipal = new Feriado(new(2026, 12, 8), "Nossa Senhora da Conceição", AbrangenciaFeriado.Municipal, Origem: OrigemFeriado.Manual);
        var dados = _dados with
        {
            Feriados = [Nacional(10, 12, "Aparecida (renomeado)") with { Considerado = false }, municipal],
        };

        var (atualizado, erro) = await Feriados.Atualizar(dados,
            new ApiFalsa(Nacional(10, 12, "Nossa Senhora Aparecida"), Nacional(11, 2, "Finados")), 2026);

        Assert.Null(erro);
        Assert.Equal(3, atualizado.Feriados.Count);
        var aparecida = atualizado.Feriados.Single(f => f.Data.Month == 10);
        Assert.False(aparecida.Considerado);
        Assert.Equal("Aparecida (renomeado)", aparecida.Nome);
        Assert.Contains(municipal, atualizado.Feriados);
        Assert.Contains(2026, atualizado.AnosDeFeriadosCarregados);
    }

    [Fact]
    public async Task Sem_internet_mantem_os_dados_e_avisa()
    {
        var (atualizado, erro) = await Feriados.Atualizar(_dados, new ApiFalsa { Falhar = true }, 2026);

        Assert.Same(_dados, atualizado);
        Assert.Contains("cadastre manualmente", erro);
    }

    [Fact]
    public void Feriado_nacional_nao_muda_de_data_nem_e_apagado()
    {
        var aparecida = Nacional(10, 12, "Nossa Senhora Aparecida");
        var dados = _dados with { Feriados = [aparecida] };

        Assert.Throws<EscalaException>(() => Feriados.Salvar(dados, aparecida, aparecida with { Data = new(2026, 10, 13) }));
        Assert.Throws<EscalaException>(() => Feriados.Remover(dados, aparecida));
        Assert.False(Feriados.Salvar(dados, aparecida, aparecida with { Considerado = false }).Feriados.Single().Considerado);
    }

    [Fact]
    public void Nao_aceita_dois_feriados_na_mesma_data()
    {
        var dados = _dados with { Feriados = [Nacional(10, 12, "Aparecida")] };

        Assert.Throws<EscalaException>(() => Feriados.Salvar(dados, null,
            new Feriado(new(2026, 10, 12), "Outro", AbrangenciaFeriado.Municipal, Origem: OrigemFeriado.Manual)));
    }
}
