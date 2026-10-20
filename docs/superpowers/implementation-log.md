# SDD ledger — plan: docs/superpowers/plans/2026-10-20-mapas-negociacao-atividades.md

Execução direta aprovada. Raiz: /Users/nicollau/Downloads/unicar-app. Especificação aprovada: docs/superpowers/specs/2026-10-20-mapas-negociacao-atividades-design.md.

Ruling: projeto sem Git; usar cópia de segurança e este ledger em vez de inicializar repositório/worktree e scripts dependentes de Git — conforme plano aprovado.
Pre-flight: T1→T2/T6 modelos e valores nullable; T2→T4/T5 PricingSuggestionDto; T4→T5 RouteQuoteDto e validade; T1/T6→T3/T7 histórico/ações. Contratos consistentes. T3 consulta propostas pode ser implementada antes da lógica de T6.
Ruling: perfil CAR/MOTO e preços são validados pelo servidor; valores históricos não podem ser recalculados a partir da referência atual. APIs pagas não são provisionadas automaticamente.

Task 1: in progress — migração aditiva e modelos.
Task 2: pending — contribuição.
Task 3: pending — navegação e histórico.
Task 4: pending — rotas reais e cotação.
Task 5: pending — mapa/publicação.
Task 6: pending — negociação.
Task 7: pending — negociação no app.
Task 8: pending — verificação/revisão/entrega.

Task 1: complete — migração preserva linhas e reaplicação (RED ausência → GREEN); API compilada; backup antes de aplicar ao banco local.
Task 2: complete — 8 checks puros passaram (RED tipos ausentes → GREEN).
Task 3: in progress — API Atividades passou privacidade, legado e paginação 20/1 em banco descartável; UI em compilação.
Ruling: histórico ordena por data de partida e Id, pois Ride não tinha UpdatedAt; empates estáveis e datas legíveis.

Task 3: API e UI compiladas; verificações isoladas de histórico passaram. Revisão de sessão em andamento no conjunto final.
Task 4: adapter Google, validação, cotação de dez minutos e estados sem credenciais implementados; checks controlados passaram. Configuração real ainda ausente.
Task 5: WebView, origem/nonce, seleção e cálculo, contribuição gratuita/negociável e atualização de oferta implementados; validação real de mapa depende das chaves.
Task 6: lógica de negociação executada antes do acabamento de T5 para validar o backend primeiro — Ruling: não muda contratos nem escopo, reduz acoplamento da validação às credenciais de mapas. Testes reais de banco passaram para negociação, preço imutável, reenvio e disputa por última vaga.
Task 7: telas e serviços implementados; parsers e ponte testados; revisão final pendente.
Ruling: labels de encontro confirmadas pelo usuário persistem; geometria só permanece na cotação em memória. Legado continua com valor desconhecido.

Task 3: complete (código + build + API: privacidade, visão motorista, legado, paginação estável). Teste visual nativo depende de ferramenta disponível; não declarado como feito.
Task 4: complete em testes controlados (adapter, tipo de veículo, ausência de chave/rota, validade e propriedade); integração real Google pendente.
Task 5: código e testes controlados de publicação/atualização implementados. UI compila; mapas reais bloqueados pelas credenciais/configuração externas ainda não fornecidas.
Task 6: complete — MySQL/HTTP verificou proposta/contraproposta, autorizações, versões, valor imutável, gratuito, retirada/reabertura, replay idempotente e aceitação concorrente.
Task 7: código compila; 21 checks de cliente/parser passaram; ciclo real de negociação verificado pela API; inspeção visual nativa indisponível (CUA timeout).
Task 8: in progress — revisão independente final iniciada, documentação atualizada e suíte integrada final em execução.
Ruling: testes antigos de reservas passaram a usar o harness isolado, pois criação agora exige cotação autenticada. Fixtures são criadas só no banco descartável; não há endpoint de bypass em produção.
Ruling: política de congelamento impede alterar trajeto de oferta com reserva/proposta pendente; valores de referência podem mudar no mesmo modo, mantendo cada acordo já aceito.

