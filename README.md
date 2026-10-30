# UniCar

O UniCar é um app de caronas feito só para universitários de Curitiba e região. Para entrar, a pessoa usa o
e-mail da faculdade e confirma com um código; assim todo mundo no app é estudante de verdade.

Existem dois jeitos de pegar carona:

- **Pedir agora**: o passageiro diz para onde vai e o app avisa quem está dirigindo naquela direção. O motorista
  aceita, e o passageiro acompanha o carro chegando no mapa.
- **Caronas marcadas**: o motorista publica o horário e quantas vagas tem, e quem vai para o mesmo lado reserva.

Todo mundo começa como passageiro. Para oferecer carona, a pessoa envia a CNH, uma selfie e os dados do carro, e a
equipe confere antes de liberar. O preço é calculado pela API a partir da rota (km e tempo), e o pagamento (Pix ou
cartão) é escolhido no fim da corrida. Também dá para conversar pelo chat, compartilhar o trajeto e avaliar a carona.

Trabalho da faculdade, apresentado em outubro de 2026.

## Integrantes

- Kamila Gabriella Brizola
- Nicollas Teixeira Gomes
- Raphael Scharf Passos
- Vinícius Lada
- Vitória Silva do Vale

## Tecnologias

- **API**: C# com ASP.NET Core 8, Entity Framework Core e PostgreSQL, login com JWT.
- **Tempo real**: SignalR, usado para a posição do motorista, os pedidos de carona e o chat.
- **Mapas**: Google Maps (Places, Geocoding e Routes). A chave fica só no servidor.
- **App**: React Native com Expo e TypeScript, usando o Expo Router.
- **Testes**: xUnit, com testes de integração que sobem a API com um banco temporário.

## Estrutura

```
src/UniCar.Api/       API
  Auth/               login por código no e-mail da faculdade
  Users/              perfil, foto, preferências, casa
  Places/             busca de endereço e rotas (Google)
  Pricing/            cálculo do preço
  Rides/              caronas marcadas, reservas, viagens, alertas
  Trips/              pedir agora e corridas
  Driver/             pedido para virar motorista e painel de análise
  Chat/, Realtime/    chat e hub do SignalR
  Data/               banco, migrations e dados de demonstração
tests/UniCar.Tests/   testes da API
mobile/               app (Expo)
  src/app/            telas
  src/features/       caronas, corrida do passageiro e do motorista
  src/components/     botões, campos, mapa, barra de abas
docs/                 especificação, protótipo das telas e documentos da entrega
```

## Como rodar

Precisa do **.NET 8 SDK** (ou mais novo), do **PostgreSQL** e do **Node.js** (versão LTS).

Para preparar um computador do zero, siga o passo a passo em
[docs/COMO-RODAR-NO-NOTEBOOK.md](docs/COMO-RODAR-NO-NOTEBOOK.md).

### Banco

Instale o PostgreSQL, crie o usuário `unicar` com senha `unicar_dev` e permissão `CREATEDB`, e crie o
banco `unicar`. As tabelas são criadas sozinhas quando a API sobe.

### API

```powershell
dotnet build
dotnet run --project src\UniCar.Api
```

A API fica em http://localhost:5080. O arquivo `src/UniCar.Api/UniCar.Api.http` tem exemplos de cada rota.

Sem servidor de e-mail configurado, o código de login não é enviado: ele aparece no terminal da API, numa linha
`E-MAIL (modo dev)`.

Sem chave do Google, a API funciona em modo de desenvolvimento com uma lista de lugares de Curitiba e rotas
estimadas. Para usar o Google de verdade, crie `src/UniCar.Api/appsettings.Local.json` (esse arquivo não vai para o
Git):

```json
{ "GoogleMaps": { "ApiKey": "SUA_CHAVE" } }
```

### Testes

```powershell
dotnet test
```

Os testes criam um banco `unicar_test_...` no PostgreSQL local e apagam no fim. Para usar outro servidor, defina a
variável `UNICAR_TEST_PG`.

### App

```powershell
cd mobile
npm install
npx expo start --web
```

Abre em http://localhost:8081. No navegador o mapa aparece como um espaço reservado. Para abrir no celular, use
`npx expo start --tunnel` e o Expo Go. Nesse caso, a API também precisa estar acessível pela internet (por exemplo,
com ngrok) e o endereço vai em `EXPO_PUBLIC_API_URL`, num arquivo `mobile/.env.local`.

### Demonstração

Com a API em Development, o banco já vem com contas de exemplo, todas terminadas em `.demo@`: a Ana é passageira,
o Bruno e a Carla são motoristas, a Eduarda está com o cadastro de motorista em análise e `admin.demo@ufpr.br`
aprova motoristas.

Na tela de boas-vindas do app, toque 5 vezes no logo para escolher uma conta e entrar sem código. No mesmo lugar
há o botão para recriar a demonstração com horários a partir de agora.

## Documentos da entrega

Ficam em `docs/entrega`. A especificação completa das telas e das regras está em `docs/ESPECIFICACAO.md`, e o
protótipo aprovado das telas em `docs/prototipo`.
