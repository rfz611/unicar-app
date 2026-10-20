# UniCar Maps, Negotiation and Activities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar Inicial/Atividades, seleção de percurso real com sugestão de contribuição e negociação que confirma uma reserva persistente.

**Architecture:** Evoluir o MAUI e a API existentes, com serviços próprios de rotas, preço e negociação e migrações aditivas. Atividades lê contratos autenticados; mapa usa WebView controlada e Routes via servidor. As etapas compartilham os contratos definidos abaixo e formam uma única entrega integrada.

**Tech Stack:** .NET 10, .NET MAUI, ASP.NET Core, EF Core/Pomelo, MySQL, Google Maps JavaScript/Places/Routes e verificações C#/Python.

**Spec:** `docs/superpowers/specs/2026-10-20-mapas-negociacao-atividades-design.md` (aprovada pelo usuário nesta conversa).

## Global Constraints

- Ordem Carro → Moto → Reservas; menu inferior autenticado Inicial / Atividades.
- Moto: uma vaga; carro: uma a quatro; valores em BRL, decimal com duas casas; zero difere de desconhecido.
- Cotação de percurso vinculada ao usuário/pontos/modo e válida por dez minutos.
- Sugestão divide custo estimado por vagas publicadas originais mais motorista; não utiliza vagas restantes.
- Proposta não reduz vagas; aceitação confirma exatamente uma reserva, transacionada e protegida por índice único.
- Atividades pagina vinte registros e expõe apenas dados do usuário autenticado; Próxima/Passada não significa pagamento ou conclusão confirmada.
- Dados antigos preservados; mapa/quilômetros não são inventados; serviços reais só são considerados validados após teste com credenciais configuradas.
- Chaves de rotas no servidor; tokens fora de URLs/logs; não persistir respostas completas/geométricas do provedor como histórico permanente.
- Projeto em `/Users/nicollau/Downloads/unicar-app`, sem Git. Não inicializar Git nem criar worktree automaticamente. Em cada checkpoint, registrar arquivos/testes em `docs/superpowers/implementation-log.md`; commits só se o usuário disponibilizar um repositório.

## Review Focus

- Passageiro digitando `12,50`, vazio, negativo ou mais de duas casas deve receber validação clara; o cliente envia decimal JSON sem ambiguidade (tarefas 2 e 7).
- Resposta atrasada do mapa após troca dos pontos/veículo não pode substituir a cotação mais recente (tarefas 4 e 5).
- Duas respostas simultâneas, reenvio após timeout e versão antiga não podem alterar o preço aceito nem consumir outra vaga (tarefas 6 e 7).
- Usuário sai e outra conta entra: histórico, propostas e mapa em memória não podem revelar os dados da conta anterior (tarefas 3 e 7).
- Migração repetida ou dados anteriores sem valores/coordenadas devem manter reservas legíveis e desconhecido distinto de gratuito (tarefas 1 e 8).

## Estrutura e contratos de fronteira

Caminhos relativos à raiz do projeto. Arquivos novos e responsabilidades:

- API `Models/PricingMode.cs`, `Models/FareNegotiation.cs`, `Models/FareProposal.cs`: estados e persistência; adaptar `Models/Ride.cs`, `Models/RideReservation.cs` e `Data/AppDbContext.cs`.
- API `DTOs/TravelContracts.cs`: contratos abaixo; Mobile `Models/TravelModels.cs`: equivalentes de transporte e propriedades de apresentação, sem regras de autorização.
- API `Services/PricingService.cs`, `Services/Maps/IRouteProvider.cs`, `GoogleRouteProvider.cs`, `RouteQuoteService.cs`, `Services/NegotiationService.cs`, `Services/ActivityService.cs`: uma responsabilidade por unidade.
- API `Controllers/RoutesController.cs`, `NegotiationsController.cs`, `ActivitiesController.cs`; ajustar `RidesController.cs` para preço, percurso e join protegido.
- API `wwwroot/map/index.html`, `wwwroot/map/map.js`: página de mapa sem token; bootstrap fornece somente configuração pública de mapa.
- Mobile `Pages/ActivitiesPage.xaml(.cs)`, `NegotiationPage.xaml(.cs)`, `RoutePickerPage.xaml(.cs)` e `Services/ActivityService.cs`, `NegotiationService.cs`, `RouteQuoteService.cs`; adaptar Shell, Home e OfferRidePage.
- `database/004_pricing_negotiation.sql`: migração aditiva do banco atualmente criado com os modelos da API e script 003. Não executar migrations antigas automaticamente sobre esse banco.
- `tests/UniCar.API.Checks/`: console C# no padrão já usado pelo projeto, referência à API e testes puros de preço/contratos/provedor. `tests/api_negotiation_check.py`: testes HTTP/MySQL em banco descartável. `tests/README.md`: comandos e requisitos.

