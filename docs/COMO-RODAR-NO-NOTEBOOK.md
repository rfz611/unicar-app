# Como rodar o UniCar num notebook novo (Windows)

Precisa de internet só para instalar e baixar. Depois de pronto, a apresentação funciona sem internet,
porque a API, o banco e o app rodam no próprio notebook.

Tempo total: uns 30 minutos. Faça antes da apresentação, não na hora.

## 1. Instalar os programas

Abra o **PowerShell** (tecla Windows, digite PowerShell, Enter) e rode um comando por vez.
Se o Windows perguntar se permite alterações, clique em Sim.

```powershell
winget install --id Git.Git -e
```
```powershell
winget install --id Microsoft.DotNet.SDK.8 -e
```
```powershell
winget install --id OpenJS.NodeJS.LTS -e
```

**PostgreSQL (banco de dados):** baixe o instalador da versão 16 para Windows em
https://www.enterprisedb.com/downloads/postgres-postgresql-downloads e instale clicando em Next.

- Quando pedir uma senha (é a senha do usuário `postgres`), crie uma e **anote**.
- Deixe a porta **5432**.
- No fim, desmarque o "Stack Builder" e clique em Finish.

Depois de instalar tudo, **feche o PowerShell e abra de novo**, para ele enxergar os programas novos.

## 2. Criar o banco do UniCar

No PowerShell, rode os dois comandos abaixo. Cada um pede a senha do `postgres` que você anotou
(ao digitar, nada aparece na tela; é normal).

```powershell
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -c "CREATE ROLE unicar LOGIN PASSWORD 'unicar_dev' CREATEDB;"
```
```powershell
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -c "CREATE DATABASE unicar OWNER unicar;"
```

Cada um deve responder `CREATE ROLE` e `CREATE DATABASE`.

## 3. Baixar o projeto

```powershell
cd $HOME\Documents
```
```powershell
git clone https://github.com/rfz611/unicar-app.git
```

## 4. Ligar a API (janela 1)

```powershell
cd $HOME\Documents\unicar-app
```
```powershell
dotnet run --project src\UniCar.Api
```

A primeira vez demora um ou dois minutos. Está pronta quando aparecer `Now listening on: http://localhost:5080`.
Se o Windows perguntar sobre o firewall, clique em Permitir. **Deixe esta janela aberta.**

## 5. Ligar o app (janela 2)

Abra **outro** PowerShell:

```powershell
cd $HOME\Documents\unicar-app\mobile
```
```powershell
npm install
```
```powershell
npx expo start --web
```

O `npm install` leva alguns minutos na primeira vez. Depois o app abre no navegador em http://localhost:8081.
**Deixe esta janela aberta também.**

## 6. Na hora de apresentar

- **Formato de celular:** no navegador, aperte F12 e depois Ctrl+Shift+M, e escolha um iPhone na lista de cima.
- **Entrar sem código de e-mail:** na tela de boas-vindas, toque 5 vezes no logo "unicar" e escolha a conta.
- **Horários atualizados:** no mesmo lugar, toque em "Recriar demonstração" antes de começar.
- **Passageiro e motorista ao mesmo tempo:** abra duas janelas do navegador lado a lado, uma em
  http://localhost:8081 (Bruno, motorista) e outra em http://127.0.0.1:8081 (Ana, passageira).

### Roteiro

1. **Caronas marcadas:** Ana busca "polit", vê a rota e o preço e reserva.
2. **Pedir agora:** Bruno vai em Oferecer, Dirigir agora, Saindo de "Batel", Indo para "Minha faculdade",
   Começar a dirigir. Ana toca em Pedir carona. Bruno recebe e aceita; Ana vê o carro chegando.
3. **Corrida:** Bruno toca em Cheguei, Ana em Já embarquei, os dois conversam pelo chat.
4. **Fim:** Bruno toca em Finalizar corrida; Ana dá a nota e escolhe Pix.
5. **Se der tempo:** Perfil, Configurações, Viagens e Virar motorista.

## Nas próximas vezes

Os passos 1, 2 e 3 são só da primeira vez. Depois, basta abrir as duas janelas: `dotnet run --project src\UniCar.Api`
na pasta `unicar-app` e `npx expo start --web` na pasta `unicar-app\mobile`.

## Se der problema

| O que aparece | O que fazer |
|---|---|
| `winget` não é reconhecido | Instale o "Instalador de Aplicativo" pela Microsoft Store, ou baixe Git, .NET 8 SDK e Node.js LTS dos sites oficiais |
| `dotnet`, `git` ou `npm` não é reconhecido | Feche e abra o PowerShell de novo |
| A API mostra erro de senha ou conexão com o banco | Refaça o passo 2 e confira se o PostgreSQL está instalado na porta 5432 |
| O app mostra "Sem conexão com o UniCar" | Confira se a janela da API está aberta e sem erro em vermelho |
| As caronas sumiram ou os horários já passaram | 5 toques no logo e "Recriar demonstração" |
