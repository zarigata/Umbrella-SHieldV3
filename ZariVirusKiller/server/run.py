import os
import sys

# Add the parent directory to the path so we can import the models module
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from app import create_app
from models import db

# Create the Flask application instance
app = create_app()

# Create database tables if they don't exist
with app.app_context():
    db.create_all()

if __name__ == '__main__':
    # Get port from environment variable or use default
    port = int(os.environ.get('PORT', 5000))
    
    # Run the application
    app.run(
        host='0.0.0.0',
        port=port,
        debug=os.environ.get('FLASK_ENV') == 'development'
    )