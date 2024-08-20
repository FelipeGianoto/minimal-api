MinimalApi
MinimalApi é uma API desenvolvida com .NET Core, utilizando o conceito de APIs mínimas, que proporciona uma estrutura simples e eficiente para criar e consumir endpoints RESTful. Este projeto foi criado com foco na simplicidade e eficiência, proporcionando um ponto de partida fácil para desenvolvedores que desejam implementar autenticação JWT, gerenciamento de entidades e integração com bancos de dados MySQL.

Funcionalidades
Autenticação JWT: Implementação de autenticação usando JSON Web Tokens (JWT) para proteger os endpoints da API.
Gerenciamento de Administradores: CRUD completo para a entidade Administrador, incluindo endpoints para login, criação, listagem, e exclusão de administradores.
Gerenciamento de Veículos: CRUD completo para a entidade Veículo, permitindo operações de criação, leitura, atualização e exclusão de registros.
Validação e Tratamento de Erros: Validação de dados de entrada e retorno de mensagens de erro apropriadas.
Documentação com Swagger: Integração com Swagger para facilitar a exploração e teste dos endpoints da API.
Banco de Dados MySQL: Uso de Entity Framework Core para integração com MySQL, com configuração automática da versão do servidor.
Tecnologias Utilizadas
.NET Core
Entity Framework Core
MySQL
JWT Bearer Authentication
Swagger/OpenAPI
AutoMapper (se aplicável)
Docker (se aplicável)
Estrutura do Projeto
MinimalApi.Dominio: Contém as entidades, DTOs, enums, interfaces e serviços principais do domínio.
MinimalApi.Infraestrutura: Configuração e integração com o banco de dados.
MinimalApi.Startup: Configuração inicial do projeto, incluindo serviços, middlewares e endpoints.
Como Executar
Clone o repositório:
bash
Copiar código
git clone https://github.com/seuusuario/MinimalApi.git
Configure a string de conexão com o MySQL no appsettings.json.
Execute as migrações do banco de dados:
bash
Copiar código
dotnet ef database update
Inicie o projeto:
bash
Copiar código
dotnet run
Acesse a documentação da API em: http://localhost:<porta>/swagger.
