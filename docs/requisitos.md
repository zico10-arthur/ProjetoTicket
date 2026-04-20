# Requisitos do Sistema — SoldOut Tickets

## Histórias de Usuário

- HU01: Como usuário, Quero cadastrar a minha conta, Para ter acesso ao sistema.
- HU02: Como usuário, Quero ter a possibilidade de trocar meu endereço de e-mail, Para ter a opção de mudar o meu e-mail.
- HU03: Como usuário, Quero ter a possibilidade de alterar o meu nome de cadastro, Para fins de mudança na minha identidade.
- HU04: Como usuário, Quero ter a possibilidade de remover minha conta, Para não ter mais meus dados no sistema.
- HU05: Como administrador, Quero ter a possibilidade de listar os usuários cadastrados no sistema, Para ter controle de todos.
- HU06: Como administrador, Quero ter a possibilidade de listar um usuário específico, Para saber suas informações.
- HU07: Como usuário, Quero ter o login, Para acessar minha conta.
- HU08: Como usuário, Quero poder trocar a minha senha, Para eventual mudança.
- HU09: Como usuário, Quero ter acesso a uma lista de eventos disponíveis, Para me manter informado sobre os próximos eventos.
- HU10: Como vendedor, Quero ter a possibilidade de cadastrar um novo evento, Para ter mais eventos no sistema.
- HU11: Como administrador, Quero ter a possibilidade de remover um evento, Para caso ele seja cancelado.
- HU12: Como administrador, Quero ter a possibilidade de alterar o nome do evento, Para caso o evento tenha mudado de nome.
- HU13: Como administrador, Quero ter a possibilidade de alterar a data do evento, Para caso o evento mude de data.
- HU14: Como administrador, Quero ter a possibilidade de alterar o preço do evento, Para poder fazer promoções ou liquidações.
- HU15: Como administrador, Quero ter a possibilidade de alterar a capacidade do evento, Para caso o evento tenha a capacidade alterada.
- HU16: Como usuário, Quero ter uma lista de cupons disponíveis para mim, Para poder usar em uma reserva.
- HU17: Como administrador, Quero ter a possibilidade de cadastrar um novo cupom, Para ter mais cupons no sistema.
- HU18: Como administrador, Quero ter a possibilidade de listar todos os cupons disponíveis, Para ter controle dos cupons.
- HU19: Como administrador, Quero ter a possibilidade de alterar o valor mínimo do cupom, Para o valor ser aumentado ou diminuído.
- HU20: Como administrador, Quero ter a possibilidade de alterar a porcentagem de desconto, Para alterar o desconto do cupom.
- HU21: Como administrador, Quero ter a possibilidade de remover um cupom do sistema, Para ele não ser mais utilizado.
- HU22: Como usuário, Quero ter o meu histórico de eventos que já participei, Para ter conhecimento de eventos passados, valor pago e etc.
- HU23: Como usuário, Quero ter a possibilidade de fazer uma reserva em um evento aplicando ou não um cupom, Para ter o desconto e participar do evento.
- HU24: Como administrador, Quero ter a possibilidade de listar todas as reservas por evento, Para ter o controle de reservas.
- HU25: Como usuário, Quero ter uma página inicial com a marca da empresa, Para saber qual funcionalidade vou usar.
- HU26: Como administrador, Quero criar um perfil, Para diferenciar os usuários do sistema.
- HU27: Como administrador, Quero ter a possibilidade de ativar ou desativar um cupom, Para ele ser ou não ser mais utilizado.
- HU28: Como administrador, Quero ter a possibilidade de alterar a data de expiração de um cupom, Para aumentar ou diminuir a sua validade.
- HU29: Como vendedor, Quero cadastrar ingressos, Para a venda dos mesmos em um evento.
- HU30: Como usuário, Quero poder fazer o pagamento dentro do sistema, Para questões de praticidade.
- HU31: Como usuário, Quero poder realizar a minha reserva dentro do sistema, Para reservar a vaga no evento.

## Critérios de Aceitação (BDD)

**HU01 — Cadastro de conta**
- Dado que o usuário acessa a página de cadastro
- Quando ele preencher CPF, nome, e-mail e senha válidos e confirmar
- Então o sistema deve criar a conta e redirecionar para o login

