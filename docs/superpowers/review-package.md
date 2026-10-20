# Revisão final — mapa, preço, negociação e Atividades

Raiz: /Users/nicollau/Downloads/unicar-app (sem Git). Backup anterior: /Users/nicollau/Documents/Codex/2026-10-20/skill-creator-users-nicollau-codex-skills/work/unicar-implementation/before-implementation.zip.
Spec: docs/superpowers/specs/2026-10-20-mapas-negociacao-atividades-design.md.
Plano: docs/superpowers/plans/2026-10-20-mapas-negociacao-atividades.md.
Ledger/rulings: docs/superpowers/implementation-log.md.

Escopo: entidades de contribuição e negociação, migração004, serviços/DTOs/controllers API, adapter Google e cotações em memória, mapa wwwroot/map, Shell com Inicial/Atividades, páginas Activities/Negotiation/RoutePicker, integração de Offer/Home, parsers/clientes Mobile, testes.

Review Focus do plano:
- Passageiro digitando `12,50`, vazio, negativo ou mais de duas casas deve receber validação clara; o cliente envia decimal JSON sem ambiguidade.
- Resposta atrasada do mapa após troca dos pontos/veículo não pode substituir a cotação mais recente.
- Duas respostas simultâneas, reenvio após timeout e versão antiga não podem alterar o preço aceito nem consumir outra vaga.
- Usuário sai e outra conta entra: histórico, propostas e mapa em memória não podem revelar os dados da conta anterior.
- Migração repetida ou dados anteriores sem valores/coordenadas devem manter reservas legíveis e desconhecido distinto de gratuito.

Verificações já executadas: API/mobile builds sem erros (aviso preexistente CS0108 no Mobile); 21 checks Mobile; checks puros preço/adapter; testes isolados SQL/HTTP para migração, privacidade, paginação e negociação/concorrência. Publicação/atualização com provedor de teste está em testes/UniCar.API.Checks/PublishChecks.cs, sendo finalizada. Mapas reais sem credenciais e parâmetros de custo/km: explicitamente pendentes; não habilitar bypass de rota no produto. CUA nativo indisponível por timeout, portanto não há validação visual nativa declarada.

Solicitação: revisão somente leitura, focada em falhas concretas/importantes de autorização, dados, concorrência, navegação, contratos ou comportamento observável. Apontar arquivo/linha e reprodução. Separar defeitos de dependência externa conhecida.
