# UniCar — app (Expo)

App do UniCar em Expo (SDK 58) + TypeScript + Expo Router. Visual e textos copiados de
`docs/prototipo/UniCar-prototipo.dc.html`; todos os dados vêm da API (`src/UniCar.Api`).

## Rodar no navegador (desenvolvimento)

1. Suba a API (na raiz do repositório): `dotnet run --project src\UniCar.Api`
2. Aqui em `mobile/`:

```powershell
npm install
npx expo start --web
```

Abre em http://localhost:8081. O mapa aparece como espaço reservado no navegador; no celular é o mapa real.

**Modo apresentação:** na tela de boas-vindas, toque 5 vezes no logo "unicar" (canto de cima). Abre a lista das
contas de demonstração (`*.demo@`) para entrar sem código por e-mail, e o botão "Recriar demonstração".
Só funciona com a API em Development (`Demo:Enabled`).

## Endereço da API

O app lê `EXPO_PUBLIC_API_URL` (padrão `http://localhost:5080`). Para mudar, copie `.env.example` para `.env.local`.
No celular, `localhost` é o próprio celular: use o túnel (ngrok) ou o IP do notebook — veja `docs/APP-MOBILE.md`.

## Estrutura

```
src/app/            telas (Expo Router): (auth) entrada, (tabs) Caronas, Viagens, Oferecer, Perfil
src/components/     peças do protótipo: Text (Geist), Button/Field/Chip/List (ui.tsx), TabBar, Toast, Page
src/theme/          cores e medidas do protótipo (tokens.ts), tema claro/escuro
src/api/            cliente da API (token, erros com a mensagem do backend), tipos
src/session/        sessão: token salvo (SecureStore no celular, localStorage no navegador)
assets/fonts/       Geist 400/500/600/700 (licença OFL)
```

Antes de fechar uma etapa: `npx tsc --noEmit` e `npx expo lint`.
