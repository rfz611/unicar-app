# UniCar — mapa, contribuição negociada e Atividades

Data: 20/10/2026. Situação: fluxo e especificação escrita aprovados pelo usuário nesta conversa; plano de implementação preparado para revisão.

## Resultado esperado

O UniCar permite escolher origem e destino no mapa, visualizar o percurso, obter uma sugestão de contribuição por passageiro, negociar um valor e reservar uma vaga. Caronas podem ser gratuitas. O menu inferior permite alternar entre Inicial e Atividades, onde o usuário acompanha propostas e consulta seu histórico.

Decisões já aprovadas: ordem Carro → Moto → Reservas; origem e destino selecionados para calcular o percurso; sugestão de valor, propostas e contrapropostas; opção gratuita; histórico com pessoa, trajeto, data e valor. A referência funcional é a negociação descrita pelo usuário, preservando a identidade visual atual do UniCar.

## Resumo para revisão

- Inicial conserva as três opções e recebe navegação inferior Inicial / Atividades.
- Motorista seleciona o percurso, horário, veículo, vagas e carona gratuita ou contribuição negociável.
- Passageiro vê o valor de referência e propõe uma contribuição por sua vaga.
- A outra pessoa pode aceitar, recusar ou contrapropor. Uma contraproposta substitui o valor pendente; não constitui aceitação.
- Uma aceitação válida confirma exatamente uma reserva e ocupa exatamente uma vaga, mesmo com toques repetidos ou solicitações simultâneas.
- Atividades mostra propostas pendentes e viagens confirmadas/passadas, com detalhes e valor combinado.
- Sugestão é estimativa de custo compartilhado, não tarifa de outra empresa nem preço de mercado.

## Arquitetura e escolhas

Manter .NET MAUI, ASP.NET Core, Entity Framework e MySQL já usados. Não substituir a autenticação, as tabelas existentes nem o aplicativo por uma versão web.

Abordagens consideradas: mapa nativo em cada plataforma oferece integração específica, mas exige soluções diferentes para Android/iOS/Mac Catalyst; mapa em WebView permite uma seleção coerente nas três plataformas; abrir um aplicativo externo de mapas não permite controlar adequadamente a cotação e a confirmação no UniCar. Escolha proposta: WebView com uma página de mapa controlada pelo UniCar e serviços do provedor encapsulados na API.

Google Maps é o provedor proposto, não uma integração já instalada. Maps JavaScript exibe o mapa; Places permite escolher um endereço e confirmar o ponto; Routes calcula percurso, distância e duração. O aplicativo conhece contratos próprios (RouteQuote, PricingSuggestion), e não estruturas específicas do Google. As chaves da API de rotas ficam no servidor; a chave pública do mapa tem restrições apropriadas ao ambiente.

A página de mapa terá origem configurada. No Mac local, usa a API local; em dispositivo físico, usa o endereço acessível da API; no ambiente publicado, usa HTTPS. A ponte WebView aceita apenas mensagens estruturadas de seleção de pontos da origem permitida. O token da sessão não vai na URL, no histórico do navegador nem em uma página de terceiros.

## Navegação e telas

### Inicial

Carro aparece primeiro e selecionado inicialmente, Moto em segundo e Reservas em terceiro. O menu inferior autenticado mostra Inicial e Atividades, destaca a página corrente e permite alternar sem empilhar cópias das telas. As páginas de acesso e cadastro não exibem o menu. Sair limpa a sessão e o conteúdo autenticado em memória.

Carro/Moto filtram ofertas disponíveis. Cada card mostra motorista, origem, destino, horário, vagas e uma das condições: Gratuita; contribuição de referência por passageiro; ou Valor não informado para dados antigos. Gratuita oferece Reservar vaga; negociável oferece Fazer proposta.

Reservas mostra confirmações já persistidas. Agendar nova viagem continua buscando ofertas existentes por data, destino e veículo, e leva à reserva gratuita ou à negociação da oferta escolhida. Escolher uma data não cria uma reserva nem uma oferta sem motorista.