Contratos compartilhados definidos antes das telas:

- `PricingMode`: `LegacyUnknown`, `Free`, `Negotiable`. `NegotiationStatus`: `Pending`, `Accepted`, `Declined`, `Withdrawn`, `Expired`, `Unavailable`.
- `MapPoint(double Latitude, double Longitude, string Label, string? PlaceId)`; limites geográficos válidos, label de encontro confirmado pelo usuário com até 150 caracteres.
- `RouteRequest(MapPoint Origin, MapPoint Destination, string VehicleType)`; tipos `CAR`/`MOTO`.
- `RouteQuoteDto(Guid Id, MapPoint Origin, MapPoint Destination, string VehicleType, int DistanceMeters, int DurationSeconds, string EncodedPolyline, DateTimeOffset ExpiresAt)`; sem userId fornecido pelo cliente.
- `PricingSuggestionDto(decimal? Amount, string Currency, string? UnavailableReason, decimal? CostPerKm, int OriginalSeats, string ConfigurationVersion)`; `Currency="BRL"`.
- `CreateOrUpdateRideRequest`: manter campos atuais e acrescentar `Guid RouteQuoteId`, `PricingMode PricingMode`, `decimal? ReferenceAmount`; servidor extrai pontos, modo e capacidade da cotação/contexto validado.
- `SubmitProposalRequest(int ExpectedVersion, decimal Amount)`. Criar conversa usa `POST /api/rides/{rideId}/negotiations` com `ExpectedVersion=0`; uma rodada após Declined/Withdrawn usa a versão terminal atual. Rejeitar versão diferente.
- `NegotiationDto`: `Id`, `RideId`, `PassengerId`, `DriverId`, `CounterpartyName`, `Status`, `Version`, `LatestAmount`, `LatestAuthorId`, `CanRespond`, `CanWithdraw`, `RideSummary`, `IReadOnlyList<ProposalDto> Proposals`, `int? ReservationId`.
- `ProposalDto(int Id, int AuthorId, string AuthorName, decimal Amount, DateTimeOffset CreatedAt)`.
- `RideSummaryDto`: `Id`, `Origin`, `Destination`, `DepartureTime`, `VehicleType`, `DriverName`, `PricingMode`, `decimal? ReferenceAmount`.
- `ReservationReceiptDto(int ReservationId, int RideId, decimal? AgreedAmount, string Currency, PricingMode PricingMode)`.
- `PageResult<T>(IReadOnlyList<T> Items, int Page, bool HasMore)`. `ActivityTripDto`: `RideSummary`, `Role` (Driver/Passenger), `DateTimeOffset? ReservedAt`, `decimal? AgreedAmount`, `IReadOnlyList<PassengerAgreementDto> Passengers`; motorista recebe todos os confirmados de sua oferta, passageiro não recebe os demais passageiros. `PassengerAgreementDto`: nome e valor por pessoa.
- Horário de partida mantém a convenção local existente (Brasília) nesta entrega; cotações/eventos de propostas usam UTC/DateTimeOffset. Valores nunca representam comprovante de pagamento.

## Task 1: Persistência compatível e migração repetível

**Files:** criar enums/modelos API descritos acima e `database/004_pricing_negotiation.sql`; modificar Ride, RideReservation e AppDbContext; criar `tests/api_negotiation_check.py` com preparação de banco isolado.

**Interfaces:** produz as entidades equivalentes aos contratos; `FareNegotiation` tem índice único `(RideId, PassengerId)`, `Version` inteiro e situação; propostas são anexadas, não sobrescritas. Reserva tem `AgreedAmount decimal(10,2)?`, `Currency=BRL`, condição aceita e negociação opcional. Ride tem capacidade original, condição, referência e pontos/place IDs opcionais.

