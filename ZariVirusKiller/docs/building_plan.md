# Plano de Construção Detalhado

Este documento reúne o passo a passo e a visão geral do projeto **ZariVirusKiller**, garantindo que não percamos o foco.

---

## 1. Visão Geral

- Antivírus Windows leve (.NET 4.8 + WinForms) com UI XP-style.
- Server Flask + TS/Tailwind para gerenciamento de chaves.
- Arquitetura modular (plugins, dependency injection).
- UI em Português (Brasil) via arquivos JSON de tradução.

## 2. Estrutura do Projeto

```text
ZariVirusKiller/
├── client/                  # Aplicação C# (.NET 4.8 + WinForms)
│   ├── src/
│   ├── ui/
│   ├── plugins/
│   ├── data/                # SQLite, traduções
│   ├── build.bat            # Script de build cliente
│   └── README.md            # Guia de configuração cliente
├── server/                  # API Flask + Dashboard TS
│   ├── app/
│   ├── web/
│   ├── db/                  # Schemas PostgreSQL
│   ├── scripts/             # Geração de chaves, migrações
│   ├── docker-compose.yml
│   └── README.md            # Guia de configuração servidor
├── docs/
│   └── building_plan.md     # Este arquivo
├── .gitignore
└── README.md                # Visão geral e link para docs/
```

## 3. Passo a Passo Detalhado

### 3.1 Fase 1: Scaffold Inicial

1. Criar diretórios e arquivos base:
   - `client/`, `server/`, `docs/`
   - `.gitignore`, `README.md`
2. Configurar repositório Git.
3. Inicializar `.gitignore` p/ Python, .NET, Node, SQLite.
4. Adicionar README principal resumido.

### 3.2 Fase 2: Configuração do Cliente

1. Abrir `client/`:
   - Criar solução `ZariVirusKiller.sln` (via Visual Studio).
   - Criar projeto WinForms C# targeting .NET Framework 4.8.
2. Adicionar referência a `System.Data.SQLite` e pacote AES via NuGet.
3. Criar estrutura de pastas: `engine/`, `key_verification/`, `updates/`.
4. Gerar arquivo `translations.json` em `data/`.
5. Implementar template de `build.bat` para compilar e gerar instalador.

### 3.2.1 Implementação de Funcionalidades Básicas

1. Implementar funcionalidades básicas de antivírus.

### 3.2.2 Distribuição Standalone e Atualizações Automáticas

1. Gerar um instalador EXE standalone (NSIS ou WiX) que inclua todas as dependências .NET e SQLite.
2. Registrar o aplicativo como serviço Windows para proteção em tempo real e updates silenciosos.
3. Implementar mecanismo de atualização automática de definições via Windows Update Agent ou background task sem intervenção do usuário.
4. Inscrever File System Filter Driver no kernel para escanear em níveis de subsistema antes de escrita/leitura de arquivos.

### 3.2.3 Integração Profunda com o Subsistema Windows

1. Desenvolver um File System Filter Driver usando Windows Driver Kit (WDK) para interceptar operações de arquivo.
2. Hook de APIs críticas (CreateProcess, WriteFile) no user-space via Microsoft Detours para heurística em processos.
3. Garantir assinatura de driver e loader bootstrap para evitar bypass.

### 3.3 Fase 3: Configuração do Servidor

1. Em `server/`, criar virtualenv Python 3.x:
   ```powershell
   python -m venv venv
   .\venv\Scripts\Activate.ps1
   pip install Flask psycopg2-binary
   pip freeze > requirements.txt
   ```
2. Criar `app/app.py` com blueprint para `/verify-key` e `/definitions`.
3. Configurar conexão PostgreSQL em `app/config.py`.
4. Criar scripts de migração SQL em `scripts/`.
5. Definir `docker-compose.yml` para web, db e migrations.
6. Em `web/`, inicializar projeto TS com Tailwind.

### 3.3.1 Interface Avançada de Gerenciamento de Chaves

1. Dashboard web para geração de chaves únicas e em lote (batch) via UI com parâmetros (expiração, limite dispositivos).
2. Botão “Gerar lote” que cria N chaves com prefixo personalizável e exporta relatório CSV.
3. Endpoint `/issue-keys` na API Flask para geração de chaves via scripts ou UI.
4. Fluxo de certificação automática: ao primeiro contato do cliente, chave é marcada como "ativada" e vinculada a UUID do dispositivo.
5. Logs de certificação e alertas em tempo real no dashboard.

### 3.4 Fase 4: Implementação do Core

1. **Engine de Scan** (`engine/`):
   - Leitura de assinaturas `.ndb`.
   - Rotina de heurística básica.
2. **Verificação de Chave** (`key_verification/`):
   - AES-256 para transmissão.
   - Requisição HTTPS ao `/verify-key`.
3. **Atualização de Definições** (`updates/`):
   - Download seguro via HTTPS.
4. **Quarentena**:
   - Movimentação de arquivos infectados para pasta segura.

### 3.5 Fase 5: UI e Temas

1. Designer WinForms custom:
   - Janelas com cantos arredondados.
   - Botões em pastel (#A8D5BA, #FFD1DC).
2. Dashboard principal:
   - Status do scan, atualizações, ações rápidas.
3. Ícone na tray e notificações.
4. Carregar traduções do JSON.

### 3.6 Fase 6: Testes e CI

1. **Unit tests**:
   - `engine.Tests`, `key_verification.Tests`.
2. **Integration tests** cliente ↔ servidor.
3. Configurar GitHub Actions:
   - Build cliente, rodar testes.
   - Build Docker do servidor, rodar testes.

### 3.7 Fase 7: Build e Deploy

1. **Cliente**:
   - `build.bat` gera instalador (.msi).
2. **Servidor**:
   - `docker-compose up -d`
3. Documentar procedimentos finais em ambos READMEs.

---

Concluída a Fase 1. Avançar para a Fase 2 (Configuração do Cliente).