**HU02 — Trocar e-mail**
- Dado que o usuário está logado e acessa seu perfil
- Quando ele informar um novo e-mail válido e salvar
- Então o sistema deve atualizar o e-mail e exibir mensagem de sucesso

**HU03 — Alterar nome**
- Dado que o usuário está logado e acessa seu perfil
- Quando ele informar um novo nome válido e salvar
- Então o sistema deve atualizar o nome e exibir mensagem de sucesso

**HU04 — Remover conta**
- Dado que o usuário está logado e acessa seu perfil
- Quando ele clicar em remover conta e confirmar a ação
- Então o sistema deve excluir a conta e redirecionar para a página inicial

**HU05 — Listar usuários**
- Dado que o administrador está logado
- Quando ele acessar a página de usuários
- Então o sistema deve exibir a lista de todos os usuários cadastrados

**HU06 — Listar usuário específico**
- Dado que o administrador está logado
- Quando ele buscar um usuário pelo CPF
- Então o sistema deve exibir as informações do usuário encontrado

**HU07 — Login**
- Dado que o usuário possui uma conta cadastrada
- Quando ele informar e-mail e senha corretos e clicar em entrar
- Então o sistema deve autenticá-lo e redirecionar para a página inicial

**HU08 — Trocar senha**
- Dado que o usuário está logado e acessa seu perfil
- Quando ele informar uma nova senha válida e salvar
- Então o sistema deve atualizar a senha e exibir mensagem de sucesso

**HU09 — Listar eventos**
- Dado que o usuário acessa a página inicial
- Quando a página carregar
- Então o sistema deve exibir a lista de eventos disponíveis

**HU10 — Cadastrar evento**
- Dado que o vendedor está logado
- Quando ele preencher nome, capacidade, data e preço e confirmar
- Então o sistema deve criar o evento com os ingressos gerados automaticamente

**HU11 — Remover evento**
- Dado que o administrador está logado
- Quando ele clicar em excluir um evento e confirmar
- Então o sistema deve remover o evento e seus ingressos do sistema

**HU12 — Alterar nome do evento**
- Dado que o administrador está logado
- Quando ele editar o nome do evento e salvar
- Então o sistema deve atualizar o nome e exibir mensagem de sucesso

**HU13 — Alterar data do evento**
- Dado que o administrador está logado
- Quando ele editar a data do evento e salvar
- Então o sistema deve atualizar a data e exibir mensagem de sucesso

**HU14 — Alterar preço do evento**
- Dado que o administrador está logado
- Quando ele editar o preço do evento e salvar
- Então o sistema deve atualizar o preço e exibir mensagem de sucesso

**HU15 — Alterar capacidade do evento**
- Dado que o administrador está logado
- Quando ele editar a capacidade do evento e salvar
- Então o sistema deve atualizar a capacidade e exibir mensagem de sucesso

**HU16 — Listar cupons disponíveis**
- Dado que o usuário está logado
- Quando ele acessar a página de cupons
- Então o sistema deve exibir apenas os cupons ativos e não expirados

**HU17 — Cadastrar cupom**
- Dado que o administrador está logado
- Quando ele preencher código, desconto, valor mínimo e data de expiração e confirmar
- Então o sistema deve criar o cupom e exibir mensagem de sucesso

**HU18 — Listar todos os cupons**
- Dado que o administrador está logado
- Quando ele acessar a página de gestão de cupons
- Então o sistema deve exibir todos os cupons incluindo os inativos e expirados

**HU19 — Alterar valor mínimo do cupom**
- Dado que o administrador está logado
- Quando ele editar o valor mínimo de um cupom e salvar
- Então o sistema deve atualizar o valor mínimo e exibir mensagem de sucesso

**HU20 — Alterar porcentagem de desconto**
- Dado que o administrador está logado
- Quando ele editar a porcentagem de desconto de um cupom e salvar
- Então o sistema deve atualizar o desconto e exibir mensagem de sucesso

**HU21 — Remover cupom**
- Dado que o administrador está logado
- Quando ele clicar em remover um cupom e confirmar
- Então o sistema deve excluir o cupom do sistema

