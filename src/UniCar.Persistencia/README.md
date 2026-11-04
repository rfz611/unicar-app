# UniCar — persistência com Entity Framework Core e SQLite

Entrega "Projeto - Persistência com Entity Framework". É um programa de console que guarda os dados do UniCar
(universitários, veículos, caronas marcadas e reservas) num banco SQLite, usando o Entity Framework Core 8.

O programa é separado da API do app (`src/UniCar.Api`, que usa PostgreSQL). Os campos foram baseados nas
entidades da API (`src/UniCar.Api/Domain`), com nomes em português.

## Como rodar

Precisa do .NET 8 SDK. Na pasta deste projeto:

```powershell
cd src\UniCar.Persistencia
dotnet run
```

Na primeira vez, o programa cria o arquivo `unicar.db` nesta pasta e aplica as migrations (`Database.Migrate()`).
O banco já vem com as cinco faculdades e três regras de carona.

Modo automático, para o vídeo: cria um usuário, um veículo e uma carona, lista, muda o horário, reserva, exclui e
mostra o SQL de cada passo. No fim apaga o que criou, então pode rodar quantas vezes quiser.

```powershell
dotnet run -- --demo
```

Testes (na raiz do repositório):

```powershell
dotnet test tests\UniCar.Persistencia.Tests
```

Migrations: ficam em `Migracoes/`. Para criar uma nova depois de mudar uma entidade (na pasta do projeto):

```powershell
dotnet tool restore
dotnet ef migrations add NomeDaMudanca --output-dir Migracoes
```

## Menu

| Opção | O que faz |
|---|---|
| 1. Usuários | cadastrar (cria as preferências junto), listar com a faculdade, editar, excluir |
| 2. Veículos | cadastrar, listar, excluir |
| 3. Caronas | publicar (escolhendo veículo e regras), listar com motorista, vagas livres e regras, editar horário e vagas, excluir |
| 4. Reservas | reservar vaga, listar por carona, cancelar |
| 5. Mostrar o SQL | liga ou desliga o log dos comandos que o EF manda ao banco (INSERT, SELECT, UPDATE, DELETE) |
| 6. Resumo do banco | quantas linhas tem cada tabela |

## Entidades

| Entidade | Tabela | Campos | Anotações |
|---|---|---|---|
| `Universidade` | Universidades | Id, Nome, Sigla, DominioEmail | `[Key]`, `[Required]`, `[MaxLength]`, `[Index(Sigla, IsUnique)]` |
| `Usuario` | Usuarios | Id, NomeCompleto, Email, Telefone, Curso, Periodo, UniversidadeId | `[Required]`, `[EmailAddress]`, `[Range(1,12)]`, `[ForeignKey]`, `[Index(Email, IsUnique)]` |
| `PreferenciasUsuario` | Preferencias | UsuarioId, TemaEscuro, AvisosDeCarona, MostrarCurso | `[Key]` + `[ForeignKey]` na mesma coluna |
| `Veiculo` | Veiculos | Id, Placa, Modelo, Cor, MotoristaId | `[Required]`, `[RegularExpression]` na placa, `[Index(Placa, IsUnique)]` |
| `Carona` | Caronas | Id, MotoristaId, VeiculoId, Origem, Destino, SaidaEm, Vagas, PrecoPorVaga, Status | `[MaxLength(200)]`, `[Range(1,4)]` nas vagas, `[NotMapped]` em VagasLivres |
| `Reserva` | Reservas | Id, CaronaId, PassageiroId, Preco, Status, CriadaEm | `[Index(CaronaId, PassageiroId, IsUnique)]` |
| `RegraCarona` | Regras | Id, Nome | `[Required]`, `[MaxLength]` |
| (junção do EF) | CaronaRegraCarona | CaronasId, RegrasId | criada sozinha pelo EF |

`SaidaEm` e `CriadaEm` são `DateTime`, não `DateTimeOffset`, porque o SQLite não sabe ordenar `DateTimeOffset`.
`Status` da carona é o enum `Marcada`/`Cancelada`; o da reserva, `Confirmada`/`Cancelada`.

## Relacionamentos

