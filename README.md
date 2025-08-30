# Challenge Avanade 🚀  

Este projeto é um **sistema de vendas e estoque** baseado em **arquitetura de microsserviços**, utilizando **.NET 9**, **RabbitMQ**, **SQL Server** e um **API Gateway com Ocelot**.  

O objetivo é gerenciar pedidos e controle de estoque, com autenticação via JWT.  

---

🚀 Como rodar o projeto com Docker
### 1. Pré-requisitos
  - Docker
  - Docker Compose

### 2. Clonar o repositório
  ```bash
  git clone https://github.com/gvicencotti/challengeavanade.git
  cd challengeavanade
  ```

### 3. Subir todos os serviços com Docker Compose
  ```bash
  docker-compose up --build
  ```
  Isso irá construir e iniciar todos os microsserviços, SQL Server e RabbitMQ automaticamente.

  - RabbitMQ Management UI: http://localhost:15672
    (usuário: guest, senha: guest)
  - API Gateway: http://localhost:5208

### 4. Aplicar migrações do banco de dados
Os serviços tentam aplicar as migrações automaticamente ao iniciar. Se necessário, rodar manualmente:
  ```bash
  docker-compose exec salesservice dotnet ef database update --project SalesService
  docker-compose exec stockservice dotnet ef database update --project StockService
  ```

### 5. Testar os endpoints
  Acesse o Swagger de cada serviço:

  AuthService: http://localhost:5017/swagger
  SalesService: http://localhost:5002/swagger
  StockService: http://localhost:5001/swagger


## 🔑 Principais Endpoints & Exemplos de Uso

### 1. Autenticação via API Gateway

Faça login para obter o token JWT:
```http
POST http://localhost:5208/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "123"
}
```
O token gerado deve ser usado no header `Authorization` para acessar os demais endpoints.

### 2. Criar Pedido (SalesService)
```http
POST http://localhost:5208/api/orders
Authorization: Bearer <seu_token_jwt>
Content-Type: application/json

{
  "customerId": 1,
  "items": [
    { "productId": 2, "quantity": 1 }
  ]
}
```

### 3. Listar Pedidos
```http
GET http://localhost:5208/api/orders
Authorization: Bearer <seu_token_jwt>
```

### 4. Adicionar Produto ao Estoque (StockService)
```http
POST http://localhost:5208/api/products
Authorization: Bearer <seu_token_jwt>
Content-Type: application/json

{
  "name": "Notebook Dell",
  "description": "Notebook potente",
  "quantity": 10,
  "price": 3500.00
}
```

---

## 🧪 Testando com Postman

1. **Faça login** usando o endpoint `/api/auth/login` para obter o token JWT.
2. **Adicione o token** no header `Authorization` de todas as requisições protegidas:
   ```
   Authorization: Bearer <seu_token_jwt>
   ```
3. **Utilize os exemplos acima** para criar pedidos, listar pedidos e adicionar produtos ao estoque.
4. **Explore o Swagger** de cada serviço para ver todos os endpoints disponíveis.

---