### Publicar carona e selecionar trajeto

O motorista escolhe origem e destino por pesquisa de endereço ou por pontos no mapa, confirma ambos e visualiza a linha do percurso, distância e duração estimadas. Localização atual é opcional, solicitada apenas se ele tocar em Usar minha localização. Os endereços permanecem editáveis/confirmáveis como descrição do encontro.

O cálculo usa perfil de carro ou de duas rodas conforme a oferta. Se o provedor não suportar o modo/região, a tela explica a indisponibilidade; não apresenta rota de carro como se fosse de moto. Uma mudança de ponto ou veículo invalida a cotação anterior.

Mantêm-se data/hora e vagas (moto: uma; carro: uma a quatro). A escolha Gratuita fixa a contribuição em zero e dispensa negociação. Em Negociável, mostrar sugestão, memória simples do cálculo e campo de referência ajustável pelo motorista. Não somar pedágios ou outros custos que não foram calculados/informados.

### Proposta e contraproposta

Passageiro informa valor em reais por uma vaga. Mostrar trajeto, horário, referência do motorista e último valor proposto. Apenas os dois participantes veem a negociação. A pessoa que enviou a última oferta aguarda resposta e pode retirar a proposta; não pode aceitar sua própria oferta.

A outra pessoa pode aceitar, recusar ou enviar contraproposta. A confirmação explicita o valor e a pessoa antes de ocupar a vaga. Sem vagas ou após a partida, uma proposta pendente não pode ser aceita. A tela mantém a proposta como indisponível/expirada e permite retornar à busca.

Uma carona gratuita não aceita lances pagos. Uma oferta negociável aceita contribuição zero como proposta, sujeita à aceitação da outra pessoa.

### Atividades

Duas seções: Propostas e Viagens. Atualização ao abrir a página e botão Atualizar; não depender de notificações para concluir o fluxo.

Propostas inclui negociações enviadas pelo passageiro e recebidas pelo motorista, com nome da outra pessoa, trajeto, data, veículo, último valor, situação e indicação de quem deve responder. Responder abre o detalhe da conversa de valores.

Viagens inclui caronas em que o usuário tem reserva e caronas oferecidas por ele. Para passageiro, mostrar o motorista e o valor individual aceito. Para motorista, mostrar passageiros confirmados e o valor combinado com cada um, sem confundir contribuição individual com total. Oferta sem passageiros mostra Nenhuma reserva confirmada.

Detalhe: nomes, origem/destino combinados, data/hora, veículo, condição gratuita ou contribuição, momento da reserva e situação. Próxima/Passada depende do horário; não apresentar Concluída apenas porque o tempo passou. Não afirmar que houve pagamento ou que o percurso efetivo foi rastreado: o sistema registra o trajeto combinado, sem GPS de acompanhamento nem processamento de pagamento nesta entrega.

## Sugestão de contribuição

Usar quilometragem viária retornada pelo cálculo de rotas. A API mantém parâmetros explícitos e separados para carro e moto, em BRL por km de custo estimado, com uma versão da configuração. Esses parâmetros precisam ser configurados para o ambiente; não usar valores de mercado inventados.

Fórmula inicial proposta: custo estimado do trajeto = km × custo configurado por km do veículo; contribuição sugerida por passageiro = custo estimado ÷ (vagas oferecidas + motorista), arredondada para centavos. O número de vagas oferecidas é a capacidade publicada original, não as vagas que sobram no momento. Assim a sugestão não aumenta a cada reserva.

A fórmula é uma decisão de produto proposta para esta especificação, não algo que o usuário já havia definido. Exibir os fatores usados na estimativa. Motorista pode ajustar a referência; passageiro pode negociar. Alteração posterior de configuração não muda acordos já aceitos.

Se faltarem parâmetros válidos, não gerar uma sugestão numérica. Mostrar Estimativa indisponível e permitir valor informado pelo motorista ou Gratuita; a sugestão só é considerada entregue e validada quando configurada e testada com rota real.

