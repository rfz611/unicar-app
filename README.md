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
src/UniCar.Persistencia/   entrega de persistência: console com EF Core e SQLite (ver o README dele)
tests/UniCar.Tests/   testes da API
tests/UniCar.Persistencia.Tests/   testes da persistência (SQLite em memória)
mobile/               app (Expo)
  src/app/            telas
  src/features/       caronas, corrida do passageiro e do motorista
  src/components/     botões, campos, mapa, barra de abas
docs/                 especificação, protótipo das telas e documentos da entrega
```

