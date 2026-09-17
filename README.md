# Folguinha

Gerenciador de escalas e folgas conforme a CLT, para o gerente montar, validar, publicar e exportar a escala da loja pelo celular ou computador.

- **Beta:** PWA em Blazor WebAssembly (.NET 10). Instala na tela inicial do iPhone e do Android e funciona offline. Os dados ficam só no aparelho (IndexedDB), com backup em arquivo.
- **Motor em C#:** regras da CLT, gerador automático, remanejamento, versões e exportação `.xlsx`.
- **Especificação:** [SPEC.md](SPEC.md) (decisões e escopo) e [docs/especificacao.md](docs/especificacao.md) (requisitos funcionais). O andamento está em [tasks/todo.md](tasks/todo.md).

> O Folguinha auxilia na validação da escala, mas não substitui o Departamento Pessoal, o contador ou a assessoria jurídica.

## Estrutura

```
src/Folguinha.Domain          regras CLT, gerador de escala, remanejamento (C# puro, sem I/O)
src/Folguinha.Application     casos de uso: escalas versionadas, publicação, ocorrências, regras, feriados, backup
src/Folguinha.Infrastructure  BrasilAPI (feriados) e planilhas .xlsx sem dependências
src/Folguinha.Web             PWA Blazor WebAssembly (telas, IndexedDB, compartilhar arquivo)
tests/                        xUnit (Domain, Application, Infrastructure)
tools/                        testes de ponta a ponta no navegador e servidor estático de teste
docs/design/                  mockups aprovados e ícone
```

## Comandos

```bash
dotnet build Folguinha.slnx                       # compila tudo
dotnet test Folguinha.slnx                        # testes (136)
dotnet run --project src/Folguinha.Web            # app em http://localhost:5xxx
dotnet publish src/Folguinha.Web -c Release -o publicado   # site estático em publicado/wwwroot
```

Teste de ponta a ponta (precisa do Node.js e do Edge ou Chrome):

```bash
dotnet run --project src/Folguinha.Web --urls http://localhost:5180
node tools/navegador.mjs http://localhost:5180/ --script tools/e2e-beta.mjs                            # celular 390×844
node tools/navegador.mjs http://localhost:5180/ --script tools/e2e-beta.mjs --largura 1280 --altura 900
node tools/navegador.mjs http://localhost:5190/ --script tools/e2e-offline.mjs   # sobre o site publicado
```

As fotos das telas vão para `artefatos/` (ou para a pasta em `FOTOS`). A página `/diagnostico` roda um autoteste numa loja separada, sem tocar nos dados reais.

## Publicar (GitHub Pages)

O workflow [.github/workflows/publicar.yml](.github/workflows/publicar.yml) testa e publica a cada push na `main`.

1. Crie um repositório no GitHub e envie este projeto (`git remote add origin …` e `git push -u origin main`).
2. No repositório, abra **Settings → Pages** e, em **Source**, escolha **GitHub Actions**.
3. Depois do primeiro push, o site fica em `https://<usuário>.github.io/<repositório>/`.

A base do app se ajusta sozinha à subpasta do Pages, e o `404.html` permite abrir links diretos. Outras hospedagens estáticas com HTTPS (Cloudflare Pages, Azure Static Web Apps) também servem: basta publicar a pasta `publicado/wwwroot`.

## Instalar no celular

- **iPhone (Safari):** abra o endereço, toque em Compartilhar e depois em **Adicionar à Tela de Início**.
- **Android (Chrome):** abra o endereço, toque no menu (⋮) e depois em **Instalar app**.

Instalar é importante: sem isso, o Safari pode apagar os dados de sites que ficam 7 dias sem uso. Faça backup em **Mais → Backup e dados**.

## Limitações conhecidas do beta

- Um aparelho e um gerente. Login, perfis, funcionário consultando a escala, notificações e sincronização ficam para a fase online (API ASP.NET Core + PostgreSQL).
- PDF e envio por WhatsApp ainda não existem.
- No 6x1, a semana anterior a uma folga de domingo obrigatória pode ter 5 dias de trabalho, para não passar de 6 dias seguidos.
- O horário do intervalo na planilha é uma sugestão (meio da jornada); o app guarda só a duração.
- As fontes (Google Fonts) só aparecem com internet; offline, o app usa as fontes do sistema.