Valores usam decimal com duas casas, moeda BRL, validação de intervalo compatível com o banco, cultura pt-BR na interface e números JSON sem separador de milhar. Valor zero é diferente de valor desconhecido.

## Dados e compatibilidade

Evolução aditiva, mantendo usuários, ofertas e reservas atuais:

- Ride: condição de preço (LegacyUnknown, Free, Negotiable), referência do motorista, capacidade publicada original, pontos escolhidos pelo usuário/place IDs e versão da configuração da estimativa.
- RouteQuote: cotação curta, vinculada ao usuário e aos pontos/modo selecionados; validade de dez minutos. Distância, duração, geometria e demais conteúdos do provedor não viram um arquivo permanente de respostas externas.
- FareNegotiation: oferta, passageiro, último valor, autor da última oferta, versão concorrente, situação e datas. Uma conversa por passageiro/oferta, com índice para impedir duplicação; negociações terminais podem iniciar uma nova rodada na mesma conversa apenas por ação explícita e se não houver reserva.
- Situações de negociação: Pending (aguarda a outra pessoa), Accepted (reserva criada), Declined (recusada), Withdrawn (retirada pelo autor pendente), Expired (horário de partida alcançado) e Unavailable (oferta sem vagas). Só Pending recebe novas propostas ou aceitação; uma nova rodada após recusa ou retirada exige ação explícita, versão nova e ausência de reserva; Expired e Unavailable não reabrem. Versão é um inteiro incrementado em cada mudança.
- FareProposal: trilha das propostas/contrapropostas, autor, valor e instante; não editar mensagens antigas.
- RideReservation: valor final aceito nullable, moeda, negociação de origem opcional e condição de preço aceita. Índice único passageiro/oferta continua obrigatório.

Reservas antigas mantêm valor desconhecido, exibido como Valor não informado; não converter automaticamente em gratuitas. Ofertas antigas conservam seus endereços e tipo de veículo. Não inventar coordenadas para elas. Para receber novas propostas, o motorista deve atualizar os pontos/condição de preço; a atualização de referência não altera reservas já confirmadas.

Dados independentes da contratação (participantes, valores acordados, descrições de encontro inseridas/confirmadas pelo usuário) persistem para histórico. Place IDs podem persistir; reconsultar mapa quando necessário. A implementação deve obedecer às regras de armazenamento e atribuição do provedor, sem armazenar indefinidamente geometria ou a resposta inteira de rotas.

## API e responsabilidades

- Serviço RouteService: resolve o contrato do provedor e devolve cotação de percurso autenticada. Serviço PricingService: calcula contribuição a partir de cotação e configuração validadas.
- Criar/atualizar oferta: servidor valida propriedade, perfil, capacidade, data futura, cotação e preço; não confiar na quilometragem nem na sugestão enviadas pelo aplicativo.
- Endpoints de negociação: criar/enviar proposta, listar por usuário, consultar detalhe, contrapropor, recusar, retirar e aceitar. Ações recebem a versão vista pelo cliente para rejeitar respostas a ofertas ultrapassadas.
- Reservar: caminho gratuito permanece direto; caminho negociável exige aceitação válida. O endpoint antigo de join também aplica essa regra, impedindo contornar a negociação.
- Atividades: endpoint autenticado reúne propostas e viagens apenas do usuário, com paginação de vinte registros, ordenação por atualidade e filtro por seção.

Distâncias e valores sugeridos não exigem uma reserva. Uma proposta também não reduz vagas. Aceitar bloqueia a linha da carona e a negociação, sempre nessa ordem; verifica autor/permissão, versão, situação, horário e vagas; cria a reserva e reduz uma vaga na mesma transação. Atualiza negociações concorrentes sem vaga para indisponíveis. Reenvio da mesma aceitação retorna a confirmação existente sem nova cobrança de vaga. Pedido com valor/versão antigos retorna conflito e exige revisão da última oferta.