- [ ] Escrever o caso `migration_preserves_legacy_and_is_repeatable`: banco temporário com uma oferta e reserva antigas; aplicar 004 duas vezes; afirmar mesmos IDs/vagas e `AgreedAmount IS NULL`, `PricingMode=LegacyUnknown`, sem coordenadas inventadas. O teste começa falhando por ausência do novo esquema.
- [ ] Criar migração condicional por information_schema, sem recriar tabelas ou apagar registros; capacidade antiga recebe vagas restantes + reservas persistidas conhecidas. Não inferir ocupações que o backend antigo nunca registrou.
- [ ] Implementar entidades, índices e relacionamentos (evitar dependência circular de deleção entre reserva e negociação; negociação de origem pode usar DeleteBehavior.Restrict).
- [ ] Executar `python3 tests/api_negotiation_check.py migration`; esperado PASS. O harness cria banco `unicar_check_<id>`, configura API de teste em 5056 e limpa somente esse banco. Credenciais de teste vêm de `UNICAR_TEST_MYSQL` ou padrão local explicitamente documentado; não usar o banco de usuários como fixture.
- [ ] Executar `dotnet build src/UniCar.API`; esperado zero erros. Registrar checkpoint.

## Task 2: Contribuição decimal configurável

**Files:** criar `Services/PricingService.cs`, `DTOs/TravelContracts.cs`, `tests/UniCar.API.Checks/{UniCar.API.Checks.csproj,Program.cs,PricingChecks.cs}`; modificar configuração/Program sem valores de mercado presumidos.

**Interfaces:** `PricingService.Suggest(int distanceMeters, string vehicleType, int originalSeats, PricingMode mode) -> PricingSuggestionDto`; `PricingOptions` tem `decimal? CarCostPerKm`, `decimal? MotoCostPerKm`, `string ConfigurationVersion`. Valores positivos e versão não vazia habilitam estimativa. Limite monetário `0..99999999.99`; rejeitar precisão acima de centavos em entradas negociadas, sem arredondamento silencioso.

- [ ] Testes puros com valores de fixture: `Suggest(12000,"CAR",3,Negotiable)` com custo 1,00/km resulta 3,00; redução de vagas restantes não altera o resultado; Free resulta zero; custo ausente resulta Amount null; assentos inválidos e distância não positiva são rejeitados. Para arredondamento, 1000 m × 1,01 / 2 resulta 0,51 com AwayFromZero.
- [ ] Executar `dotnet run --project tests/UniCar.API.Checks -- pricing`; confirmar falha antes da implementação.
- [ ] Implementar fórmula decimal `round((distanceMeters / 1000m) * costPerKm / (originalSeats + 1), 2, AwayFromZero)` e configuração versionada. Testar também excesso do limite monetário.
- [ ] Reexecutar o comando e checks existentes; esperado PASS. Registrar checkpoint.

## Task 3: Menu inferior e histórico autenticado

**Files:** criar API ActivityService/ActivitiesController; Mobile TravelModels/ActivityService/ActivitiesPage; modificar AppShell.xaml(.cs), MauiProgram.cs e SessionService se necessário para limpar caches.

**Interfaces:** API `GET /api/activities/trips?page=1` → `PageResult<ActivityTripDto>`; `GET /api/activities/proposals?page=1` → `PageResult<NegotiationDto>` (lista vazia antes de existirem propostas). Páginas de vinte, ordenar por última atualização e Id decrescentes. `ActivityService.GetTripsAsync(int userId,int page,CancellationToken)` e `GetProposalsAsync` retornam esses contratos; cliente usa token da sessão, nunca escolhe userId na URL.

- [ ] Criar casos `activities_are_private`, `driver_sees_own_passengers_only`, `legacy_amount_is_not_zero`, `activity_pagination_has_no_duplicates`: 21 registros exigem páginas 20/1 e ordenação estável; fixtures com mesmo instante são ordenadas por Id. Executar `python3 tests/api_negotiation_check.py activities` e observar falha.
- [ ] Implementar consultas apenas sobre reservas/ofertas/negociações do autenticado, incluindo motorista sem passageiros. Não carregar tabelas inteiras no cliente.
- [ ] Trocar destinos autenticados do Shell por um TabBar com Inicial/Atividades, mantendo rota absoluta `//home` e `//welcome` funcionando; ocultar abas fora do fluxo autenticado. Ajustar o seletor inicial do Shell, pois HomeItem deixa de ser item raiz; nunca empilhar Shells. Manter Carro/Moto/Reservas na ordem aprovada.
- [ ] Implementar ActivitiesPage com seções Propostas/Viagens, Atualizar, carregar mais, estado vazio/erro e detalhe de viagem. Em legado mostrar Valor não informado; horários passados mostram Passada. Recarregar pelo usuário atual e invalidar solicitações/caches ao sair.
- [ ] Verificar contratos no console Mobile.ServiceChecks; executar casos HTTP e build MAUI. No app real: alternar Inicial/Atividades repetidamente, sair e entrar com outra conta; nenhum dado anterior permanece. Registrar checkpoint.