Conclusão T4–T8 em 20/10/2026: credenciais locais configuradas fora do projeto; Google Routes CAR/MOTO e Places reais responderam. Custos aprovados: carro 0,70 e moto 0,35 BRL/km, versão 2026-10-20-user-v1. Teste real de 10.156 m confirmou 2,37 (carro/2 vagas) e 1,78 (moto/1 vaga), mais publicação, reserva e histórico em banco isolado.
Revisão independente final concluída: quatro P2 corrigidos (respostas de Places/localização obsoletas, sugestão sobrescrevendo preço manual, edição isolada de referência/atualização de legado com reservas, retorno rápido à Inicial). Checks de regressão verificam essas condições. Guarda de sessão também impede resposta de reserva anterior em nova conta.
Verificação final: suíte MySQL/HTTP completa passou; 28 checks de cliente passaram; map_selection_check passou; build Mac Catalyst sem erros, com CS0108 preexistente. API real reiniciada em Development para ler user secrets. Inspeção visual nativa indisponível por timeout da ferramenta; não declarada como realizada.

## Busca de locais no Brasil — 20/10/2026

A busca de destino na Inicial e em Agendar nova viagem abre sugestões Google Places, com nomes e endereços. Ao selecionar, preenche o destino e pesquisa as caronas. Na oferta, tocar em saída/destino abre o seletor do percurso com o ponto correspondente ativo; selecione os dois pontos e calcule para confirmar. As sugestões de ambos os pontos usam includedRegionCodes=[br], restringindo os resultados ao Brasil. Marcação manual no mapa permanece disponível.

Verificado: teste controlado de restrição em ambos os campos, retorno de local isolado sem cotação, proteção contra resposta antiga de seleção; 28 checks de cliente; build Mac Catalyst sem erros (CS0108 preexistente). Lista real de sugestões no navegador exibiu locais brasileiros com nome/endereço, sem erros JavaScript. Inspeção visual nativa permanece indisponível neste ambiente.

Referência do provedor: https://developers.google.com/maps/documentation/javascript/reference/places-widget#PlaceAutocompleteElement.includedRegionCodes

Simplificação autorizada da oferta: botão duplicado de escolher percurso removido; campos Saída/Destino são os pontos de entrada da busca. Nova oferta herda CAR/MOTO da aba Inicial e mostra apenas o tipo, sem pedir novamente. Edição existente mantém seletor. Build Mac Catalyst passou com zero erros e CS0108 preexistente.

## Endereços na própria tela — atualização de 20/10/2026

Substitui o fluxo modal descrito acima: Inicial/agendamento e oferta agora usam AddressSearchField nativo, com sugestões abaixo do campo após três caracteres e pausa de 350 ms. Não abre outra página ou mapa. Na oferta, escolher ambas as sugestões calcula a rota automaticamente; alterações invalidam a cotação anterior e respostas antigas são descartadas. Google Places fica atrás de endpoints autenticados /api/places e /api/places/resolve; chave de servidor permanece fora do app. A chave configurada em Maps:RoutesApiKey precisa autorizar também Places API (New); configuração local validada. Busca restrita a BR e resolução valida país BR. Atribuição Google Maps acompanha as sugestões.

Verificação: checks controlados de autocomplete/país/coordenadas passaram; integração real autenticada retornou sugestões e coordenadas brasileiras, seguida de rota, contribuição, publicação, reserva e histórico em banco descartável. Build Mac Catalyst sem erros (CS0108 preexistente); 28 checks de cliente passaram. Inspeção visual nativa não realizada por indisponibilidade da ferramenta.

Campos de endereço alinhados ao FormField existente: fundo F1F4F9, altura 56, raio 8, margem interna 14, texto 16 e borda Brand ao focar. Retirado contêiner externo duplicado da busca Inicial. Build Mac Catalyst passou, zero erros e aviso CS0108 preexistente.

Sugestões de locais: botões com raio 16, borda suave, espaçamento 8, padding 16/12 e quebra de linha para endereços longos. Build Mac Catalyst sem erros, CS0108 preexistente.

Botão Buscar: fundo azul claro normal, azul escuro e texto branco em PointerOver, azul mais profundo ao pressionar, estado desabilitado próprio. Alinhamento com campo e altura 56. Build Mac Catalyst passou; inspeção visual nativa não realizada.

Sugestões de locais: alinhamento nativo do conteúdo e das linhas à esquerda no Mac/iOS; Gravity Start no Android e HorizontalContentAlignment Left no Windows. Padding mantido. Build Mac Catalyst sem erros; demais plataformas não compiladas neste ajuste.