Negociar com a própria carona, ler negociações alheias, aceitar a própria proposta e alterar valores confirmados são recusados pelo servidor. Tokens não aparecem em logs. Parâmetros do cliente são validados antes de chamar serviços externos.

## Erros e estados

Prever mapa carregando/indisponível, chave ausente, endereço sem resultado, pontos inválidos, ausência de rota, timeout, cotação expirada, estimativa sem configuração, sessão expirada, proposta alterada, carona sem vagas/partida e falha de conexão. Cada falha mantém os dados preenchidos e oferece tentativa adequada. Sem provedor configurado, não simular mapa, quilômetros, duração ou sugestão como se fossem reais.

O armazenamento da reserva e sua leitura em Reservas e Atividades é a confirmação do sucesso. Falha de rede após aceitar exige consultar a situação existente antes de reenviar uma ação potencialmente duplicada.

## Dependências e limites desta entrega

Google Maps JavaScript, Places e Routes exigem configuração externa apropriada. Ativar os mapas reais depende de credenciais, APIs habilitadas, restrições de chave, origem de acesso e faturamento no projeto Google Cloud. Esta especificação não ativa serviços nem gera custos externos. Os valores de custo por km também precisam ser fornecidos/configurados antes de validar a sugestão real. Não colocar segredos no repositório, ZIP ou aplicativo.

A implementação pode desenvolver e testar os contratos e as telas com respostas controladas explicitamente identificadas, mas não pode declarar a integração real concluída sem cálculo de rota real. Publicação em lojas, processamento de pagamentos, rastreamento ao vivo e notificações push ficam para uma entrega própria.

## Verificação e critérios de aceite

- Compilar API e app Mac Catalyst; repetir verificações atuais de serviço/reservas.
- Testar a fórmula com distâncias e parâmetros controlados, casas decimais, capacidade original, gratuito e configuração ausente.
- Testar negociação completa (passageiro → motorista → passageiro), recusa/retirada, valor zero e autorização de cada participante.
- Confirmar que join direto não contorna acordo; testar aceitação concorrente, último assento, versão antiga, reenvio e reserva prévia.
- Confirmar privacidade e paginação de Atividades; nova sessão mantém histórico e preço final.
- Migração preserva dados antigos e não converte valores desconhecidos em zero.
- Navegação Inicial/Atividades permite ida/volta, destaque correto, saída e estados vazios/erro, inclusive em largura de celular.
- Com credenciais configuradas, selecionar dois pontos reais, obter rota/quilômetros/duração, mostrar atribuição e reproduzir a cotação na sugestão. Repetir para carro e moto e verificar o erro quando não houver rota suportada.
- Visualizar no aplicativo real a publicação, negociação e histórico. A prévia no navegador continua identificada como demonstração.

## Fontes de referência

Consultadas em 20/10/2026:

- [Configuração da Maps JavaScript API](https://developers.google.com/maps/documentation/javascript/get-api-key).
- [Cálculo de rotas](https://developers.google.com/maps/documentation/routes/compute-route-over).
- [Cobertura de duas rodas](https://developers.google.com/maps/documentation/javascript/routes/coverage-two-wheeled).
- [Configuração/faturamento de Routes](https://developers.google.com/maps/documentation/routes/usage-and-billing).
- [Políticas de conteúdo e atribuição de Routes](https://developers.google.com/maps/documentation/routes/policies).
- [Políticas da Maps JavaScript API](https://developers.google.com/maps/documentation/javascript/policies).

## Revisão da especificação

Revisão interna: fluxo e telas correspondem ao pedido; proposta não reserva vaga; gratuito e desconhecido são distintos; reserva mantém o valor final; autorizações e concorrência estão definidas; dependências externas e critérios para declarar integração real concluída estão explícitos. Nenhuma implementação de produto foi iniciada nesta etapa. O projeto recebido não contém diretório .git; documento salvo sem criar um repositório ou realizar commit.
