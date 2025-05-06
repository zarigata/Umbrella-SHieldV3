# SecureGuard KeyServer

Serviço Flask para gerenciamento de chaves e distribuições de definições.

## Setup

1. Crie e ative virtualenv Python 3.x:
   ```powershell
   python -m venv venv
   .\venv\Scripts\Activate.ps1
   ```
2. Instale dependências:
   ```powershell
   pip install -r requirements.txt
   ```
3. Inicialize o banco de dados:
   ```powershell
   python scripts/init_db.py
   ```
4. Inicie o servidor:
   ```powershell
   flask run --host=0.0.0.0 --port=5000
   ```

## Endpoints

- POST `/verify-key`  {"license_key","device_id"}
- GET  `/definitions`
- POST `/issue-keys` {"count", "prefix", "expiration_days"}
