# UniCar — contexto do projeto

App de caronas **só para universitários de Curitiba e região**. Também será usado como trabalho de faculdade, por isso o backend é em **C#**. Responda sempre em português.

## Stack (decidida)
- **Backend:** C# / ASP.NET Core (net8.0, roda em .NET mais novo), controllers, EF Core + **PostgreSQL** (docker-compose.yml), JWT.
- **App:** React Native + Expo (ainda não começado). As telas já foram desenhadas e aprovadas num protótipo do Claude Design.
- **Mapas:** Google — Places API (New) para busca de endereço, Geocoding para o GPS, Routes API para rota/km/tempo. A chave fica **só no servidor** (`appsettings.Local.json`, fora do Git).
- **Tempo real (próximo):** SignalR para posição do motorista ao vivo, "pedir agora" e chat.

## Regra de ouro
O backend faz o trabalho pesado (rota, preço, validação, tempo real); o app só mostra e envia. Nada de lógica de preço ou validação só no app.

## Estado atual
- Bloco 1 **compilado e testado** (commit "feat: bloco 1 do backend compilado e testado"): login por código no e-mail da faculdade, cadastro (maior de 18, CPF válido e único, celular, curso da lista, período, turno), perfil, busca de endereço/GPS pelo Google, rota e preço.
- Correções já feitas: desconto da carona marcada antes do mínimo; sem chave do Google, busca e preço respondem 503.
- O computador foi formatado: o banco do Docker foi apagado (rodar `docker compose up -d` de novo) e `bin/obj` apontam para o caminho antigo (um `dotnet build` resolve).
- Ainda não existe `appsettings.Local.json` (chave do Google).
- Banco com **migrations** (`src/UniCar.Api/Data/Migrations`), aplicadas ao subir a API. Nova: `dotnet ef migrations add Nome --project src/UniCar.Api --output-dir Data/Migrations`.
- Testes de integração (`tests/UniCar.Tests`) sobem a API com um banco PostgreSQL temporário (`ApiFactory`); precisam do PostgreSQL local rodando.
- Etapas da especificação feitas: 1 (migrations, campus, preferências, casa) e 2 (`Driver/`: pedido de motorista, fotos em `App_Data/private` fora do Git e do wwwroot, painel admin com política `Admin` conferida no banco).
- Etapa 3 (`Rides/`): caronas marcadas, busca, reservas (última vaga protegida por `xmin`), sugestão de horário, Viagens, recentes, alertas "Me avise". Pendências para a etapa 4 marcadas com `TODO etapa 4` (chat e avisos via SignalR). Sem chave do Google, "passa por você" usa estimativa (`Geo.EstimateRoute`, `pickup.estimated = true`); publicar carona exige a chave (503).
- Docker não roda neste PC (WSL2/Plataforma de Máquina Virtual desligada); usando PostgreSQL 16 instalado no Windows.

## O que falta: siga `docs/ESPECIFICACAO.md`
A especificação lista, tela por tela do protótipo aprovado, cada rota, entidade e regra que falta (caronas marcadas, reservas, viagens, motorista e veículo, pedir agora com SignalR, posição ao vivo, chat, avaliações, foto, preferências). Ela é a fonte da verdade. Se algo nela conflitar com este arquivo, pergunte.

## Decisões de produto já tomadas
- Dois jeitos de pegar carona: **Pedir agora** (estilo Uber: avisa quem está dirigindo na mesma direção) e **Caronas marcadas** (motorista publica horário e vagas; passageiro reserva; dá para sugerir "10 min antes/depois" pelo chat).
- Todo mundo cria conta como passageiro; **vira motorista depois** pelo perfil (CNH validada pelos dígitos, foto da CNH + selfie, análise manual num painel; integração oficial tipo Serpro só com CNPJ).
- Pagamento (Pix ou cartão) é escolhido **no fim da corrida**, não nas configurações. UniCar fica com 15%.
- Casa salva no perfil; outros só veem o bairro. Faculdade vem do e-mail.
- Compartilhar trajeto com contato de confiança. Avaliação de 1 a 5 ao fim da corrida.
- Rede social (feed entre faculdades, atléticas, conquistas, LinkedIn) é **fase 2**, depois das caronas.
- Visual: preto/branco com azul royal (#2347E0) de destaque, fonte Geist, tema claro e escuro.

Prazo: apresentação na sexta-feira, 23/10/2026. Priorize o que demonstra o fluxo principal funcionando.