**HU22 — Histórico de reservas**
- Dado que o usuário está logado
- Quando ele acessar a página de minhas reservas
- Então o sistema deve exibir o histórico com evento, data, assento e valor pago

**HU23 — Fazer reserva com cupom**
- Dado que o usuário está logado e selecionou um ingresso
- Quando ele informar um cupom válido e confirmar a reserva
- Então o sistema deve aplicar o desconto e registrar a reserva com o valor final

**HU24 — Listar reservas por evento**
- Dado que o administrador está logado
- Quando ele acessar a página de reservas
- Então o sistema deve exibir todas as reservas agrupadas por evento

**HU25 — Página inicial**
- Dado que qualquer usuário acessa o sistema
- Quando a página carregar
- Então o sistema deve exibir a marca da empresa e as opções disponíveis para o perfil do usuário

**HU26 — Criar perfil**
- Dado que o sistema possui os perfis Admin, Vendedor e Comprador
- Quando um usuário é cadastrado
- Então o sistema deve associar o perfil correto e liberar apenas as funcionalidades correspondentes

**HU27 — Ativar ou desativar cupom**
- Dado que o administrador está logado
- Quando ele clicar em ativar ou desativar um cupom
- Então o sistema deve alternar o status do cupom e exibir mensagem de sucesso

**HU28 — Alterar data de expiração do cupom**
- Dado que o administrador está logado
- Quando ele editar a data de expiração de um cupom e salvar
- Então o sistema deve atualizar a data e exibir mensagem de sucesso

**HU29 — Cadastrar ingressos**
- Dado que o vendedor criou um evento
- Quando o evento for confirmado
- Então o sistema deve gerar automaticamente os ingressos com setores VIP e Geral

**HU30 — Pagamento**
- Dado que o usuário realizou uma reserva
- Quando ele acessar a página de pagamento e confirmar
- Então o sistema deve registrar o pagamento e marcar o ingresso como vendido

**HU31 — Realizar reserva**
- Dado que o usuário está logado e selecionou um assento disponível
- Quando ele confirmar a reserva
- Então o sistema deve bloquear o assento e registrar a reserva em seu nome

## Requisitos Funcionais

### Usuário
- RF01: O sistema deve permitir o cadastro de compradores
- RF02: O sistema deve permitir o cadastro de vendedores (apenas Admin)
- RF03: O sistema deve autenticar usuários via login com e-mail e senha
- RF04: O sistema deve gerar token JWT após login bem-sucedido
- RF05: O usuário deve poder alterar nome, e-mail e senha
- RF06: O usuário deve poder remover sua própria conta

### Evento
- RF07: O vendedor deve poder criar eventos com nome, capacidade, data e preço
- RF08: O sistema deve gerar ingressos automaticamente ao criar um evento (10% VIP, 90% Geral)
- RF09: O vendedor deve poder editar e excluir apenas seus próprios eventos
- RF10: O Admin deve poder excluir qualquer evento
- RF11: Qualquer usuário deve poder visualizar os eventos disponíveis

### Ingresso
- RF12: O sistema deve exibir os ingressos de um evento com status visual
- RF13: O comprador deve poder selecionar um assento disponível
- RF14: O sistema deve bloquear temporariamente o ingresso ao iniciar uma reserva

### Reserva
- RF15: O comprador deve poder reservar um ingresso
- RF16: O sistema deve impedir reserva duplicada do mesmo usuário no mesmo evento
- RF17: O sistema deve liberar automaticamente assentos com reserva expirada
- RF18: O comprador deve poder aplicar cupom de desconto na reserva

### Cupom
- RF19: O Admin deve poder criar, editar e desativar cupons
- RF20: O sistema deve validar o cupom (ativo, não expirado, valor mínimo)
- RF21: O sistema deve impedir que o desconto gere valor negativo

## Requisitos Não Funcionais

- RNF01: A API deve ser protegida contra SQL Injection (uso de Dapper com parâmetros)
- RNF02: Senhas devem ser armazenadas com hash (pendente implementação)
- RNF03: Autenticação via JWT com roles (Admin, Vendedor, Comprador)
- RNF04: O banco de dados deve ser versionado via migrations (DbUp)
- RNF05: Operações críticas devem usar transações no banco