## Task 4: Rotas e cotações validadas no servidor

**Files:** criar `Services/Maps/{IRouteProvider,GoogleRouteProvider,RouteQuoteService}.cs`, `Controllers/RoutesController.cs`, `tests/UniCar.API.Checks/RouteChecks.cs`; modificar Program/configuração.

**Interfaces:** `IRouteProvider.ComputeAsync(RouteRequest,CancellationToken) -> ProviderRoute(int DistanceMeters,int DurationSeconds,string EncodedPolyline)`; `RouteQuoteService.CreateAsync(int userId,RouteRequest,CancellationToken) -> RouteQuoteDto`; `GetValid(Guid quoteId,int userId,RouteRequest expected) -> RouteQuoteDto`. Cotação in-memory limitada, dez minutos via TimeProvider; reinício exige recálculo. `POST /api/routes/quotes` exige autenticação; `POST /api/routes/quotes/{id}/suggestion` recebe capacidade original/condição e retorna PricingSuggestionDto após validar proprietário/validade.

- [ ] Testar HTTP de provedor com respostas controladas: DRIVE/TWO_WHEELER corretos; field mask só distância/duração/polyline; ausência de rota; 401/429/timeout/JSON inválido; chave ausente não chama o provedor. Uma cotação de outra pessoa, expirada, modo/pontos diferentes é recusada. Executar `dotnet run --project tests/UniCar.API.Checks -- routes`; observar falha.
- [ ] Implementar adapter Routes via HttpClient; usar coordenadas válidas de origem/destino, distinguir modo não suportado de indisponibilidade. Segredo por `Maps:RoutesApiKey`, fora do repositório e logs; nada de fallback silencioso para linha reta ou perfil de carro.
- [ ] Implementar serviços/endpoints e limitar cotação por usuário, sem aceitar distância/preço calculados pelo cliente. Cancelamento interrompe chamada ao provedor. Não manter geometria no histórico de ofertas.
- [ ] Executar checks e autenticação HTTP com provedor controlado; esperado PASS. Registrar dependência externa: configuração real de Maps/Places/Routes, faturamento, chaves restritas e custo/km ainda necessários para os testes reais. Registrar checkpoint.

## Task 5: Seleção de origem/destino e publicação com preço

**Files:** criar API `wwwroot/map/{index.html,map.js}` e bootstrap público sem segredos de servidor; Mobile RoutePickerPage/RouteQuoteService; modificar OfferRidePage, Models/RideModels.cs, Services/RideService.cs e API RidesController.

**Interfaces:** `RoutePickerPage(string vehicleType)` devolve `Task<RouteQuoteDto?> PickAsync()` ao proprietário da navegação modal. WebView recebe página da origem configurada e emite payload de seleção `RouteRequest` com nonce da abertura; .NET obtém cotação autenticada e chama JS `renderRoute(encodedPolyline, requestId)` somente se requestId ainda for atual. A página não recebe token. `POST /api/rides` e `PATCH /api/rides/{id}/pricing-route` consomem CreateOrUpdateRideRequest; atualizar só o proprietário, bloqueando a linha da oferta na mesma ordem usada para reservar. Com reserva confirmada, bloquear alteração de pontos, veículo, horário, capacidade e condição Free/Negotiable; permitir apenas referência ajustável dentro do modo atual, sem tocar no valor já aceito. Propostas Pending também bloqueiam mudança de percurso/modo até serem recusadas ou retiradas.

