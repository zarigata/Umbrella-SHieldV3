from flask_sqlalchemy import SQLAlchemy
from datetime import datetime

db = SQLAlchemy()

class LicenseKey(db.Model):
    """Model for license keys"""
    id = db.Column(db.Integer, primary_key=True)
    key = db.Column(db.String(128), unique=True, nullable=False)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    expires_at = db.Column(db.DateTime, nullable=False)
    device_id = db.Column(db.String(64), nullable=True)
    
    def __repr__(self):
        return f'<LicenseKey {self.key}>'

class DefinitionUpdate(db.Model):
    """Model for virus definition updates"""
    id = db.Column(db.Integer, primary_key=True)
    version = db.Column(db.String(16), nullable=False)
    path = db.Column(db.String(256), nullable=False)
    uploaded_at = db.Column(db.DateTime, default=datetime.utcnow)
    signature_count = db.Column(db.Integer, default=0)
    update_type = db.Column(db.String(16), default='hash')  # 'hash' or 'pattern'
    
    def __repr__(self):
        return f'<DefinitionUpdate {self.version}>'

class VirusSignature(db.Model):
    """Model for virus signatures"""
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(128), nullable=False)
    signature_type = db.Column(db.String(16), default='hash')  # 'hash' or 'pattern'
    hash_value = db.Column(db.String(64), nullable=True)  # For hash-based signatures
    signature_id = db.Column(db.String(16), nullable=True)  # For pattern-based signatures
    pattern_data = db.Column(db.Text, nullable=True)  # JSON string for pattern-based signatures
    severity = db.Column(db.String(16), default='medium')
    description = db.Column(db.Text, nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    
    def __repr__(self):
        return f'<VirusSignature {self.name}>'

class User(db.Model):
    """Model for admin users"""
    id = db.Column(db.Integer, primary_key=True)
    username = db.Column(db.String(64), unique=True, nullable=False)
    password_hash = db.Column(db.String(128), nullable=False)
    is_admin = db.Column(db.Boolean, default=False)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    
    def __repr__(self):
        return f'<User {self.username}>'