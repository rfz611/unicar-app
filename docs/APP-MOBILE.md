# UniCar — App mobile (Expo)

Objetivo: o app rodando **no celular** na apresentação de sexta-feira, 30/10/2026, falando com o backend real.

## Referência visual
`docs/prototipo/UniCar-prototipo.dc.html` é o protótipo aprovado (Claude Design), um HTML com todas as telas, textos, cores, espaçamentos e o fluxo inteiro na lógica do `<script>` no fim do arquivo. **Copie de lá** valores exatos (cores, tamanhos de fonte, raios, paddings) e os textos. Não invente telas novas; a lógica de negócio fica no backend, o protótipo só simula.

Os dados falsos do protótipo (lista `P` de lugares, `RIDES`, Marina chegando, respostas automáticas no chat) **não vão para o app**: tudo vem da API.

## Stack
- Expo (SDK estável mais recente) + **TypeScript** + **expo-router** (abas e pilhas).
- **react-native-maps** para o mapa real (Google no Android, Apple Maps no iOS); desenhar a rota com a polyline que `POST /api/quote` devolve.
- **expo-location** (GPS), **@microsoft/signalr** (hub `/hubs/trips`), **expo-secure-store** (token JWT), **expo-image-picker** (foto de perfil e CNH).
- **react-native-reanimated** + **react-native-svg** para a barra de abas animada.
- Fonte **Geist** carregada com expo-font (arquivos da Google Fonts no projeto).
- Funcionar no **Expo Go** (sem build nativo) para a apresentação.

## Design (tirado do protótipo)
- Destaque: azul royal `#2347E0` (texto branco por cima).
- Tema escuro (padrão): fundo `#141416`, superfícies `#1C1C1F` / `#222226` / `#2A2A2F`, linhas `#2F2F35`, texto `#E8E8E3`, secundário `#A6A6AB` / `#8F8F96`, erro `#FF9F8A`.
- Tema claro: fundo `#F4F4F2`, superfícies `#FFFFFF` / `#EDEDEA` / `#E2E2DE`, linhas `#DADAD5`, texto `#0A0A0B`, secundário `#4E4E53` / `#66666B`, erro `#B3261E`.
- Rótulos em frase normal (ex.: "Nome completo"), **nunca** em CAIXA ALTA espaçada nem fonte mono.
- Horário de saída grande (44px, peso 600, números tabulares) é o destaque de cada carona.
- Listas com separadores finos, sem ícone dentro de quadradinho.
- Sem brilhos/gradientes, exceto no ícone da tela de boas-vindas.
- Toques ≥ 44px. Tema segue a preferência salva em `/api/users/me/preferences`.
- **Barra de abas** (Caronas, Viagens, Oferecer, Perfil): a aba ativa vira um círculo azul de 56px com o ícone branco, saltado 22px acima da barra, e a barra tem um recorte curvo em volta; ao trocar de aba o círculo e o recorte deslizam juntos (mola com leve quique, ~600ms) e o círculo estica na horizontal no meio do caminho. Ver `.tabbar`, `.bubble`, `blob0/blob1` no protótipo.

## Telas → API
| Tela | API |
|---|---|
| Boas-vindas → e-mail → código → seu perfil → tudo pronto | `POST /api/auth/request-code`, `verify-code`, `register`, `GET /api/courses` |
| Caronas: "Para onde, {nome}?", localização atual, alterar origem, buscar destino, Minha faculdade, Casa, recentes | `GET /api/places/reverse`, `autocomplete` (sessionToken por busca), `GET /api/places/{id}`, `GET /api/users/me`, `/api/me/places/home`, `/api/me/places/recent` |
| Rota: distância, tempo, Pedir agora / Caronas marcadas | `POST /api/quote` |
| Pedir agora → procurando → aceita → motorista a caminho (posição ao vivo, ETA, placa) → chegou → em viagem → você chegou (Pix/Cartão, nota) | `POST /api/ride-requests`, hub `/hubs/trips` (`DriverLocation`, `TripUpdated`), `POST /api/trips/{id}/...`, avaliação |
| Caronas marcadas: lista, reservar, sugerir 10 min antes/depois, Me avise | `GET /api/rides/search`, reservas, sugestões de horário, alertas |
| Chat (corrida e carona marcada) | `GET` histórico + hub `SendMessage` / `MessageReceived` / `Typing` |
| Viagens: próximas e histórico | `GET /api/me/trips` |
| Oferecer (só motorista aprovado): origem, destino, saída, vagas, "você recebe" | `POST /api/rides/estimate`, `POST /api/rides` |
| Perfil: foto, etiquetas, virar motorista, editar, e-mail verificado, aparência, configurações | `/api/users/me`, `/photo`, `/preferences`, `/api/driver/application` (+ fotos) |

O lado do **motorista** (aceitar pedido, enviar posição a cada ~3 s, mudar status) precisa existir no app também, para demonstrar com dois celulares (ou um celular e o painel/`demo.html` do backend).

## Rodar no celular na apresentação
O celular precisa alcançar a API. Em ordem de preferência:
1. **Túnel HTTPS** (recomendado para sexta): API rodando no notebook + `ngrok http 5080` (domínio fixo grátis) ou Cloudflare Tunnel. Funciona em qualquer rede, até no 4G do celular. O app lê a URL de `EXPO_PUBLIC_API_URL`.
2. Mesma rede Wi-Fi: API em `http://0.0.0.0:5080`, liberar a porta no Firewall do Windows, app usando o IP do notebook. **Arriscado na faculdade** (redes que isolam os aparelhos).
3. Backend na nuvem (Render/Railway + PostgreSQL): mais robusto, mais trabalho; para depois.

App: `npx expo start --tunnel` e abrir no **Expo Go**. Plano B: build Android (APK) com EAS para instalar direto.

Modo apresentação: subir a API em Development com o seed de demonstração (`Demo:Enabled`) e usar `POST /api/dev/login` num botão escondido (ex.: tocar 5x no logo) para entrar sem depender do e-mail. Ter `POST /api/dev/demo/reset` à mão para recomeçar.

## Ordem de construção
1. Projeto Expo, tema (tokens acima), fonte, navegação com a barra de abas animada, cliente da API com token, tratamento de erro (`ApiError.message` aparece para o usuário).
2. Entrada e cadastro completos.
3. Caronas: mapa, busca, rota e preço, caronas marcadas, reservar.
4. Pedir agora + tempo real (passageiro e motorista) + chat.
5. Viagens, Oferecer, Perfil, Configurações, virar motorista, foto.
6. Túnel, teste no celular, modo apresentação, ensaio do roteiro.
Cada etapa: rodar no Expo Go, conferir no celular, commit.
