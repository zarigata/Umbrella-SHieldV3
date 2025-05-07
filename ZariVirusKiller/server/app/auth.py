from flask import Blueprint, request, jsonify, current_app
from flask_jwt_extended import create_access_token, jwt_required, get_jwt_identity
from werkzeug.security import generate_password_hash, check_password_hash
from models import db, User
import datetime

auth = Blueprint('auth', __name__)

@auth.route('/login', methods=['POST'])
def login():
    """Handle user login and generate JWT token"""
    data = request.get_json()
    username = data.get('username')
    password = data.get('password')
    
    if not username or not password:
        return jsonify({'error': 'Username and password are required'}), 400
    
    # Find user in database
    user = User.query.filter_by(username=username).first()
    
    # Check if user exists and password is correct
    if user and check_password_hash(user.password_hash, password):
        # Create access token with user identity
        access_token = create_access_token(
            identity=username,
            additional_claims={'is_admin': user.is_admin}
        )
        return jsonify({
            'access_token': access_token,
            'user': {
                'username': user.username,
                'is_admin': user.is_admin
            }
        }), 200
    
    return jsonify({'error': 'Invalid credentials'}), 401

@auth.route('/register', methods=['POST'])
@jwt_required()
def register():
    """Register a new admin user (requires admin privileges)"""
    # Check if current user is admin
    current_user = get_jwt_identity()
    user = User.query.filter_by(username=current_user).first()
    
    if not user or not user.is_admin:
        return jsonify({'error': 'Admin privileges required'}), 403
    
    # Get new user data
    data = request.get_json()
    username = data.get('username')
    password = data.get('password')
    is_admin = data.get('is_admin', False)
    
    if not username or not password:
        return jsonify({'error': 'Username and password are required'}), 400
    
    # Check if username already exists
    if User.query.filter_by(username=username).first():
        return jsonify({'error': 'Username already exists'}), 409
    
    # Create new user
    new_user = User(
        username=username,
        password_hash=generate_password_hash(password),
        is_admin=is_admin
    )
    
    db.session.add(new_user)
    db.session.commit()
    
    return jsonify({'message': 'User created successfully'}), 201

@auth.route('/users', methods=['GET'])
@jwt_required()
def get_users():
    """Get all users (requires admin privileges)"""
    # Check if current user is admin
    current_user = get_jwt_identity()
    user = User.query.filter_by(username=current_user).first()
    
    if not user or not user.is_admin:
        return jsonify({'error': 'Admin privileges required'}), 403
    
    # Get all users
    users = User.query.all()
    
    # Format response
    result = []
    for u in users:
        result.append({
            'id': u.id,
            'username': u.username,
            'is_admin': u.is_admin,
            'created_at': u.created_at.isoformat()
        })
    
    return jsonify(result), 200

# Function to create initial admin user
def create_initial_admin(app):
    """Create initial admin user if no users exist"""
    with app.app_context():
        if User.query.count() == 0:
            admin = User(
                username='admin',
                password_hash=generate_password_hash('admin123'),
                is_admin=True
            )
            db.session.add(admin)
            db.session.commit()
            print('Initial admin user created')