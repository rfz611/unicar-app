# UniCar — Especificação do backend a partir da interface

Fonte da verdade: o protótipo aprovado no Claude Design (app UniCar). Cada tela abaixo lista o que o backend precisa oferecer.
Padrões: controllers, EF Core + PostgreSQL, `ApiError(code, message)` com mensagem pronta em português, JWT, horários em UTC no banco e exibidos em America/Sao_Paulo.

Legenda: ✅ já existe · 🔧 existe, ajustar · ➕ criar

---

## 1. Entrada e cadastro
| Tela | Backend |
|---|---|
| Boas-vindas, "Entrar com e-mail da faculdade" | — |
| E-mail institucional, faculdade reconhecida | ✅ `POST /api/auth/request-code` (já devolve `university`) |
| Código de 6 dígitos, reenviar | ✅ `POST /api/auth/verify-code`; reenviar = chamar request-code de novo (limite 1/min) |
| Seu perfil: nome completo, nascimento, celular, CPF, curso (lista), período, turno | ✅ `POST /api/auth/register`, `GET /api/courses` |
| "Tudo pronto" | — |

## 2. Caronas (aba principal)
| Tela | Backend |
|---|---|
| "Para onde, {nome}?" com Localização atual (GPS) | ✅ `GET /api/places/reverse` |
| Alterar origem, buscar destino com nome do Google Maps | ✅ `GET /api/places/autocomplete`, `GET /api/places/{id}` |
| Atalho **Minha faculdade** | 🔧 `GET /api/users/me` deve incluir o campus principal (lat/lng, nome). Se a faculdade tiver mais de um campus, permitir escolher e salvar `User.CampusId` |
| Atalho **Casa** (definir na 1ª vez) | ➕ `GET/PUT/DELETE /api/me/places/home` (usa `SavedPlace`; outros só veem o bairro) |
| Recentes | ➕ `GET /api/me/places/recent` (últimos 5 destinos das viagens do usuário) |
| Rota: distância, tempo, **Pedir agora R$**, **Caronas marcadas a partir de R$** | ✅ `POST /api/quote` (devolve os dois preços e a polyline) |

### 2a. Pedir agora (estilo Uber)
| Tela | Backend |
|---|---|
| "Procurando colegas", cancelar pedido | ➕ `POST /api/ride-requests` {origem, destino} → cria pedido `Searching`; `DELETE /api/ride-requests/{id}` |
| Motoristas próximos recebem o pedido | ➕ motorista aprovado com status "dirigindo" (`POST /api/driver/online` com posição e destino) recebe via SignalR pedidos cujo desvio ≤ 2 km / 5 min |
| "Carona aceita", motorista a caminho, ETA, placa e carro | ➕ `POST /api/ride-requests/{id}/accept` (motorista) → vira `Trip`; evento SignalR `TripUpdated` |
| Posição do carro ao vivo | ➕ hub `/hubs/trips`: motorista envia `UpdateLocation(tripId, lat, lng)` a cada ~3 s; passageiro recebe `DriverLocation` |
| "Chegou", "Já embarquei", em viagem, chegada, progresso | ➕ status da Trip: `Accepted → DriverArrived → InProgress → Completed` (`POST /api/trips/{id}/status`) |
| Compartilhar trajeto | ➕ `POST /api/trips/{id}/share` gera link público temporário só com posição e destino (envio ao contato de confiança fica para depois) |
| "Você chegou": Pix ou Cartão, nota 1–5, Pagar | ➕ `POST /api/trips/{id}/complete` {paymentMethod: "Pix"\|"Card", rating} — pagamento real fica para depois; registrar valor, método e status `PendingPayment` |

### 2b. Caronas marcadas
| Tela | Backend |
|---|---|
| Lista "Caronas marcadas" para o destino: horário, "passa por você em X min", preço, vagas, motorista (nome, nota, curso, faculdade), carro | ➕ `GET /api/rides/search?destLat&destLng&originLat&originLng&date` — destino a até 2 km, ordenar por horário; calcular "passa por você" pela rota |
| Reservar lugar / Reservado | ➕ `POST /api/rides/{id}/reservations`, `DELETE /api/rides/{id}/reservations/mine` |
| Sugerir "10 min antes / depois" | ➕ `POST /api/rides/{id}/time-suggestions` {minutes: -10\|+10} → vira mensagem no chat + notificação ao motorista; motorista aceita em `POST .../time-suggestions/{sid}/accept` (altera o horário e avisa todos) |
| "Ninguém indo para lá" → Me avise | ➕ `POST /api/route-alerts` {destino, raio} — avisar quando surgir carona |

