import os
import datetime
import hashlib
import json
from flask import Flask, request, jsonify, send_file, Response
from flask_sqlalchemy import SQLAlchemy
from flask_cors import CORS
from flask_limiter import Limiter
from flask_limiter.util import get_remote_address
from werkzeug.utils import secure_filename

app = Flask(__name__)
CORS(app)
Limiter(app, key_func=get_remote_address, default_limits=["200 per day", "50 per hour"])

# Database Configuration
app.config['SQLALCHEMY_DATABASE_URI'] = os.getenv('DATABASE_URL', 'postgresql://user:password@localhost:5432/zarivirus')
app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False

# File upload configuration
UPLOAD_FOLDER = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'uploads')
DEFINITIONS_FOLDER = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'definitions')
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER
app.config['DEFINITIONS_FOLDER'] = DEFINITIONS_FOLDER
app.config['MAX_CONTENT_LENGTH'] = 16 * 1024 * 1024  # 16MB max upload

# Ensure directories exist
os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs(DEFINITIONS_FOLDER, exist_ok=True)

db = SQLAlchemy(app)

# Models
class LicenseKey(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    key = db.Column(db.String(128), unique=True, nullable=False)
    created_at = db.Column(db.DateTime, server_default=db.func.now())
    expires_at = db.Column(db.DateTime, nullable=False)
    device_id = db.Column(db.String(64), nullable=True)

class DefinitionUpdate(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    version = db.Column(db.String(16), nullable=False)
    path = db.Column(db.String(256), nullable=False)
    uploaded_at = db.Column(db.DateTime, server_default=db.func.now())
    signature_count = db.Column(db.Integer, default=0)

class VirusSignature(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(128), nullable=False)
    hash_signature = db.Column(db.String(64), nullable=False)
    detection_pattern = db.Column(db.Text, nullable=True)
    severity = db.Column(db.String(16), default='medium')
    added_at = db.Column(db.DateTime, server_default=db.func.now())
    definition_id = db.Column(db.Integer, db.ForeignKey('definition_update.id'))

# License Management Endpoints
@app.route('/verify-key', methods=['POST'])
def verify_key():
    data = request.get_json()
    key = LicenseKey.query.filter_by(key=data.get('license_key')).first()
    if not key:
        return jsonify({'valid': False, 'reason': 'License key not found'}), 404
    
    # Check expiration
    if key.expires_at < datetime.datetime.now():
        return jsonify({'valid': False, 'reason': 'License key expired'}), 403
    
    # Mark device_id on first activation
    if not key.device_id:
        key.device_id = data.get('device_id')
        db.session.commit()
    elif key.device_id != data.get('device_id'):
        return jsonify({'valid': False, 'reason': 'License already activated on another device'}), 403
        
    return jsonify({'valid': True}), 200

# Definition Management Endpoints
@app.route('/definitions', methods=['GET'])
def get_definitions():
    latest_definition = DefinitionUpdate.query.order_by(DefinitionUpdate.uploaded_at.desc()).first()
    
    if not latest_definition:
        return jsonify({'error': 'No definitions available'}), 404
    
    return jsonify({
        'definitions_version': latest_definition.version,
        'url': f'/download-definitions/{latest_definition.version}',
        'signature_count': latest_definition.signature_count,
        'date': latest_definition.uploaded_at.isoformat()
    })

@app.route('/download-definitions/<version>', methods=['GET'])
def download_definitions(version):
    definition = DefinitionUpdate.query.filter_by(version=version).first()
    
    if not definition:
        return jsonify({'error': 'Definition version not found'}), 404
    
    # Check if file exists
    if not os.path.exists(definition.path):
        return jsonify({'error': 'Definition file not found'}), 404
    
    return send_file(definition.path, as_attachment=True)

@app.route('/upload-definitions', methods=['POST'])
def upload_definitions():
    # Admin authentication would be implemented here
    if 'file' not in request.files:
        return jsonify({'error': 'No file part'}), 400
    
    file = request.files['file']
    version = request.form.get('version')
    signature_count = request.form.get('signature_count', 0, type=int)
    
    if not version:
        return jsonify({'error': 'Version is required'}), 400
    
    if file.filename == '':
        return jsonify({'error': 'No selected file'}), 400
    
    filename = secure_filename(f'definitions_{version}.zip')
    file_path = os.path.join(app.config['DEFINITIONS_FOLDER'], filename)
    file.save(file_path)
    
    # Create new definition record
    new_definition = DefinitionUpdate(
        version=version,
        path=file_path,
        signature_count=signature_count
    )
    
    db.session.add(new_definition)
    db.session.commit()
    
    return jsonify({'success': True, 'version': version}), 201

# Virus Scanning Endpoints
@app.route('/scan-file', methods=['POST'])
def scan_file():
    if 'file' not in request.files:
        return jsonify({'error': 'No file part'}), 400
    
    file = request.files['file']
    
    if file.filename == '':
        return jsonify({'error': 'No selected file'}), 400
    
    # Save file temporarily
    filename = secure_filename(file.filename)
    file_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
    file.save(file_path)
    
    try:
        # Calculate file hash
        file_hash = calculate_file_hash(file_path)
        
        # Check against virus signatures
        signatures = VirusSignature.query.all()
        detected_threats = []
        
        for signature in signatures:
            if signature.hash_signature == file_hash:
                detected_threats.append({
                    'name': signature.name,
                    'severity': signature.severity
                })
        
        # Clean up temporary file
        os.remove(file_path)
        
        return jsonify({
            'filename': filename,
            'threats_detected': len(detected_threats),
            'threats': detected_threats,
            'scan_date': datetime.datetime.now().isoformat()
        })
    except Exception as e:
        # Clean up on error
        if os.path.exists(file_path):
            os.remove(file_path)
        return jsonify({'error': str(e)}), 500

@app.route('/add-signature', methods=['POST'])
def add_signature():
    # Admin authentication would be implemented here
    data = request.get_json()
    
    required_fields = ['name', 'hash_signature', 'severity']
    for field in required_fields:
        if field not in data:
            return jsonify({'error': f'Missing required field: {field}'}), 400
    
    # Get latest definition
    latest_definition = DefinitionUpdate.query.order_by(DefinitionUpdate.uploaded_at.desc()).first()
    if not latest_definition:
        return jsonify({'error': 'No definition available to add signatures to'}), 404
    
    new_signature = VirusSignature(
        name=data['name'],
        hash_signature=data['hash_signature'],
        detection_pattern=data.get('detection_pattern'),
        severity=data['severity'],
        definition_id=latest_definition.id
    )
    
    db.session.add(new_signature)
    
    # Update signature count
    latest_definition.signature_count += 1
    
    db.session.commit()
    
    return jsonify({'success': True, 'id': new_signature.id}), 201

# Utility Functions
def calculate_file_hash(file_path):
    """Calculate SHA-256 hash of a file"""
    sha256_hash = hashlib.sha256()
    with open(file_path, "rb") as f:
        for byte_block in iter(lambda: f.read(4096), b""):
            sha256_hash.update(byte_block)
    return sha256_hash.hexdigest()

# Health check endpoint
@app.route('/health', methods=['GET'])
def health_check():
    return jsonify({'status': 'healthy'}), 200

# Error handlers
@app.errorhandler(404)
def not_found(error):
    return jsonify({'error': 'Not found'}), 404

@app.errorhandler(500)
def server_error(error):
    return jsonify({'error': 'Internal server error'}), 500

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=5000)