- [ ] Escrever testes de mensagem/navegação para origem diferente, nonce errado, seleção incompleta e payload inválido; teste de duas respostas atrasadas garante que só a última seleção desenha rota e habilita confirmação. Escrever teste HTTP: cotação adulterada/expirada e LegacyUnknown sem pontos não permitem criar oferta negociável.
- [ ] Implementar página de mapa com pesquisa Places e confirmação de pontos, atribuições do provedor, rota/quilômetros/duração e botão de confirmar. Localização só após ação explícita; implementar permissões por plataforma e negação com seleção manual funcionando. Bloquear navegação para documentos externos; validar origem do último documento carregado e nonce antes de processar ponte. Se o provedor exigir configuração adicional de origem/referrer no WebView, resolver antes de declarar mapa real funcional.
- [ ] Publicação mostra condição Gratuita/Negociável, valor sugerido e fatores; campo de referência editável; rota é invalidada ao mudar pontos/veículo. Manter valores quando ocorrer erro e mostrar Estimativa indisponível se faltar custo/km.
- [ ] Validar no servidor capacidade, preço, usuário e cotação, incluindo normalização do texto de encontro. As ofertas/reservas antigas continuam legíveis; novo contrato retorna preço e opção de ação para o card.
- [ ] Executar tests de serviço/contrato e build MAUI; no app testar seleção, voltar sem confirmar, erro e troca de veículo. Com credenciais ainda ausentes, reportar mapa real como pendente em vez de usar mock silencioso. Registrar checkpoint.

## Task 6: Propostas e aceitação atômica

**Files:** criar NegotiationService/NegotiationsController; modificar RidesController para delegar confirmação ao mesmo serviço transacional; expandir `tests/api_negotiation_check.py`.

**Interfaces:** `SubmitAsync(int actorId,int rideId,SubmitProposalRequest,CancellationToken)`, `CounterAsync(int actorId,int negotiationId,SubmitProposalRequest,CancellationToken)` → NegotiationDto. `GetAsync`, `DeclineAsync`, `WithdrawAsync` → NegotiationDto; últimas duas exigem versão. `AcceptAsync(int actorId,int negotiationId,int expectedVersion,CancellationToken)` e `ReserveFreeAsync(int actorId,int rideId,CancellationToken)` → ReservationReceiptDto. Endpoints sob `/api/negotiations/{id}`: GET, POST counter/accept/decline/withdraw; criar sob rota da oferta definida nos contratos. Falhas de permissão 403 (não revelar existência em consulta alheia: 404); conflito de versão/estado 409; validação 400; sem token 401.

- [ ] Escrever casos de integração `passenger_counter_driver_accept`, `cannot_accept_own_offer`, `decline_withdraw_and_reopen_explicitly`, `zero_proposal_requires_acceptance`, `free_ride_rejects_paid_bid`, `paid_join_cannot_bypass_negotiation`, `stale_version_changes_nothing` e `accepted_price_is_immutable`. Executar `python3 tests/api_negotiation_check.py negotiations`; observar falhas.
- [ ] Implementar regras de autoria e trilha append-only. Nova rodada na mesma conversa após Declined/Withdrawn precisa da versão terminal correta e nova ação explícita; Expired/Unavailable não reabrem. Expirar propostas na leitura/mutação quando partida já ocorreu.
- [ ] Implementar transação MySQL: bloquear carona antes da negociação; verificar versão/autoria/prazo/vagas e reserva prévia; salvar acordo, reserva e decremento juntos; última vaga marca outras negociações Pending da oferta como Unavailable. Reenvio da aceitação já confirmada retorna mesmo recibo apenas para participante autorizado, sem mutação.
- [ ] Join gratuito também usa transação e snapshot zero. LegacyUnknown exige que motorista atualize preço/percurso antes de novas reservas, com mensagem de Valor não informado; reservas antigas permanecem acessíveis. Negociável nunca usa join direto como confirmação.
- [ ] Acrescentar `two_acceptances_one_seat`, `duplicate_accept_after_timeout`, `free_join_races_mode_update` (troca de modo e join disputam o mesmo bloqueio: se o join vencer, a mudança é recusada; se o modo pago vencer, o join é recusado), `rollback_does_not_decrement`. Executar banco real e afirmar uma reserva/um decremento e preço final correto. Registrar checkpoint.

## Task 7: Negociação no aplicativo e detalhes de Atividades

**Files:** criar Mobile NegotiationService/NegotiationPage/TravelModels; modificar HomePage, ActivitiesPage, RideService e MauiProgram; expandir Mobile.ServiceChecks.

