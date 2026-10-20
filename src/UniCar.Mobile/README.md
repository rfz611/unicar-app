# UniCar.Mobile (.NET MAUI)

App mobile do UniCar, com visual inspirado no Uber (preto/branco, botões grandes, campos cinza).

## Telas desta etapa (Bloco 3.1 — Autenticação)

| Tela | Arquivo | O que faz |
|---|---|---|
| Boas-vindas | `Pages/WelcomePage` | Entrar ou Criar conta |
| Login | `Pages/LoginPage` | `POST /api/Auth/login` |
| Escolha de perfil | `Pages/RoleSelectionPage` | Passageiro, Motorista ou Os dois (`PASSENGER` / `DRIVER` / `BOTH`) |
| Cadastro | `Pages/RegisterPage` | `POST /api/Auth/register` com o perfil escolhido |
| Esqueci minha senha | `Pages/ForgotPasswordPage` + `Pages/ResetPasswordPage` | `POST /api/Auth/forgot-password` (código de 6 dígitos) e `POST /api/Auth/reset-password` |
| Home (provisória) | `Pages/HomePage` | Mostra o usuário logado e o botão Sair |

Fluxo de cadastro: Boas-vindas → Escolha de perfil → Cadastro → Home.
Quem escolhe **Motorista** ou **Os dois** informa também a CNH (número, categoria e validade).
O app e a API conferem: dígitos verificadores, categoria B ou superior (A é só moto), CNH não vencida e CNH única por conta.
Isso valida o formato e a validade, mas **não consulta o Detran/Senatran**: não prova que a CNH é da pessoa nem que está ativa.

Fluxo de senha: Login → Esqueci minha senha → e-mail → código de 6 dígitos (vale 15 min, 5 tentativas) → nova senha → Login.
Ainda não há envio real de e-mail: com a API em `Development` o código aparece no console da API e num aviso na tela do app.
Em produção, troque `ConsoleEmailSender` por uma implementação SMTP (`Services/IEmailSender.cs` na API).

Cor da marca: azul `#052355` (da logo). Ajuste em `Resources/Styles/Styles.xaml`.
O token JWT fica no `SecureStorage`; o app reabre direto na Home se já houver sessão.

## Como rodar

1. Suba o banco e a API (ver `PASSO_A_PASSO.md`): `dotnet run --project src/UniCar.API` (porta 5055).
2. Abra `src/UniCar.Mobile` no Visual Studio / VS Code com o workload **.NET MAUI** instalado (.NET 10).
3. Escolha o destino (emulador Android, simulador iOS ou Mac Catalyst) e execute.

Endereço da API: `Services/AppConfig.cs`
- Emulador Android usa `http://10.0.2.2:5055/` (já configurado).
- Celular físico: troque pelo IP do seu computador na rede (ex.: `http://192.168.0.10:5055/`).

## Estrutura

- `Controls/` — componentes reutilizáveis (`FormField`, `RoleCard`, `BackButton`)
- `Pages/` — telas
- `Services/` — `AuthService` (HTTP), `SessionService` (sessão), `Validators`
- `Models/` — DTOs que espelham a API
- `Resources/Styles/Styles.xaml` — cores e estilos (único lugar para ajustar o visual)
