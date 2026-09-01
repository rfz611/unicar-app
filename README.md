@'
# UniCar — Sistema de Caronas Universitárias

Plataforma mobile de caronas compartilhadas exclusiva para a comunidade da Universidade Positivo (UP).

---

## 📁 Estrutura do Projeto

* `database/` — Scripts SQL (criação de tabelas e dados de teste).
* `src/UniCar.API/` — Back-end em C# (ASP.NET Core Web API).
* `src/UniCar.Mobile/` — Aplicativo Mobile (.NET MAUI).

---

## 📌 Painel de Progresso & Afazeres

> Marque com `[x]` conforme cada tarefa for finalizada para atualizar o status do grupo.

### 🗄️ Bloco 1: Banco de Dados & Estrutura Base
- [x] **1.1** Criar script de criação das tabelas (`database/schema.sql`)
- [ ] **1.2** Criar dados fictícios de teste (`database/seed.sql`)
- [ ] **1.3** Subir o banco de dados MySQL para testes

---

### ⚙️ Bloco 2: Back-end API (C# / ASP.NET Core)
- [ ] **2.1** Configurar conexão com o MySQL via Entity Framework
- [ ] **2.2** Módulo de Autenticação (Cadastro com domínio institucional e Login)
- [ ] **2.3** Módulo de Caronas (Criar rota, listar caronas e buscar por destino/data)
- [ ] **2.4** Módulo de Reservas (Solicitar vaga e decremento automático de assentos)
- [ ] **2.5** Módulo de Avaliação (Registrar nota de 1 a 5 e feedback pós-carona)

---

### 📱 Bloco 3: Front-end Mobile (.NET MAUI)
- [ ] **3.1** Criar telas de Autenticação (Login e Cadastro)
- [ ] **3.2** Criar tela de Feed (Cards de caronas com botão de solicitar)
- [ ] **3.3** Criar tela de Publicação (Formulário para o motorista ofertar carona)
- [ ] **3.4** Criar tela de Perfil / Histórico (Dados do usuário e viagens)
- [ ] **3.5** Integrar chamadas do aplicativo com os endpoints da API

---

### 🧪 Bloco 4: Validação & Entrega
- [ ] **4.1** Testar fluxo completo ponta a ponta (Cadastro -> Criar Carona -> Reservar Vaga)
- [ ] **4.2** Gravar demonstração do sistema em funcionamento
'@ | Out-File -FilePath "README.md" -Encoding utf8