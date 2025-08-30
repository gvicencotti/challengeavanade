# Challenge Avanade 🚀  

Este projeto é um **sistema de vendas e estoque** baseado em **arquitetura de microsserviços**, utilizando **.NET 9**, **RabbitMQ**, **SQL Server** e um **API Gateway com Ocelot**.  

O objetivo é gerenciar pedidos e controle de estoque, com autenticação via JWT.  

---

## 🏗️ Arquitetura  

- **AuthService** → Responsável pela autenticação e geração de tokens JWT.  
- **SalesService** → Gerencia pedidos e integração com estoque.  
- **StockService** → Controla o estoque de produtos.  
- **RabbitMQ** → Mensageria assíncrona para eventos entre serviços.  
- **SQL Server** → Banco de dados relacional para os microsserviços.  
- **ApiGateway** → Porta de entrada única, roteando requisições via **Ocelot**.  

## ⚙️ Tecnologias  

- [.NET 9](https://dotnet.microsoft.com/)  
- [Entity Framework Core](https://learn.microsoft.com/ef/)  
- [RabbitMQ](https://www.rabbitmq.com/)  
- [SQL Server 2022](https://www.microsoft.com/sql-server/)  
- [Ocelot API Gateway](https://ocelot.readthedocs.io/)  
- [Swagger](https://swagger.io/)  

---

## 🚀 Como rodar o projeto  

### 1. Pré-requisitos  
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)  
- [SQL Server](https://www.microsoft.com/sql-server/) (local ou em container)  
- [RabbitMQ](https://www.rabbitmq.com/) (local ou em container)  

### 2. Clonar o repositório  
```bash
git clone https://github.com/gvicencotti/challengeavanade.git
cd challengeavanade
```

### 3. Subir o RabbitMQ com Docker Compose
O arquivo docker-compose.yml já está configurado.

Execute:
```bash
docker-compose up -d
```
Management UI: http://localhost:15672
 (usuário: guest, senha: guest)
 
### 4. Configurar o banco de dados

No SQL Server, crie os bancos de dados:

```bash
CREATE DATABASE SalesDb;
CREATE DATABASE StockDb;
```

As connection strings já estão configuradas no appsettings.json de cada microsserviço.

### 5. Executar as migrações
Rode os comandos abaixo para aplicar as tabelas:
```bash
dotnet ef database update --project SalesService
dotnet ef database update --project StockService
```

### 6. Subir os microsserviços
Abra terminais diferentes e rode:
```bash
dotnet run --project AuthService
dotnet run --project SalesService
dotnet run --project StockService
dotnet run --project ApiGateway
```

### 7. Acessar os serviços

- API Gateway → http://localhost:5208

- RabbitMQ Management → http://localhost:15672
 (user: guest, pass: guest)

Swagger:

- AuthService → http://localhost:5001/swagger

- SalesService → http://localhost:5002/swagger

- StockService → http://localhost:5003/swagger

## 🔑 Endpoints Principais

### 1. Faça login no AuthService para gerar o token:
🔹 AuthService

- Registrar usuário
```bash
http://localhost:5208/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "123"
}
```

### SalesService

- Criar pedido
```bash
POST http://localhost:5208/api/orders
Authorization: Bearer + (Token gerado)

{
  "name": "Notebook Dell",
  "price": 3500,
  "quantity": 10
}
```
- Listar pedidos
```bash
GET http://localhost:5208/api/orders
Authorization: Bearer + (Token gerado)
```

### StockService

- Adicionar produto ao estoque
```bash
POST http://localhost:5208/api/products
Authorization: Bearer + (Token gerado)
```
