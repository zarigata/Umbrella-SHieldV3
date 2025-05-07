from flask import Flask, send_from_directory
from flask_sqlalchemy import SQLAlchemy
from flask_jwt_extended import JWTManager
from flask_cors import CORS
import os
from datetime import timedelta

# Initialize extensions
db = SQLAlchemy()
jwt = JWTManager()

def create_app(config=None):
    # Create and configure the app
    app = Flask(__name__)
    
    # Configure the app
    app.config['SQLALCHEMY_DATABASE_URI'] = os.getenv('DATABASE_URL', 'postgresql://user:password@localhost:5432/zarivirus')
    app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False
    app.config['JWT_SECRET_KEY'] = os.getenv('JWT_SECRET_KEY', 'dev-secret-key')
    app.config['JWT_ACCESS_TOKEN_EXPIRES'] = timedelta(hours=1)
    
    # File upload configuration
    app.config['UPLOAD_FOLDER'] = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'uploads')
    app.config['DEFINITIONS_FOLDER'] = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'definitions')
    app.config['MAX_CONTENT_LENGTH'] = 16 * 1024 * 1024  # 16MB max upload
    
    # Ensure directories exist
    os.makedirs(app.config['UPLOAD_FOLDER'], exist_ok=True)
    os.makedirs(app.config['DEFINITIONS_FOLDER'], exist_ok=True)
    
    # Override config if provided
    if config:
        app.config.update(config)
    
    # Initialize extensions with app
    db.init_app(app)
    jwt.init_app(app)
    CORS(app)
    
    # Register blueprints
    from app.routes import api
    from app.auth import auth, create_initial_admin
    from app.license_manager import license_bp
    app.register_blueprint(api, url_prefix='/api')
    app.register_blueprint(auth, url_prefix='/auth')
    app.register_blueprint(license_bp, url_prefix='/api/license')
    
    # Create initial admin user
    create_initial_admin(app)
    
    # Add route to serve the dashboard
    @app.route('/admin', defaults={'path': ''})
    @app.route('/admin/<path:path>')
    def serve_admin(path):
        if path and os.path.exists(os.path.join(app.static_folder, 'admin', path)):
            return send_from_directory(os.path.join(app.static_folder, 'admin'), path)
        return send_from_directory(os.path.join(app.static_folder, 'admin'), 'index.html')
    
    # Import models for database creation
    import sys
    import os
    sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    from models import User
    
    return app