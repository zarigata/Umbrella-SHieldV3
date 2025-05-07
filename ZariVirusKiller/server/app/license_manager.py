from flask import Blueprint, request, jsonify
from flask_jwt_extended import jwt_required, get_jwt_identity
from models import db, LicenseKey
from datetime import datetime, timedelta
import uuid
import string
import random

license_bp = Blueprint('license', __name__)

@license_bp.route('/licenses', methods=['GET'])
@jwt_required()
def get_licenses():
    """Get all license keys"""
    try:
        # Get query parameters
        status = request.args.get('status')  # 'active', 'expired', 'unused'
        
        # Build query
        query = LicenseKey.query
        
        if status == 'active':
            query = query.filter(LicenseKey.expires_at > datetime.utcnow())
        elif status == 'expired':
            query = query.filter(LicenseKey.expires_at <= datetime.utcnow())
        elif status == 'unused':
            query = query.filter(LicenseKey.device_id == None)
        
        licenses = query.all()
        
        # Format response
        result = []
        for license in licenses:
            result.append({
                'id': license.id,
                'key': license.key,
                'created_at': license.created_at.isoformat(),
                'expires_at': license.expires_at.isoformat(),
                'device_id': license.device_id,
                'is_active': license.expires_at > datetime.utcnow(),
                'is_used': license.device_id is not None
            })
        
        return jsonify(result), 200
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@license_bp.route('/licenses', methods=['POST'])
@jwt_required()
def create_license():
    """Create a new license key"""
    try:
        data = request.get_json()
        duration_days = data.get('duration_days', 365)  # Default to 1 year
        
        # Generate a unique license key
        key = generate_license_key()
        
        # Create expiration date
        expires_at = datetime.utcnow() + timedelta(days=duration_days)
        
        # Create new license
        license = LicenseKey(
            key=key,
            expires_at=expires_at
        )
        
        db.session.add(license)
        db.session.commit()
        
        return jsonify({
            'id': license.id,
            'key': license.key,
            'created_at': license.created_at.isoformat(),
            'expires_at': license.expires_at.isoformat(),
            'duration_days': duration_days
        }), 201
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500

@license_bp.route('/licenses/<int:license_id>', methods=['DELETE'])
@jwt_required()
def delete_license(license_id):
    """Delete a license key"""
    try:
        license = LicenseKey.query.get(license_id)
        
        if not license:
            return jsonify({'error': 'License not found'}), 404
        
        db.session.delete(license)
        db.session.commit()
        
        return jsonify({'message': 'License deleted successfully'}), 200
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500

@license_bp.route('/licenses/revoke/<int:license_id>', methods=['POST'])
@jwt_required()
def revoke_license(license_id):
    """Revoke a license by clearing its device ID"""
    try:
        license = LicenseKey.query.get(license_id)
        
        if not license:
            return jsonify({'error': 'License not found'}), 404
        
        license.device_id = None
        db.session.commit()
        
        return jsonify({'message': 'License revoked successfully'}), 200
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 500

def generate_license_key():
    """Generate a unique license key"""
    # Generate a random license key format: XXXX-XXXX-XXXX-XXXX
    chars = string.ascii_uppercase + string.digits
    
    while True:
        # Generate a key with format XXXX-XXXX-XXXX-XXXX
        key_parts = []
        for i in range(4):
            part = ''.join(random.choice(chars) for _ in range(4))
            key_parts.append(part)
        
        key = '-'.join(key_parts)
        
        # Check if key already exists
        if not LicenseKey.query.filter_by(key=key).first():
            return key