# ZariVirusKiller

Antivírus proprietário leve inspirado no ClamAV com interface moderna e cores pastel.
Lightweight proprietary antivirus inspired by ClamAV with modern interface and pastel colors.

## Visão Geral / Overview

O ZariVirusKiller é uma solução completa de antivírus com arquitetura cliente-servidor:
ZariVirusKiller is a complete antivirus solution with a client-server architecture:

- **Cliente/Client**: Aplicação Windows (.NET 4.8 + WinForms) com UI personalizada / Windows application with custom UI
- **Servidor/Server**: API Flask para gerenciamento de licenças e distribuição de definições / Flask API for license management and definition distribution
- **Dashboard**: Interface web em TypeScript/Tailwind para administração / Web interface for administration

## Estrutura do Projeto

```
ZariVirusKiller/
├── client/                  # Aplicação C# (.NET 4.8 + WinForms)
│   ├── src/                 # Código-fonte principal
│   ├── ui/                  # Componentes de UI personalizados
│   ├── engine/              # Motor de escaneamento
│   ├── key_verification/    # Validação de licenças
│   ├── updates/             # Atualizações de definições
│   ├── plugins/             # Extensibilidade futura
│   ├── data/                # Recursos e traduções
│   ├── build.bat            # Script de compilação
│   └── README.md            # Documentação do cliente
├── server/                  # API Flask + Dashboard TS
│   ├── app/                 # Aplicação Flask
│   ├── web/                 # Dashboard de administração
│   ├── db/                  # Schemas PostgreSQL
│   ├── scripts/             # Utilitários de servidor
│   ├── Dockerfile           # Configuração Docker
│   └── README.md            # Documentação do servidor
├── docs/                    # Documentação técnica
└── README.md                # Este arquivo
```

## Funcionalidades Implementadas / Implemented Features

### Cliente / Client

- Interface gráfica personalizada com cantos arredondados e tema pastel / Custom UI with rounded corners and pastel theme
- Dashboard principal com status de proteção e ações rápidas / Main dashboard with protection status and quick actions
- Integração com a bandeja do sistema (tray icon) / System tray integration
- Sistema de licenciamento com validação online / License system with online validation
- Motor de escaneamento com detecção por hash e verificação no servidor / Scanning engine with hash detection and server verification
- Proteção em tempo real / Real-time protection
- Gerenciador de atualizações para definições de vírus / Update manager for virus definitions
- Suporte a múltiplos idiomas (Português e Inglês) / Multi-language support (Portuguese and English)

### Servidor / Server

- API RESTful para validação de licenças / RESTful API for license validation
- Endpoint para distribuição de definições de vírus / Endpoint for virus definition distribution
- API para verificação de arquivos / API for file scanning
- Gerenciamento de assinaturas de vírus / Virus signature management
- Dashboard administrativo com estatísticas e gerenciamento / Admin dashboard with statistics and management
- Banco de dados para armazenamento de licenças e definições / Database for storing licenses and definitions

## Como Executar / How to Run

### Cliente / Client

1. Abra `client/ZariVirusKiller.sln` no Visual Studio 2019+ / Open `client/ZariVirusKiller.sln` in Visual Studio 2019+
2. Restaure os pacotes NuGet / Restore NuGet packages
3. Compile o projeto em modo Debug ou Release / Build the project in Debug or Release mode
4. Execute o aplicativo / Run the application

Alternativamente, use o script de compilação / Alternatively, use the build script:
```
cd client
build.bat
```

### Servidor / Server

1. Crie e ative um ambiente virtual Python / Create and activate a Python virtual environment:
   ```
   python -m venv venv
   .\venv\Scripts\activate
   ```

2. Instale as dependências / Install dependencies:
   ```
   pip install -r server/requirements.txt
   ```

3. Inicialize o banco de dados / Initialize the database:
   ```
   python server/scripts/init_db.py
   ```

4. Inicie o servidor Flask / Start the Flask server:
   ```
   cd server/app
   flask run --host=0.0.0.0 --port=5000
   ```

5. Acesse o dashboard administrativo em / Access the admin dashboard at `http://localhost:5000/admin`

Alternativamente, use Docker / Alternatively, use Docker:
```
cd server
docker build -t zarivirus-server .
docker run -p 5000:5000 zarivirus-server
```

## Uso / Usage

### Cliente / Client

1. Inicie o aplicativo ZariVirusKiller / Launch the ZariVirusKiller application
2. Ative sua licença usando a chave de licença fornecida / Activate your license using the provided license key
3. Use o painel para realizar verificações, atualizar definições de vírus e configurar ajustes / Use the dashboard to perform scans, update virus definitions, and configure settings

### Administração do Servidor / Server Administration

1. Acesse o painel de administração em / Access the admin dashboard at `http://localhost:5000/admin`
2. Gerencie licenças, definições de vírus e visualize estatísticas / Manage licenses, virus definitions, and view statistics

## Suporte / Support

Para suporte, entre em contato com / For support, please contact: support@zarivirus.com