**Interfaces:** cliente `GetAsync(id)`, `SubmitAsync(rideId,SubmitProposalRequest)`, `CounterAsync(id,SubmitProposalRequest)`, `AcceptAsync(id,expectedVersion)`, `DeclineAsync`, `WithdrawAsync`; todos retornam resultados explícitos e usam o token atual. `NegotiationPage(int negotiationId)` apresenta o contrato do servidor e suas ações permitidas. `MoneyInput.TryParse(string text,out decimal amount)` aceita BRL com ponto ou vírgula decimal sem separador de milhar e no máximo duas casas; vazio/negativo/ambíguo/excesso é inválido.

- [ ] Checks do cliente para `12,50` e `12.50` → 12.50m, `0` aceito, vazio/`1.234,56`/`-1`/`12,345` rejeitados. Verificar rota, método, versão e token de cada ação. Rodar antes da implementação e observar falha.
- [ ] Implementar cards com Reservar vaga em Free e Fazer proposta em Negotiable; LegacyUnknown explica indisponibilidade de nova reserva. Detalhe mostra referência, autores e sequência de propostas, estado e ações corretas; não usar mensagens de chat como substituto de persistência.
- [ ] Implementar confirmação do valor antes de aceitar; bloquear duplo toque; em timeout consultar GET da negociação antes de reenviar aceitação; conflito recarrega e exige confirmação do valor atual. Não aceitar automaticamente contraproposta nova.
- [ ] Ligar Propostas em Atividades ao detalhe e manter Reservas/Viagens consistentes após sucesso. Limpar conteúdo autenticado e cancelar carregamentos ao sair; resposta de uma sessão anterior não atualiza tela da próxima sessão.
- [ ] Rodar todos os checks e testar no app com duas contas de fixture: proposta → contraproposta → aceitação → consulta após novo login. Verificar valor acordado, gratuito e legado no histórico; sem marcar como pago/concluído. Registrar checkpoint.

## Task 8: Verificação integrada e entrega

**Files:** `tests/README.md`, `docs/superpowers/implementation-log.md`, notas em outputs e prévia existente apenas se necessário para revisar o novo fluxo.

- [ ] Rodar `dotnet build src/UniCar.API`, `dotnet run --project tests/UniCar.API.Checks`, `dotnet run --project tests/UniCar.Mobile.ServiceChecks` e `python3 tests/api_negotiation_check.py all`; esperado todos os checks passarem, com dados de teste isolados e removidos.
- [ ] Atualizar `tests/api_reservations_check.py` para publicar fixtures Free com contrato/cotação de teste explícito no harness; não adicionar endpoint de bypass ao aplicativo de produção. Rodar o teste para preservar privacidade, capacidade e deduplicação já verificadas.
- [ ] Rodar `dotnet build src/UniCar.Mobile -f net10.0-maccatalyst -p:TargetFrameworks=net10.0-maccatalyst`; esperado zero erros, reportar separadamente o aviso preexistente `CS0108 FormField.Frame` se persistir. Verificação Android/iOS só será declarada se SDKs e compilação dessas plataformas forem executados.
- [ ] Validar mapa real com configurações externas disponíveis: dois pontos reais, carro/moto, distância/duração/polyline, fórmula com parâmetros explícitos e atribuição. Falta de chave, faturamento, origem autorizada ou custo/km deixa esta parte pendente e listada pelo nome; não alegar conclusão total.
- [ ] Executar a jornada no app real: publicar → propor → contrapropor → aceitar → reabrir → consultar Atividades. Confirmar navegação inferior e largura de celular; atualizar prévia com dados identificados como fictícios, sem confundir com o teste nativo.
- [ ] Entregar ZIP atualizado sem bin/obj/segredos, resumo do que mudou e resultados/limites de verificação. Registrar checkpoint e revisão final de código antes de declarar a entrega pronta.

## Revisão do plano e execução

Revisão própria realizada: tarefas cobrem mapa, estimativa, negociação, compatibilidade, histórico e navegação; os cinco riscos do Review Focus têm verificações nas tarefas responsáveis. Ordem recomendada 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8; contratos definidos antes de seus consumidores. A revisão incluiu atualização de oferta concorrente com reserva, preservação de percurso após confirmação e diferença entre preço de referência e preço aceito. Nenhuma dependência externa foi provisionada por este plano.

Método recomendado: execução direta nesta conversa (`superpowers:executing-plans`), pois as etapas dependem dos mesmos contratos, migrações e regras de reserva. O plano ainda precisa de revisão do usuário e escolha do método de execução. Alternativa: execução por subagentes, com contexto/revisões separados por tarefa, conforme `superpowers:subagent-driven-development`.
