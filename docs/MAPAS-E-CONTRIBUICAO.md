# Mapas, contribuição e negociação

O menu inferior autenticado tem Inicial e Atividades. A inicial mantém Carro, Moto e Reservas. Atividades reúne propostas recebidas/enviadas e viagens oferecidas/reservadas, com valor combinado por passageiro. Datas passadas indicam Passada, sem afirmar que houve pagamento ou rastreamento real.

## Banco existente

Aplicar os scripts 003 e 004 em ordem em bancos criados com os modelos da API. O script 004 preserva valores anteriores como desconhecidos e é reaplicável. Nesta máquina, 004 já foi aplicado após backup. Não executar migrations antigas do EF automaticamente sobre esse banco.

```sh
mysql -u root < database/004_pricing_negotiation.sql
```

## Ativar Google Maps

A integração foi implementada com Maps JavaScript e Places na seleção, e Routes no backend. Nesta máquina, as chaves estão configuradas fora do projeto, nos user secrets locais da API. Não há rotas, distâncias ou valores de mercado inventados como fallback.

As seguintes configurações do ASP.NET são lidas do ambiente (a sintaxe abaixo usa valores descritivos, que precisam ser substituídos; não são credenciais):

- `Maps__BrowserApiKey`: chave pública restrita aos serviços de mapa/Places e às origens utilizadas.
- `Maps__RoutesApiKey`: chave de servidor restrita à Routes API; nunca vai para o app ou página de mapa.
- `Maps__MapId`: identificador do mapa, se utilizado; o protótipo usa DEMO_MAP_ID.
- `Pricing__CarCostPerKm`: custo estimado de carro em BRL/km, informado pelo responsável pelo app.
- `Pricing__MotoCostPerKm`: custo estimado de moto em BRL/km.
- `Pricing__ConfigurationVersion`: versão identificável dos parâmetros de custo.

O projeto Google Cloud precisa ter as APIs correspondentes e o faturamento habilitados. A escolha do projeto, criação/restrição das chaves e configuração de gastos dependem do responsável pela conta. Nenhum serviço pago foi ativado por esta implementação. Não coloque chaves nos arquivos versionáveis nem no ZIP. Referências oficiais: https://developers.google.com/maps/documentation/javascript/get-api-key e https://developers.google.com/maps/documentation/routes/usage-and-billing.

O mapa é servido em `/map/index.html`. Em Mac Catalyst local a origem é `http://localhost:5055`; em Android Emulator é o endereço configurado em AppConfig; em telefone real configure o IP/host acessível da API. Configure as restrições de origem da chave para o ambiente efetivo e valide o funcionamento no WebView. Em ambiente publicado use HTTPS. O token da conta não é enviado na URL/página do mapa.

A cotação dura dez minutos, fica em memória e expira também ao reiniciar a API. Se expirar, o app pede novo cálculo. Geometria não é armazenada permanentemente no histórico. Mantenha as atribuições do Google e verifique as regras do provedor antes de publicar: https://developers.google.com/maps/documentation/routes/policies.

## Sugestão de contribuição

Custo estimado = distância viária em km × custo/km do veículo.
Sugestão individual = custo estimado ÷ (vagas originalmente oferecidas + motorista), arredondada para centavos. Custos informados pelo responsável e configurados localmente em 20/10/2026: carro R$ 0,70/km; moto R$ 0,35/km (versão 2026-10-20-user-v1). Os custos não representam preços de mercado. Sem configuração, a tela informa Estimativa indisponível e permite contribuição digitada ou Gratuita, desde que o percurso real seja selecionado.

O motorista define a referência e pode escolher Gratuita. Proposta zero em uma oferta negociável depende de aceitação; carona gratuita reserva diretamente. Alterar a referência não altera acordos aceitos. Reservas antigas não tinham valores registrados e continuam mostrando Valor não informado.

## Verificação executada e limites

API e Mac Catalyst compilados, com o aviso preexistente CS0108 no Mobile. Testes puros e de MySQL/HTTP em banco descartável verificam histórico privado/paginado, migração, moeda, adapter de rotas, cotação, publicação/atualização, negociação, concorrência e preservação de valores.

As rotas nos testes são fixtures identificadas dentro dos testes, sem endpoint de bypass no produto. Google Routes real foi validado para carro e moto; mapa e busca Places carregaram no navegador. A API validou cotação, contribuição configurada, publicação, reserva e histórico em banco descartável. No percurso de 10.156 m, carro com duas vagas sugeriu R$ 2,37; moto com uma vaga, R$ 1,78. O carregamento dentro do WebView nativo ainda requer inspeção visual. Testes Android/iOS e verificação visual nativa não foram executados. A ferramenta de inspeção nativa não respondeu neste ambiente.

## Busca de locais no Brasil — 20/10/2026

A busca de destino na Inicial e em Agendar nova viagem abre sugestões Google Places, com nomes e endereços. Ao selecionar, preenche o destino e pesquisa as caronas. Na oferta, tocar em saída/destino abre o seletor do percurso com o ponto correspondente ativo; selecione os dois pontos e calcule para confirmar. As sugestões de ambos os pontos usam includedRegionCodes=[br], restringindo os resultados ao Brasil. Marcação manual no mapa permanece disponível.

Verificado: teste controlado de restrição em ambos os campos, retorno de local isolado sem cotação, proteção contra resposta antiga de seleção; 28 checks de cliente; build Mac Catalyst sem erros (CS0108 preexistente). Lista real de sugestões no navegador exibiu locais brasileiros com nome/endereço, sem erros JavaScript. Inspeção visual nativa permanece indisponível neste ambiente.

Referência do provedor: https://developers.google.com/maps/documentation/javascript/reference/places-widget#PlaceAutocompleteElement.includedRegionCodes

## Endereços na própria tela — atualização de 20/10/2026

Substitui o fluxo modal descrito acima: Inicial/agendamento e oferta agora usam AddressSearchField nativo, com sugestões abaixo do campo após três caracteres e pausa de 350 ms. Não abre outra página ou mapa. Na oferta, escolher ambas as sugestões calcula a rota automaticamente; alterações invalidam a cotação anterior e respostas antigas são descartadas. Google Places fica atrás de endpoints autenticados /api/places e /api/places/resolve; chave de servidor permanece fora do app. A chave configurada em Maps:RoutesApiKey precisa autorizar também Places API (New); configuração local validada. Busca restrita a BR e resolução valida país BR. Atribuição Google Maps acompanha as sugestões.

Verificação: checks controlados de autocomplete/país/coordenadas passaram; integração real autenticada retornou sugestões e coordenadas brasileiras, seguida de rota, contribuição, publicação, reserva e histórico em banco descartável. Build Mac Catalyst sem erros (CS0108 preexistente); 28 checks de cliente passaram. Inspeção visual nativa não realizada por indisponibilidade da ferramenta.
