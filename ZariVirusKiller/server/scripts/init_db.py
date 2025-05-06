import os
from app.app import db, LicenseKey

def init_db():
    db.create_all()
    print("Banco inicializado com sucesso.")

if __name__ == '__main__':
    init_db()
