using Folguinha.Domain;

namespace Folguinha.Application.Tests;

public class RepositorioDadosTests
{
    sealed class MemoriaFalsa : IArmazenamento
    {
        public readonly Dictionary<string, string> Itens = [];
        public ValueTask<string?> Ler(string chave) => ValueTask.FromResult(Itens.GetValueOrDefault(chave));
        public ValueTask Gravar(string chave, string valor) { Itens[chave] = valor; return ValueTask.CompletedTask; }
        public ValueTask Remover(string chave) { Itens.Remove(chave); return ValueTask.CompletedTask; }
    }

    static DadosDaUnidade Exemplo()
    {
        var manha = new Turno(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
        var caixa = new Funcao(Guid.NewGuid(), "Caixa");
        var ana = new Funcionario(Guid.NewGuid(), "Ana", Regime.SeisPorUm, TimeSpan.FromHours(44), PermiteHoraExtra: true)
        {
            Funcoes = [caixa.Id],
            TurnoPrincipal = manha.Id,
            Disponibilidades = [new Disponibilidade(DayOfWeek.Sunday, null, null)],
            Situacao = new SituacaoInicial(new DateOnly(2026, 10, 1), TrabalhouUltimoFeriado: true),
            FolgaExtra = new FolgaExtraPeriodica(2, new DateOnly(2026, 10, 5), Posicao: PosicaoFolgaExtra.FimDeSemana),
        };
        return new DadosDaUnidade
        {
            Empresa = new Empresa(Guid.NewGuid(), "Loja Centro", RamoAtividade.Comercio),
            Funcionarios = [ana],
            Turnos = [manha],
            Funcoes = [caixa],
            Demandas = [new Demanda(manha.Id, 1, 2, caixa.Id, DayOfWeek.Monday)],
            Feriados = [new Feriado(new DateOnly(2026, 10, 12), "Nossa Senhora Aparecida", AbrangenciaFeriado.Nacional, Considerado: false)],
            Ocorrencias = [new Ocorrencia(Guid.NewGuid(), ana.Id, TipoOcorrencia.Ferias, new(2026, 11, 2), new(2026, 11, 11))],
            Funcionamento = [new PeriodoFuncionamento(DayOfWeek.Friday, new(8, 0), new(23, 30))],
            Escalas =
            [
                new Escala(Guid.NewGuid(), new(2026, 10, 5), new(2026, 10, 11))
                {
                    Rascunho = [Alocacao.Trabalho(ana.Id, new(2026, 10, 5), new(8, 0), new(16, 20), TimeSpan.FromHours(1), manha.Id, caixa.Id) with { Travada = true }],
                    Versoes = [new VersaoEscala(1, new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.FromHours(-3)), "Gerente", "Primeira publicação",
                        [Alocacao.Folga(ana.Id, new(2026, 10, 5))]) { Afetados = [ana.Id] }],
                },
            ],
            RegrasConfiguradas = [new RegraConfigurada("op.cobertura", Domain.Regras.NivelRegra.Preferencia, new(2026, 10, 1), 2, "Gerente", DateTimeOffset.UnixEpoch)],
        };
    }

    [Fact]
    public async Task Sem_dados_salvos_comeca_vazio()
    {
        var repo = new RepositorioDados(new MemoriaFalsa());

        var dados = await repo.Carregar();

        Assert.Null(dados.Empresa);
        Assert.Empty(dados.Funcionarios);
    }

    [Fact]
    public async Task Salva_e_carrega_tudo_sem_perder_informacao()
    {
        var repo = new RepositorioDados(new MemoriaFalsa());
        var original = Exemplo();

        await repo.Salvar(original);
        var lido = await repo.Carregar();

        Assert.Equal(original.Empresa, lido.Empresa);
        Assert.Equal(original.Turnos, lido.Turnos);
        Assert.Equal(original.Funcoes, lido.Funcoes);
        Assert.Equal(original.Demandas, lido.Demandas);
        Assert.Equal(original.Feriados, lido.Feriados);
        Assert.Equal(original.Ocorrencias, lido.Ocorrencias);
        Assert.Equal(original.Funcionamento, lido.Funcionamento);
        Assert.Equal(original.RegrasConfiguradas, lido.RegrasConfiguradas);
        var escala = Assert.Single(lido.Escalas);
        Assert.Equal(original.Escalas[0].Rascunho, escala.Rascunho);
        var versao = Assert.Single(escala.Versoes);
        Assert.Equal(original.Escalas[0].Versoes[0].Alocacoes, versao.Alocacoes);
        Assert.Equal(original.Escalas[0].Versoes[0].Afetados, versao.Afetados);
        Assert.Equal(original.Escalas[0].Versoes[0].PublicadaEm, versao.PublicadaEm);

        var ana = Assert.Single(lido.Funcionarios);
        var esperado = original.Funcionarios[0];
        Assert.Equal(esperado with { Funcoes = null, Disponibilidades = null }, ana with { Funcoes = null, Disponibilidades = null });
        Assert.Equal(esperado.Funcoes, ana.Funcoes);
        Assert.Equal(esperado.Disponibilidades, ana.Disponibilidades);
    }

    [Fact]
    public void Backup_usa_nomes_legiveis_para_os_enums()
    {
        var json = RepositorioDados.ParaBackup(Exemplo());

        Assert.Contains("\"SeisPorUm\"", json);
        Assert.Contains("\"FimDeSemana\"", json);
    }

    [Fact]
    public void Valores_calculados_nao_vao_para_o_arquivo()
    {
        var json = RepositorioDados.ParaBackup(Exemplo());

        Assert.DoesNotContain("HorasComputadas", json);
        Assert.DoesNotContain("\"Estado\"", json);
    }

    [Fact]
    public void Importa_o_proprio_backup()
    {
        var json = RepositorioDados.ParaBackup(Exemplo());

        var dados = RepositorioDados.DeBackup(json);

        Assert.Equal("Loja Centro", dados?.Empresa?.Nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("isto não é json")]
    [InlineData("{\"VersaoFormato\": 99}")]
    public void Recusa_backup_invalido(string conteudo)
    {
        Assert.Null(RepositorioDados.DeBackup(conteudo));
    }
}