Regras de reserva: não reservar a própria carona, nem duas vezes, nem acima das vagas, nem depois da saída; cancelar até 30 min antes. Concorrência na última vaga (transação ou `xmin` do PostgreSQL como token de concorrência).

## 3. Viagens (aba)
| Tela | Backend |
|---|---|
| Próximas: reservas (com Mensagem/Cancelar) e caronas que eu ofereci (Passageiros/Cancelar) | ➕ `GET /api/me/trips` → `{ upcoming: [...], history: [...] }` com tipo `Passenger`/`Driver` |
| Histórico: trajeto, com quem, data, valor | idem |
| Ponto azul na aba quando há viagem próxima | `upcoming.length > 0` |

## 4. Oferecer (aba)
| Tela | Backend |
|---|---|
| Bloqueado até virar motorista; "em análise" | ✅ `User.DriverStatus` (None/InReview/Approved/Rejected) |
| Saindo de / Indo para (busca Google), saída HH:MM, vagas 1–4 | ➕ `POST /api/rides` |
| "Você recebe por passageiro R$" | ➕ `POST /api/rides/estimate` {origem, destino} → `DriverAmount` do preço Scheduled |
| Publicar → aparece em Viagens | ➕ |

## 5. Perfil
| Tela | Backend |
|---|---|
| Foto (trocar) | ➕ `POST /api/users/me/photo` (multipart, jpg/png/webp ≤ 5 MB, redimensionar 512px; em dev salvar em `wwwroot/uploads`, depois storage) |
| Nome, bio, etiquetas (curso, período, faculdade, turno) | ✅ `GET/PUT /api/users/me` (🔧 incluir `ShowCourse` das preferências) |
| Virar motorista / Cadastro em análise / Motorista verificado | ➕ `POST /api/driver/application` {cnh (11 dígitos, validar dígitos verificadores), validade MM/AAAA (não vencida), placa (ABC1234 ou ABC1D23), modelo, cor} + `POST /api/driver/application/cnh-photo` e `/selfie`; status vai para `InReview`. Aprovação manual: `POST /api/admin/drivers/{userId}/approve\|reject` (role Admin) |
| Editar perfil (nome, nascimento, celular, CPF só leitura, curso, período, turno, gênero opcional, sobre você ≤140) | ✅ `PUT /api/users/me` |
| E-mail da faculdade: verificado em | ✅ `emailVerifiedAt` no `/me` |

## 6. Configurações
| Tela | Backend |
|---|---|
| Aparência escuro/claro | ➕ `UserPreferences.Theme` |
| Avisos de carona, Compartilhar trajeto sempre, Mostrar meu curso | ➕ `GET/PUT /api/users/me/preferences` |
| Contato de confiança (telefone) | ✅ `TrustedContactPhone` no `PUT /me` |
| Sair da conta | só no app (apagar token); opcional `POST /api/auth/logout` |

## 7. Chat
| Tela | Backend |
|---|---|
| Conversa com o motorista (por viagem ou carona marcada), respostas rápidas, "digitando…" | ➕ entidade `Message` (conversationId = tripId ou rideId, autor, texto ≤ 500, data). `GET /api/conversations/{id}/messages`; envio pelo hub `SendMessage`, evento `MessageReceived`, `Typing` |
| Só participantes acessam | motorista + passageiros confirmados |

## 8. Avaliações
➕ `Rating` (tripId, de, para, nota 1–5). Nota média em `PublicUserDto.Rating` e número de viagens.

---

## Modelo de dados final (resumo)
`University`, `UniversityDomain`, `Campus` ✅ · `User` 🔧 (+CampusId, +Role) · `UserPreferences` ➕ · `SavedPlace` ✅ · `EmailVerificationCode` ✅ ·
`DriverApplication` ➕ · `Vehicle` ➕ · `Ride` (carona marcada) ➕ · `Reservation` ➕ · `TimeSuggestion` ➕ · `RideRequest` (pedir agora) ➕ ·
`Trip` (corrida em andamento/concluída, de qualquer modo) ➕ · `Message` ➕ · `Rating` ➕ · `RouteAlert` ➕

## Ordem sugerida
1. Migrations (trocar `EnsureCreated`), `User.CampusId`, `UserPreferences`, lugares salvos.
2. Veículo + motorista (cadastro, análise, admin) — necessário para oferecer.
3. Caronas marcadas + reservas + Viagens.
4. SignalR: pedir agora, posição ao vivo, status, chat.
5. Avaliações, foto de perfil, preferências.
6. Seed de demonstração (só em Development).

Cada etapa: `dotnet build` + `dotnet test` verdes e commit.
