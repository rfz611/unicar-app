# 🚀 PASSO A PASSO - UNICAR API

---

## 📋 PRÉ-REQUISITOS (O QUE INSTALAR NA MÁQUINA)
1. .NET 10 SDK
2. Docker Desktop

---

## 🛠️ PASSO 1: SUBIR O BANCO DE DADOS
Abra o Docker Desktop e execute no terminal:

docker start unicar-app

---

## 🗃️ PASSO 1.5: ATUALIZAR O BANCO (CNH e recuperação de senha)
Escolha UM caminho:

dotnet ef migrations add AddCnhAndPasswordReset --project unicar-app/src/UniCar.API
dotnet ef database update --project unicar-app/src/UniCar.API

ou execute o script database/002_cnh_e_reset_senha.sql no MySQL.

---

## 🚀 PASSO 2: EXECUTAR A API BACKEND
No terminal, na pasta do projeto, execute:

dotnet run --project unicar-app/src/UniCar.API

Aguarde a mensagem: "Now listening on: http://localhost:5055"

---

## 🧪 PASSO 3: TESTAR O FLUXO COMPLETO VIA TERMINAL
Abra uma SEGUNDA ABA do terminal e rode os comandos abaixo na ordem:

### 1. Fazer Login do Passageiro (Gerar Token)
Set-Content -Path "login.json" -Value '{"email":"lucas.teste@cs.up.edu.br","password":"SenhaSegura123!"}' -Encoding utf8
curl.exe -i -X POST "http://localhost:5055/api/Auth/login" -H "Content-Type: application/json" --data-binary "@login.json"

### 2. Reservar Vaga na Carona
Copie a string do "token" retornado no comando anterior, substitua abaixo e rode:
curl.exe -i -X POST "http://localhost:5055/api/Rides/1/join" -H "Authorization: Bearer COLA_O_TOKEN_AQUI"

### 3. Confirmar Abatimento de Vagas (GET)
curl.exe -i -X GET "http://localhost:5055/api/Rides"

---

## 🌐 PASSO 4: ABRIR SWAGGER (INTERFACE GRÁFICA)
Acesse no navegador:
http://localhost:5055/swagger