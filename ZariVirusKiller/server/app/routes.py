from flask import Blueprint, request, jsonify, send_file, current_app
import os
import json
from app import db
from models import LicenseKey, DefinitionUpdate, VirusSignature
from signature_manager import SignatureManager
from flask_jwt_extended import jwt_required, get_jwt_identity

api = Blueprint('api', __name__)

@api.route('/verify-license', methods=['POST'])
def verify_license():
    """
    Verifies a license key
    """
    data = request.get_json()
    key = data.get('key')
    device_id = data.get('device_id')
    
    if not key:
        return jsonify({'error': 'License key is required'}), 400
    
    # Check if license exists and is valid
    license = LicenseKey.query.filter_by(key=key).first()
    if not license:
        return jsonify({'valid': False, 'message': 'Invalid license key'}), 200
    
    # Check if license has expired
    if license.expires_at < db.func.now():
        return jsonify({'valid': False, 'message': 'License has expired'}), 200
    
    # Update device ID if provided
    if device_id and not license.device_id:
        license.device_id = device_id
        db.session.commit()
    
    # Check if device ID matches
    if license.device_id and license.device_id != device_id:
        return jsonify({'valid': False, 'message': 'License is already in use on another device'}), 200
    
    return jsonify({
        'valid': True,
        'expires_at': license.expires_at.isoformat()
    }), 200

@api.route('/definitions', methods=['GET'])
def get_definitions():
    """
    Gets the latest virus definitions
    """
    try:
        # Get latest definitions info
        definitions_info = SignatureManager.get_latest_definitions()
        
        return jsonify(definitions_info), 200
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@api.route('/download-definitions/<type>', methods=['GET'])
def download_definitions(type):
    """
    Downloads the latest virus definitions file
    """
    try:
        if type not in ['hash', 'pattern']:
            return jsonify({'error': 'Invalid definition type'}), 400
            
        # Get latest definition update
        update = DefinitionUpdate.query.filter_by(update_type=type).order_by(DefinitionUpdate.id.desc()).first()
        
        if not update or not os.path.exists(update.path):
            return jsonify({'error': 'Definitions file not found'}), 404
        
        return send_file(update.path, as_attachment=True)
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@api.route('/check-file', methods=['POST'])
def check_file():
    """
    Checks if a file hash matches any known virus
    """
    data = request.get_json()
    file_hash = data.get('hash')
    
    if not file_hash:
        return jsonify({'error': 'File hash is required'}), 400
    
    # Check if hash matches any known virus
    signature = VirusSignature.query.filter_by(hash_value=file_hash).first()
    
    if signature:
        return jsonify({
            'is_infected': True,
            'threat_name': signature.name,
            'severity': signature.severity
        }), 200
    
    return jsonify({'is_infected': False}), 200

@api.route('/add-signature', methods=['POST'])
@jwt_required()
def add_signature():
    """
    Adds a new virus signature
    """
    data = request.get_json()
    signature_type = data.get('type')
    name = data.get('name')
    severity = data.get('severity', 'medium')
    description = data.get('description')
    
    if not signature_type or not name:
        return jsonify({'error': 'Signature type and name are required'}), 400
    
    if signature_type == 'hash':
        hash_value = data.get('hash')
        if not hash_value:
            return jsonify({'error': 'Hash value is required'}), 400
            
        success, message = SignatureManager.add_hash_signature(
            name=name,
            hash_value=hash_value,
            severity=severity,
            description=description
        )
    elif signature_type == 'pattern':
        patterns = data.get('patterns')
        logic = data.get('logic', 'all')
        
        if not patterns:
            return jsonify({'error': 'Patterns are required'}), 400
            
        success, message = SignatureManager.add_pattern_signature(
            name=name,
            patterns=patterns,
            logic=logic,
            severity=severity,
            description=description
        )
    else:
        return jsonify({'error': 'Invalid signature type'}), 400
    
    if success:
        return jsonify({'message': message}), 201
    else:
        return jsonify({'error': message}), 400

@api.route('/signatures', methods=['GET'])
@jwt_required()
def get_signatures():
    """
    Gets all virus signatures
    """
    try:
        # Get query parameters
        signature_type = request.args.get('type')
        
        # Build query
        query = VirusSignature.query
        
        if signature_type:
            query = query.filter_by(signature_type=signature_type)
        
        signatures = query.all()
        
        # Format response
        result = []
        for sig in signatures:
            signature_data = {
                'id': sig.id,
                'name': sig.name,
                'type': sig.signature_type,
                'severity': sig.severity,
                'created_at': sig.created_at.isoformat()
            }
            
            if sig.signature_type == 'hash':
                signature_data['hash_value'] = sig.hash_value
            elif sig.signature_type == 'pattern':
                signature_data['signature_id'] = sig.signature_id
                signature_data['pattern_data'] = json.loads(sig.pattern_data) if sig.pattern_data else None
            
            result.append(signature_data)
        
        return jsonify(result), 200
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@api.route('/statistics', methods=['GET'])
@jwt_required()
def get_statistics():
    """
    Gets system statistics
    """
    try:
        # Get license statistics
        total_licenses = LicenseKey.query.count()
        active_licenses = LicenseKey.query.filter(LicenseKey.expires_at > db.func.now()).count()
        used_licenses = LicenseKey.query.filter(LicenseKey.device_id != None).count()
        
        # Get signature statistics
        total_signatures = VirusSignature.query.count()
        hash_signatures = VirusSignature.query.filter_by(signature_type='hash').count()
        pattern_signatures = VirusSignature.query.filter_by(signature_type='pattern').count()
        
        # Get definition update statistics
        latest_update = DefinitionUpdate.query.order_by(DefinitionUpdate.id.desc()).first()
        
        return jsonify({
            'licenses': {
                'total': total_licenses,
                'active': active_licenses,
                'used': used_licenses
            },
            'signatures': {
                'total': total_signatures,
                'hash_based': hash_signatures,
                'pattern_based': pattern_signatures
            },
            'definitions': {
                'latest_update': latest_update.uploaded_at.isoformat() if latest_update else None,
                'version': latest_update.version if latest_update else None,
                'signature_count': latest_update.signature_count if latest_update else 0
            }
        }), 200
    except Exception as e:
        return jsonify({'error': str(e)}), 500