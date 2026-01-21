# Users API

> API de gerenciamento de usuários com autenticação JWT e autorização baseada em permissões

## 📋 Índice

- [Sobre o Projeto](#sobre-o-projeto)
- [Funcionalidades](#funcionalidades)
- [Tecnologias](#tecnologias)
- [Arquitetura](#arquitetura)
- [Pré-requisitos](#pré-requisitos)
- [Instalação e Execução](#instalação-e-execução)
- [Endpoints da API](#endpoints-da-api)
- [Configuração](#configuração)
- [Deployment](#deployment)
- [Testes](#testes)
- [Contribuindo](#contribuindo)
- [Licença](#licença)

## 🎯 Sobre o Projeto

O **Users API** é um microserviço robusto desenvolvido em .NET 8 que gerencia todas as operações relacionadas a usuários em uma arquitetura de microserviços. O serviço é responsável por:

- Cadastro de novos usuários
- Autenticação via JWT (JSON Web Tokens)
- Autorização baseada em permissões (Admin/User)
- Validação de tokens para outros microserviços
- Integração com RabbitMQ para eventos de domínio

## ✨ Funcionalidades

### Gestão de Usuários
- ✅ Cadastro de usuários (público e admin)
- ✅ Listagem de usuários (requer permissão Admin)
- ✅ Busca de usuário por ID
- ✅ Atualização de dados do usuário
- ✅ Exclusão de usuários

### Autenticação e Autorização
- ✅ Login com geração de token JWT
- ✅ Validação de tokens JWT para outros serviços
- ✅ Autorização baseada em roles (User/Admin)
- ✅ Hash seguro de senhas com Identity PasswordHasher
- ✅ Tokens com validade de 30 minutos

### Integração e Eventos
- ✅ Publicação de eventos no RabbitMQ (user.created)
- ✅ Health checks para monitoramento
- ✅ Documentação Swagger/ReDoc
- ✅ Middleware de logging com Correlation ID

## 🚀 Tecnologias

### Core
- **.NET 8.0** - Framework principal
- **ASP.NET Core** - Web API
- **Entity Framework Core 8.0** - ORM
- **SQL Server 2022** - Banco de dados

### Segurança
- **JWT Bearer Authentication** - Autenticação
- **ASP.NET Identity** - Hash de senhas
- **FluentValidation** - Validação de entrada

### Mensageria
- **RabbitMQ Client 7.2** - Message broker

### Documentação
- **Swashbuckle (Swagger)** - Documentação OpenAPI
- **ReDoc** - Documentação alternativa

### DevOps
- **Docker** - Containerização
- **Docker Compose** - Orquestração local
- **Kubernetes** - Orquestração em produção

## 🏗️ Arquitetura

O projeto segue uma arquitetura em camadas:

```
UsersApi/
├── UsersApi/          # Camada de apresentação (Controllers, Middlewares)
├── Core/              # Camada de domínio (Entities, DTOs, Interfaces)
├── Infrastructure/    # Camada de infraestrutura (Repositories, Migrations)
└── UsersApi.Tests/    # Testes unitários
```

### Estrutura de Pastas

```
.
├── Core/
│   ├── Entity/              # Entidades do domínio
│   ├── Dtos/                # Data Transfer Objects
│   ├── Models/              # Models e interfaces
│   └── Repository/          # Interfaces de repositório
├── Infrastructure/
│   ├── Repository/          # Implementação dos repositórios
│   └── Migrations/          # Migrações do EF Core
├── UsersApi/
│   ├── Controllers/         # API Controllers
│   ├── Middlewares/         # Middlewares customizados
│   ├── Service/             # Serviços e validators
│   └── Configs/             # Configurações
├── UsersApi.Tests/          # Testes unitários
├── k8s/                     # Manifests Kubernetes
├── docker-compose.yaml      # Configuração Docker Compose
└── Dockerfile               # Dockerfile multi-stage
```

## 📦 Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) e Docker Compose
- [SQL Server 2022](https://www.microsoft.com/sql-server) (ou via Docker)
- [RabbitMQ](https://www.rabbitmq.com/) (opcional, para eventos)

## 🔧 Instalação e Execução

### Opção 1: Docker Compose (Recomendado)

1. Clone o repositório:
```bash
git clone https://github.com/capuchetagames/usersapi.git
cd usersapi
```

2. Inicie os serviços:
```bash
docker-compose up -d
```

3. A API estará disponível em: `http://localhost:5200`

4. Acesse a documentação:
   - Swagger UI: `http://localhost:5200/swagger`
   - ReDoc: `http://localhost:5200/api-docs`

### Opção 2: Execução Local

1. Configure a connection string no `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=Db.Users;User Id=sa;Password=SuaSenha;TrustServerCertificate=True;"
  }
}
```

2. Execute as migrações:
```bash
dotnet ef database update --project Infrastructure --startup-project UsersApi
```

3. Execute a aplicação:
```bash
dotnet run --project UsersApi
```

### Opção 3: Visual Studio / Rider

1. Abra o arquivo `UsersApi.sln`
2. Configure a connection string
3. Pressione F5 para executar

## 📡 Endpoints da API

### Autenticação

#### POST `/Auth`
Autentica um usuário e retorna um token JWT.

**Request Body:**
```json
{
  "name": "usuario",
  "password": "senha123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### POST `/Auth/validate`
Valida um token JWT (usado por outros microserviços).

**Request Body:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response:**
```json
{
  "isValid": true,
  "username": "usuario",
  "role": "User",
  "userId": 1,
  "tokenId": "guid-do-token"
}
```

### Usuários

#### POST `/new-user` (Público)
Registra um novo usuário na plataforma.

**Request Body:**
```json
{
  "name": "João Silva",
  "email": "joao@example.com",
  "password": "Senha@123"
}
```

**Response:** `201 Created`
```json
{
  "id": 1,
  "name": "João Silva",
  "email": "joao@example.com",
  "permission": "User"
}
```

#### GET `/User` 🔒 (Admin)
Lista todos os usuários cadastrados.

**Response:**
```json
[
  {
    "id": 1,
    "name": "João Silva",
    "email": "joao@example.com",
    "permission": "User"
  }
]
```

#### GET `/User/{id}` 🔒 (Admin)
Busca um usuário específico por ID.

**Response:**
```json
{
  "id": 1,
  "name": "João Silva",
  "email": "joao@example.com",
  "permission": "User"
}
```

#### POST `/User` 🔒 (Admin)
Cria um novo usuário com permissões específicas.

**Request Body:**
```json
{
  "name": "Maria Admin",
  "email": "maria@example.com",
  "password": "Senha@123",
  "permission": "Admin"
}
```

#### PUT `/User` 🔒 (Admin)
Atualiza um usuário existente.

**Request Body:**
```json
{
  "id": 1,
  "name": "João Silva Atualizado",
  "email": "joao.novo@example.com",
  "password": "NovaSenha@123",
  "permission": "User"
}
```

#### DELETE `/User/{id}` 🔒 (Admin)
Exclui um usuário.

**Response:** `204 No Content`

### Health Check

#### GET `/health`
Verifica o status da API.

**Response:** `200 OK`

## ⚙️ Configuração

### Variáveis de Ambiente

As principais configurações podem ser feitas via `appsettings.json` ou variáveis de ambiente:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=users-db;Database=Db.Users;..."
  },
  "Jwt": {
    "Key": "sua-chave-secreta-minimo-32-caracteres",
    "Issuer": "UsersApi"
  },
  "RabbitMq": {
    "Host": "rabbitmq",
    "User": "admin",
    "Password": "admin"
  }
}
```

### Configuração JWT

Para gerar uma chave JWT segura:
```bash
openssl rand -base64 32
```

### Kubernetes Environment Variables

Para deployment em Kubernetes, utilize as variáveis do ConfigMap/Secret:
- `DB_HOST` - Host do banco de dados
- `DB_NAME` - Nome do banco
- `DB_USER` - Usuário do banco
- `DB_PASSWORD` - Senha do banco

## 🚢 Deployment

### Docker

Build da imagem:
```bash
docker build -t usersapi:latest .
```

Executar container:
```bash
docker run -d -p 5200:8080 \
  -e ConnectionStrings__DefaultConnection="Server=..." \
  -e Jwt__Key="..." \
  usersapi:latest
```

### Kubernetes

Os manifestos Kubernetes estão disponíveis no diretório `k8s/`:

1. **Banco de Dados:**
```bash
kubectl apply -f k8s/sql-configmap.yaml
kubectl apply -f k8s/sql-deployment.yaml
kubectl apply -f k8s/sql-service.yaml
```

2. **API:**
```bash
kubectl apply -f k8s/users-secret.yaml
kubectl apply -f k8s/users-configmap.yaml
kubectl apply -f k8s/users-deployment.yaml
kubectl apply -f k8s/users-service.yaml
```

Ou use os scripts auxiliares:
```bash
# Deploy completo
./k8s/k8s-start-all-deploy.sh

# Desenvolvimento
./k8s/k8s-start-all-dev.sh

# Remover tudo
./k8s/k8s-delete-all.sh
```

## 🧪 Testes

Execute os testes unitários:

```bash
dotnet test
```

Execute com cobertura:
```bash
dotnet test /p:CollectCoverage=true
```

## 👥 Contribuindo

Contribuições são bem-vindas! Para contribuir:

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

### Convenções

- Siga os padrões de código do projeto
- Adicione testes para novas funcionalidades
- Atualize a documentação quando necessário
- Use mensagens de commit descritivas

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

## 📞 Contato

Capucheta Games - [@capuchetagames](https://github.com/capuchetagames)

Link do Projeto: [https://github.com/capuchetagames/usersapi](https://github.com/capuchetagames/usersapi)

---

Desenvolvido com ❤️ por [Capucheta Games](https://github.com/capuchetagames)
