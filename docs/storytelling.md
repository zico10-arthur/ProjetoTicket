# Storytelling — SoldOut Tickets (v2.0)

## Índice

1. [Novas Funcionalidades](#1-novas-funcionalidades)
2. [Melhorias de Funcionalidades Existentes](#2-melhorias-de-funcionalidades-existentes)
3. [Resumo das Mudanças](#3-resumo-das-mudanças)

---

## 1. Novas Funcionalidades

### ST-01: Auto Cadastro de Vendedor

**Como** uma empresa que deseja vender ingressos para eventos de pequeno porte,
**Quero** me cadastrar diretamente no site, sem depender de um administrador,
**Para** começar a criar e vender eventos imediatamente.

**Critérios de Aceitação:**
- Dado que acesso a página inicial, Quando clico em "Quero Vender", Então sou direcionado para a página de cadastro de vendedor
- Dado que estou na página de cadastro, Quando preencho CNPJ, Razão Social, Nome Fantasia, Email, Senha e Telefone válidos, E clico em "Cadastrar", Então minha conta é criada com o perfil Vendedor (B2B2...) e Plano Gratuito
- Dado que o CNPJ já está cadastrado, Quando tento me cadastrar, Então recebo uma mensagem de erro "CNPJ já cadastrado"
- Dado que me cadastrei com sucesso, Quando faço login, Então recebo um JWT com role=Vendedor e sou redirecionado ao Painel do Vendedor

> ⚠️ **Admin continua sendo cadastrado manualmente no banco (SQL seed).** Apenas Vendedor e Comprador têm páginas de cadastro público.

---

### ST-02: Painel do Vendedor

**Como** um vendedor logado,
**Quero** um painel com todas as funcionalidades de gestão dos meus eventos,
**Para** gerenciar minhas vendas de forma centralizada.

**Critérios de Aceitação:**
- Dado que estou logado como Vendedor, Quando acesso o painel, Então vejo: Criar Evento, Meus Eventos, Gerenciar Cupons, Relatórios e Configurações
- Dado que estou nos Meus Eventos, Quando vejo a lista, Então apenas eventos com meu VendedorId aparecem
- Dado que acesso Configurações, Quando edito minha logo, descrição e site, Então os dados são salvos e exibidos nos meus eventos

---

### ST-03: Eventos de Pequeno Porte — Foco em Palestras

**Como** um vendedor,
**Quero** criar eventos do tipo Palestra (sem assentos numerados, apenas controle de vagas),
**Para** vender ingressos para workshops, cursos e meetups de forma simples.

**Critérios de Aceitação:**
- Dado que estou criando um evento, Quando escolho o tipo "Palestra", Então o sistema NÃO gera ingressos individuais — apenas um contador de vagas
- Dado que o evento é do tipo Palestra, Quando um comprador faz uma reserva, Então a reserva tem IngressoId = null e Quantidade informada
- Dado que há X vagas reservadas, Quando consulto vagas disponíveis, Então o sistema calcula: CapacidadeTotal - SUM(Quantidade reservada)
- Dado que as vagas chegam a 0, Quando um comprador tenta reservar, Então recebe "Evento esgotado"

---

### ST-04: Reserva com Múltiplos Participantes (ItemReserva)

**Como** um comprador,
**Quero** comprar ingressos para mim e para outras pessoas em uma única reserva,
**Para** não precisar fazer múltiplas compras para o mesmo evento.

**Critérios de Aceitação:**
- Dado que estou na tela de compra de uma Palestra, Quando adiciono itens à reserva, Então posso adicionar de 1 a 4 CPFs de participantes
- Dado que adiciono CPFs de participantes, Quando finalizo a compra, Então cada CPF gera um ItemReserva vinculado à minha Reserva
- Dado que os CPFs informados não estão cadastrados no sistema, Quando finalizo a compra, Então a reserva é criada normalmente (CPFs são apenas participantes, não precisam de conta)
- Dado que já adicionei 4 itens, Quando tento adicionar mais um, Então o sistema bloqueia "Limite máximo de 4 participantes por reserva"
- Dado que o valor total da reserva é R$200 (4 itens × R$50), Quando aplico um cupom de 10%, Então o ValorFinalPago é R$180

---

### ST-05: Comprador Cancela Reserva com Reembolso

**Como** um comprador (ou Admin, ou Vendedor),
**Quero** cancelar minha reserva antes do evento começar e receber reembolso,
**Para** não perder dinheiro se não puder mais comparecer.

**Critérios de Aceitação:**
- Dado que tenho uma reserva em um evento que ainda não começou, Quando clico em "Cancelar Reserva", Então o sistema pergunta confirmação
- Dado que confirmo o cancelamento, Quando o evento é pago, Então minha reserva é marcada como Reembolsada = true, os ingressos voltam ao status Livre (Status=0), e recebo notificação de reembolso
- Dado que confirmo o cancelamento, Quando o evento é gratuito, Então a reserva é cancelada sem reembolso (não houve cobrança)
- Dado que o evento já começou (DataEvento <= agora), Quando tento cancelar, Então o sistema bloqueia: "Não é possível cancelar. O evento já começou."
- Dado que minha reserva tem 4 itens (4 CPFs), Quando cancelo, Então TODOS os itens são marcados como Reembolsado = true e as vagas são liberadas

---

### ST-06: Vendedor Cancela Evento com Reembolso Obrigatório

**Como** um vendedor,
**Quero** cancelar um evento que não será mais realizado,
**Para** liberar a grade de eventos e reembolsar os compradores quando necessário.

**Critérios de Aceitação:**
- Dado que acesso Meus Eventos, Quando clico em "Cancelar Evento" em um evento pago com ingressos vendidos, Então o sistema alerta: "X ingressos vendidos. O cancelamento exigirá reembolso. Deseja continuar?"
- Dado que confirmo o cancelamento, Quando o evento é pago, Então: Evento.Cancelado = true, Ingressos Status=3 (Reembolsado), Reservas Reembolsada = true, compradores notificados
- Dado que o evento é gratuito, Quando cancelo, Então é cancelado sem reembolso (não houve cobrança)
- Dado que o evento não tem nenhum ingresso vendido, Quando cancelo, Então é cancelado diretamente

---

### ST-07: Admin e Vendedor Podem Fazer Reservas

**Como** um Admin ou Vendedor,
**Quero** poder comprar ingressos para eventos usando meu próprio perfil,
**Para** participar de eventos como qualquer outro comprador.

**Critérios de Aceitação:**
- Dado que estou logado como Admin, Quando acesso a Home, Então vejo a lista de eventos e posso fazer reservas
- Dado que estou logado como Vendedor, Quando acesso a Home, Então vejo a lista de eventos e posso fazer reservas
- Dado que faço uma reserva como Admin/Vendedor, Quando acesso Minhas Reservas, Então vejo o histórico com minhas reservas

---

## 2. Melhorias de Funcionalidades Existentes

### ST-08: Login Unificado — Três Perfis, Um Endpoint

**Como** qualquer usuário (Admin, Vendedor ou Comprador),
**Quero** fazer login no mesmo endpoint,
**Para** ter uma experiência de autenticação simplificada.

**Critérios de Aceitação (MELHORADO):**
- Dado que tenho uma conta de Admin, Quando faço login em `/api/usuario/login`, Então recebo JWT com role=Admin
- Dado que tenho uma conta de Vendedor, Quando faço login em `/api/usuario/login`, Então recebo JWT com role=Vendedor
- Dado que tenho uma conta de Comprador, Quando faço login em `/api/usuario/login`, Então recebo JWT com role=Comprador

> **Antes:** Empresa tinha endpoint separado (`/api/empresa/login`). **Agora:** Vendedor é perfil na tabela Usuarios, mesmo endpoint para todos.

---

### ST-09: Vendedor como Perfil na Tabela Usuarios

**Como** arquiteto do sistema,
**Quero** que Vendedor seja um perfil na tabela Usuarios (com propriedades específicas),
**Para** simplificar a arquitetura eliminando a tabela Empresas separada.

**Critérios de Aceitação (MELHORADO):**
- Dado que um Vendedor se cadastra, Quando seus dados são salvos, Então ficam na tabela Usuarios com PerfilId = B2B2...
- Dado que o Vendedor tem CNPJ, NomeFantasia, LogoUrl, Descricao, Site, Plano, Telefone, Quando esses campos são preenchidos, Então ficam como colunas na tabela Usuarios (nullable para Admin/Comprador)
- Dado que o Vendedor faz login, Quando o JWT é gerado, Então contém role=Vendedor (não role=Empresa)

> **Antes:** Empresa era entidade separada com tabela `Empresas`, endpoint `/api/empresa/login`, JWT role=Empresa. **Agora:** Tudo unificado em `Usuarios`.

---

### ST-10: Perfis Simplificados — Admin, Vendedor, Comprador

**Como** administrador do sistema,
**Quero** ter apenas três perfis bem definidos (Admin, Vendedor, Comprador),
**Para** simplificar o controle de acesso.

**Critérios de Aceitação (MELHORADO):**
- Dado que o sistema está configurado, Quando consulto a tabela Perfis, Então existem exatamente 3 registros: Admin (A1A1...), Vendedor (B2B2...), Comprador (C3C3...)
- Dado que um novo usuário se cadastra como Comprador, Quando a conta é criada, Então PerfilId = C3C3...
- Dado que um novo vendedor faz auto cadastro, Quando a conta é criada, Então PerfilId = B2B2...
- ⚠️ Dado que o sistema precisa de um Admin, Quando o Admin é criado, Então é inserido manualmente no banco via SQL seed (não há página de cadastro para Admin)

> **Antes:** Perfis Admin, Vendedor e Comprador com Vendedor cadastrado pelo Admin. **Agora:** Mesmos 3 perfis, mas Vendedor faz auto cadastro. Admin SEMPRE via SQL seed.

---

### ST-11: Evento com Tipo (Teatro/Palestra) e Gratuito/Pago

**Como** um vendedor,
**Quero** definir se meu evento é do tipo Teatro ou Palestra e se é gratuito ou pago,
**Para** configurar o evento conforme sua natureza.

**Critérios de Aceitação (MELHORADO):**
- Dado que crio um evento, Quando escolho o Tipo, Então: Teatro gera assentos numerados (VIP/Geral); Palestra usa controle de vagas
- Dado que defino PrecoPadrao = 0, Quando um comprador faz a reserva, Então o sistema pula a etapa de pagamento e confirma direto
- Dado que o evento é gratuito, Quando um comprador tenta aplicar cupom, Então o sistema rejeita: "Cupom não aplicável em evento gratuito"

> **Antes:** Todos os eventos geravam ingressos (Teatro), sem suporte a gratuito. **Agora:** Suporte a TipoEvento (Teatro/Palestra) e PrecoPadrao = 0 (gratuito).

---

### ST-12: Cancelamento de Reserva — Visão Unificada

**Como** qualquer usuário logado (Comprador, Admin ou Vendedor),
**Quero** acessar Minhas Reservas e cancelar qualquer reserva minha,
**Para** ter controle total sobre minhas compras.

**Critérios de Aceitação (MELHORADO):**
- Dado que estou em Minhas Reservas, Quando vejo a lista, Então aparecem apenas reservas onde UsuarioCpf = meu CPF
- Dado que tenho uma reserva com múltiplos itens, Quando cancelo, Então todos os itens são cancelados e as vagas liberadas
- Dado que cancelo um evento pago, Quando o reembolso é processado, Então cada ItemReserva tem Reembolsado = true

> **Antes:** Apenas Comprador podia fazer/cancelar reservas. **Agora:** Admin e Vendedor também podem, com as mesmas regras.

---

## 3. Resumo das Mudanças

| # | História | Tipo | O que mudou |
|---|----------|------|-------------|
| ST-01 | Auto Cadastro de Vendedor | **NOVA** | Vendedor se cadastra sozinho (antes: Admin cadastrava) |
| ST-02 | Painel do Vendedor | **NOVA** | Dashboard centralizado para Vendedor gerenciar eventos |
| ST-03 | Eventos de Pequeno Porte | **NOVA** | Foco em Palestras (workshops, cursos) sem assentos fixos |
| ST-04 | Reserva Multi-Participante | **NOVA** | Entidade ItemReserva, até 4 CPFs por reserva |
| ST-05 | Cancelamento com Reembolso | **NOVA** | Usuário cancela reserva antes do evento começar |
| ST-06 | Cancelamento de Evento | **NOVA** | Vendedor cancela evento com reembolso obrigatório |
| ST-07 | Admin/Vendedor Faz Reserva | **NOVA** | Todos os perfis podem comprar ingressos |
| ST-08 | Login Unificado | **MELHORIA** | Um endpoint de login para todos os 3 perfis |
| ST-09 | Vendedor como Perfil | **MELHORIA** | Vendedor na tabela Usuarios (não mais Empresas separada) |
| ST-10 | Perfis Simplificados | **MELHORIA** | 3 perfis: Admin, Vendedor, Comprador |
| ST-11 | Tipo de Evento + Gratuito | **MELHORIA** | Teatro/Palestra e PrecoPadrao = 0 |
| ST-12 | Cancelamento Unificado | **MELHORIA** | Qualquer perfil pode cancelar sua reserva |

---

> **Documento v1.0** — Storytelling do SoldOut Tickets. Baseado no [`especificacoes.md`](./especificacoes.md).
>
> ⚠️ **Admin SEMPRE cadastrado manualmente no banco (SQL seed).** Não há página de cadastro para Admin.
> Apenas Vendedor e Comprador se cadastram pelo site.