| Tipo | Entre | Como foi feito |
|---|---|---|
| Um para um | Usuario e PreferenciasUsuario | `PreferenciasUsuario.UsuarioId` com `[Key]` e `[ForeignKey(nameof(Usuario))]`: a chave primária é a própria FK. Navegação `Usuario.Preferencias` e `PreferenciasUsuario.Usuario`. |
| Um para muitos | Universidade → Usuarios | FK `Usuario.UniversidadeId` com `[ForeignKey]`; coleção `Universidade.Usuarios`. Fluent API: `Restrict`. |
| Um para muitos | Usuario (motorista) → Veiculos | FK `Veiculo.MotoristaId`; coleção `Usuario.Veiculos` com `[InverseProperty]`. Excluir o usuário apaga os veículos (cascata padrão). |
| Um para muitos | Usuario (motorista) → Caronas | FK `Carona.MotoristaId`; coleção `Usuario.CaronasOferecidas` com `[InverseProperty]`. Fluent API: `Restrict`. |
| Um para muitos | Veiculo → Caronas | FK `Carona.VeiculoId`; coleção `Veiculo.Caronas`. Fluent API: `Restrict`. |
| Um para muitos | Carona → Reservas | FK `Reserva.CaronaId`; coleção `Carona.Reservas`. Fluent API: `Cascade`, excluir a carona apaga as reservas. |
| Muitos para muitos com entidade de junção | Usuario (passageiro) e Carona | Pela entidade `Reserva`, que tem as duas FKs e dados próprios (preço, status, data). Fluent API: passageiro → reservas com `Restrict`. |
| Muitos para muitos simples | Carona e RegraCarona | `List<RegraCarona> Regras` em Carona e `List<Carona> Caronas` em RegraCarona. Sem configuração: o EF cria a tabela `CaronaRegraCarona`. |

A Fluent API (`Dados/UniCarContext.cs`, `OnModelCreating`) só aparece onde a anotação não resolve: o comportamento
ao excluir (`OnDelete`) e os dados iniciais (`HasData`). O `Restrict` em motorista → caronas e passageiro → reservas
evita dois caminhos de cascata até a mesma reserva, que o EF não aceita.

## Onde fica o CRUD

Em `Servicos/`, um serviço por entidade, cada um com `Criar`, `Listar`, `BuscarPorId`, `Atualizar` e `Excluir`:

| Arquivo | Destaques |
|---|---|
| `UsuarioService.cs` | cria as preferências junto; e-mail único; não exclui quem tem caronas publicadas |
| `VeiculoService.cs` | placa única e em maiúsculas; não exclui veículo com caronas |
| `CaronaService.cs` | grava as regras na junção; `Include`/`ThenInclude` de motorista, veículo, regras e reservas; não deixa menos vagas que reservas |
| `ReservaService.cs` | ninguém reserva a própria carona; sem reserva acima das vagas; `Cancelar` libera a vaga |
| `Validacao.cs` | `Validator.ValidateObject(obj, ctx, true)` antes de salvar, com as mensagens das anotações em português |

Os métodos usam `Add` + `SaveChanges`, `Include`/`ThenInclude`, `Find`, `Update` e `Remove` do EF.
O menu está em `Menu.cs`, o modo automático em `Demo.cs` e o banco em `Dados/UniCarContext.cs`.

## Testes

`tests/UniCar.Persistencia.Tests` usa SQLite em memória (`DataSource=:memory:`, com a conexão aberta durante o
teste) e aplica as mesmas migrations. Testa: criar e ler uma carona com motorista e regras; atualizar; excluir com
as reservas indo junto; o índice único de e-mail; o `[Required]` barrando usuário sem nome; e as regras de reserva
e de vagas.

## Abrir o banco no DB Browser for SQLite

1. Instale o DB Browser for SQLite (https://sqlitebrowser.org/dl/ ou `winget install DBBrowserForSQLite.DBBrowserForSQLite`).
2. Rode o programa uma vez para o `unicar.db` existir.
3. No DB Browser, clique em **Abrir banco de dados** e escolha `src/UniCar.Persistencia/unicar.db`.
4. A aba **Estrutura do banco de dados** mostra as tabelas, índices e chaves. A aba **Navegar dados** mostra as
   linhas de cada tabela. A tabela `__EFMigrationsHistory` lista as migrations aplicadas.

O `unicar.db` fica fora do Git (`*.db` no `.gitignore`). Se quiser começar do zero, feche o programa e apague o
arquivo: ele é recriado na próxima vez que rodar.
