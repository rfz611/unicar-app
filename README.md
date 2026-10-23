# UniCar — Backend (C# / ASP.NET Core)

API do UniCar: caronas entre universitários de Curitiba.

## O que já tem (bloco 1)

| Parte | Rotas |
|---|---|
| Login por e-mail da faculdade | `POST /api/auth/request-code`, `POST /api/auth/verify-code` |
| Cadastro (nome, nascimento, CPF, celular, curso, período, turno) | `POST /api/auth/register` |
| Perfil | `GET /api/users/me`, `PUT /api/users/me` |
| Listas | `GET /api/courses`, `GET /api/universities` |
| Busca de endereço pelo Google (região de Curitiba) | `GET /api/places/autocomplete`, `GET /api/places/{placeId}` |
| Endereço do GPS | `GET /api/places/reverse?lat=&lng=` |
| Rota real e preço | `POST /api/quote` |

Regras já aplicadas:
- Só entra e-mail de faculdade cadastrada (UFPR, PUCPR, UTFPR, Positivo, FAE).
- Código de 6 dígitos, vale 10 minutos, no máximo 5 tentativas, 1 pedido por minuto e 5 por hora.
- Maior de 18 anos, CPF com dígitos verificadores válidos, um CPF por conta, celular com DDD, curso só da lista.
- CPF guardado **criptografado**; a API só devolve mascarado (`***.***.*47-25`).
- Chave do Google fica só no servidor.
- Preço: R$ 2 + R$ 0,90/km + R$ 0,12/min, mais 15% do UniCar, mínimo R$ 7. Carona marcada: 10% menos, mínimo R$ 6.
  Tudo ajustável em `appsettings.json` → `Pricing`.

## Como rodar no Windows

Precisa de: **.NET 8 SDK ou mais novo** e **PostgreSQL** (pelo Docker ou instalado).

```powershell
cd C:\Users\<voce>\Dev\UniCar

# 1. Banco (com Docker Desktop aberto)
docker compose up -d

# 2. Compilar e rodar os testes
dotnet build
dotnet test

# 3. Subir a API
dotnet run --project src\UniCar.Api
```

Abra **http://localhost:5080** — cai no Swagger, onde dá para testar cada rota.
O arquivo `src/UniCar.Api/UniCar.Api.http` tem o passo a passo do login pronto para o VS Code (extensão REST Client) ou Visual Studio.

**Sem Docker?** Instale o PostgreSQL, crie o usuário `unicar` com senha `unicar_dev` (com permissão `CREATEDB`) e o banco `unicar`,
ou troque a `ConnectionStrings:Default`.

As tabelas são criadas pelas **migrations** quando a API sobe. Para criar uma nova depois de mudar o modelo:

```powershell
dotnet tool restore
dotnet ef migrations add NomeDaMudanca --project src\UniCar.Api --output-dir Data/Migrations
```

Os testes de integração criam um banco temporário `unicar_test_...` no mesmo PostgreSQL e apagam no fim
(por isso o `CREATEDB`). Outro servidor: variável `UNICAR_TEST_PG`.

### Demonstração (apresentação)

Em Development (`Demo:Enabled = true` em `appsettings.Development.json`) a API cria, na primeira vez que sobe,
universitários e caronas de exemplo. Todas as contas terminam em `.demo@...`:

| Conta | Papel |
|---|---|
| `ana.demo@ufpr.br` | Passageira: casa no Água Verde, reserva amanhã 7h30 com o Bruno, 2 viagens no histórico, alerta "Me avise" |
| `bruno.demo@ufpr.br` | Motorista (Onix prata): Batel → Politécnico hoje em ~2h e amanhã 7h30 |
| `carla.demo@pucpr.edu.br` | Motorista (HB20 branco): Água Verde → PUCPR amanhã 7h45, com sugestão de +10 min do Felipe |
| `diego.demo@utfpr.edu.br` | Motorista (Gol preto): carona lotada para a UTFPR |
| `eduarda.demo@up.edu.br` | Pedido de motorista **em análise** |
| `felipe.demo@utfpr.edu.br`, `gabriela.demo@up.edu.br` | Passageiros |
| `admin.demo@ufpr.br` | Admin: aprova ou recusa motoristas |

- **Antes de apresentar**, recrie com horários a partir de agora: `POST /api/dev/demo/reset`.
- **Entrar sem código**: `POST /api/dev/login` com `{ "email": "ana.demo@ufpr.br" }` (só aceita contas `.demo@`).
- Fora de Development, ou com `Demo:Enabled = false`, as rotas `/api/dev` respondem 404 e nada é criado.
- Os preços das caronas de exemplo usam a rota estimada (sem chamar o Google), então funcionam sem chave.

### O código de verificação em desenvolvimento

Sem SMTP configurado, a API **não envia e-mail**: o código aparece no terminal onde a API está rodando,
numa linha `E-MAIL (modo dev) para ...`. Para enviar de verdade, preencha a seção `Smtp`
com um provedor (Resend, Brevo, SendGrid, Amazon SES).

### Chave do Google Maps

1. Crie um projeto em https://console.cloud.google.com e ative o faturamento (há cota grátis mensal).
2. Ative **Places API (New)**, **Routes API** e **Geocoding API**.
3. Crie uma chave em *APIs e serviços → Credenciais* e restrinja a essas três APIs.
4. Crie `src/UniCar.Api/appsettings.Local.json` (fica fora do Git):

```json
{ "GoogleMaps": { "ApiKey": "SUA_CHAVE" } }
```

## Segredos

`appsettings.Development.json` tem chaves **só para desenvolvimento**. Em produção, defina
`Jwt__SigningKey` e `Security__HashPepper` como variáveis de ambiente com valores longos e aleatórios.
A pasta `keys/` guarda a chave que criptografa os CPFs: não apague e não suba para o Git.

## Estrutura

```
src/UniCar.Api
  Auth/      login por e-mail, código, tokens
  Users/     perfil e validações de cadastro
  Places/    Google Places, Geocoding e Routes
  Pricing/   cálculo do preço
  Domain/    entidades (User, University, Campus, SavedPlace, EmailVerificationCode)
  Data/      banco (EF Core + PostgreSQL) e faculdades iniciais
  Common/    CPF, celular, criptografia, cursos
tests/UniCar.Tests   testes de preço e validações
```

## Próximos blocos

2. Caronas: oferecer, buscar, reservar, cancelar; casa e faculdade salvas.
3. Tempo real com SignalR: pedir agora, motorista a caminho, posição ao vivo, chat.
4. Motorista: CNH validada, fotos, análise manual; avaliações; pagamento (Pix/cartão).
