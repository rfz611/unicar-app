# UniCar — contexto do projeto

App de caronas **só para universitários de Curitiba e região**. Também será usado como trabalho de faculdade, por isso o backend é em **C#**. Responda sempre em português.

## Stack (decidida)
- **Backend:** C# / ASP.NET Core (net8.0, roda em .NET mais novo), controllers, EF Core + **PostgreSQL** (docker-compose.yml), JWT.
- **App:** React Native + Expo (ainda não começado). As telas já foram desenhadas e aprovadas num protótipo do Claude Design.
- **Mapas:** Google — Places API (New) para busca de endereço, Geocoding para o GPS, Routes API para rota/km/tempo. A chave fica **só no servidor** (`appsettings.Local.json`, fora do Git).
- **Tempo real (próximo):** SignalR para posição do motorista ao vivo, "pedir agora" e chat.

## Regra de ouro
O backend faz o trabalho pesado (rota, preço, validação, tempo real); o app só mostra e envia. Nada de lógica de preço ou validação só no app.

## O que já existe (bloco 1) — ainda NÃO compilado
Foi escrito sem acesso ao .NET. **Primeira tarefa: `dotnet build` e `dotnet test`, corrigir o que falhar.**
- `Auth/`: login pelo e-mail da faculdade com código de 6 dígitos (10 min, 5 tentativas, 1/min, 5/hora); token de cadastro curto; `register`.
- `Users/`: perfil, validações (maior de 18, CPF com dígitos verificadores, um CPF por conta, celular com DDD, curso da lista fechada, período 1–12, turno Manhã/Tarde/Noite).
- CPF criptografado (Data Protection) + hash HMAC para unicidade; API só devolve mascarado.
- `Places/`: autocomplete restrito ao retângulo da região metropolitana, detalhes do lugar, reverse geocode.
- `Pricing/`: preço = (R$2 + R$0,90/km + R$0,12/min) × 1,15, arredonda a R$0,50, mínimo R$7. Carona marcada: ×0,90, mínimo R$6. Valores em `appsettings.json` → `Pricing`.
- Faculdades iniciais em `Data/SeedData.cs` (UFPR, PUCPR, UTFPR, Positivo, FAE). Domínios de e-mail e coordenadas dos campi precisam ser conferidos.
- Banco criado com `EnsureCreated` (dev). Trocar por migrations quando o modelo estabilizar.

## Decisões de produto já tomadas
- Dois jeitos de pegar carona: **Pedir agora** (estilo Uber: avisa quem está dirigindo na mesma direção) e **Caronas marcadas** (motorista publica horário e vagas; passageiro reserva; dá para sugerir "10 min antes/depois" pelo chat).
- Todo mundo cria conta como passageiro; **vira motorista depois** pelo perfil (CNH validada pelos dígitos, foto da CNH + selfie, análise manual num painel; integração oficial tipo Serpro só com CNPJ).
- Pagamento (Pix ou cartão) é escolhido **no fim da corrida**, não nas configurações. UniCar fica com 15%.
- Casa salva no perfil; outros só veem o bairro. Faculdade vem do e-mail.
- Compartilhar trajeto com contato de confiança. Avaliação de 1 a 5 ao fim da corrida.
- Rede social (feed entre faculdades, atléticas, conquistas, LinkedIn) é **fase 2**, depois das caronas.
- Visual: preto/branco com azul royal (#3D63F5) de destaque, fonte Geist, tema claro e escuro.

## Próximos blocos
1. Compilar e testar o bloco 1.
2. Caronas: oferecer, buscar, reservar, cancelar; lugares salvos.
3. SignalR: pedir agora, motorista a caminho, posição ao vivo, chat.
4. Motorista (CNH, veículo, análise), avaliações, pagamento.
5. App Expo seguindo as telas do protótipo.

Prazo: algo funcional e apresentável até **sexta-feira, 23/10/2026**.
