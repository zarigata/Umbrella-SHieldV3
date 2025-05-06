# Server Implementation Guide

## Overview

This document provides detailed implementation instructions for completing the server-side components of the ZariVirusKiller project, including the API endpoints and admin dashboard.

## Current State

The server currently has:
- Basic Flask API with endpoints for license verification and virus definition distribution
- Database models for license keys, definition updates, and virus signatures
- File upload and scanning functionality

## Implementation Tasks

### 1. Complete API Endpoints

#### 1.1 Authentication System

Implement JWT-based authentication for admin endpoints:

```python
from flask_jwt_extended import JWTManager, create_access_token, jwt_required, get_jwt_identity

app.config['JWT_SECRET_KEY'] = os.getenv('JWT_SECRET_KEY', 'dev-secret-key')
app.config['JWT_ACCESS_TOKEN_EXPIRES'] = datetime.timedelta(hours=1)
jwt = JWTManager(app)

@app.route('/login', methods=['POST'])
def login():
    data = request.get_json()
    username = data.get('username')
    password = data.get('password')
    
    # Check credentials against database
    if username == 'admin' and password == 'secure_password':  # Replace with actual auth
        access_token = create_access_token(identity=username)
        return jsonify(access_token=access_token), 200
    
    return jsonify({'error': 'Invalid credentials'}), 401
```

Secure admin endpoints with the `@jwt_required()` decorator.

#### 1.2 Complete Virus Signature Management

Enhance the `/add-signature` endpoint to support pattern-based signatures:

```python
@app.route('/add-signature', methods=['POST'])
@jwt_required()
def add_signature():
    data = request.get_json()
    
    required_fields = ['name', 'severity']
    for field in required_fields:
        if field not in data:
            return jsonify({'error': f'Missing required field: {field}'}), 400
    
    # Get latest definition
    latest_definition = DefinitionUpdate.query.order_by(DefinitionUpdate.uploaded_at.desc()).first()
    if not latest_definition:
        return jsonify({'error': 'No definition available to add signatures to'}), 404
    
    # Create new signature based on type
    if 'hash_signature' in data:
        # Hash-based signature
        new_signature = VirusSignature(
            name=data['name'],
            hash_signature=data['hash_signature'],
            severity=data['severity'],
            definition_id=latest_definition.id
        )
    elif 'patterns' in data:
        # Pattern-based signature
        new_signature = VirusSignature(
            name=data['name'],
            detection_pattern=json.dumps(data['patterns']),
            severity=data['severity'],
            definition_id=latest_definition.id
        )
    else:
        return jsonify({'error': 'Either hash_signature or patterns must be provided'}), 400
    
    db.session.add(new_signature)
    db.session.commit()
    
    return jsonify({
        'id': new_signature.id,
        'name': new_signature.name,
        'severity': new_signature.severity
    }), 201
```

#### 1.3 Implement Batch License Generation

```python
@app.route('/generate-licenses', methods=['POST'])
@jwt_required()
def generate_licenses():
    data = request.get_json()
    count = data.get('count', 1)
    expiration_days = data.get('expiration_days', 365)
    prefix = data.get('prefix', '')
    
    if count > 1000:
        return jsonify({'error': 'Cannot generate more than 1000 licenses at once'}), 400
    
    generated_keys = []
    expiration_date = datetime.datetime.now() + datetime.timedelta(days=expiration_days)
    
    for _ in range(count):
        # Generate a unique key with prefix
        key = f"{prefix}{uuid.uuid4().hex}"
        
        # Create license record
        new_license = LicenseKey(
            key=key,
            expires_at=expiration_date
        )
        
        db.session.add(new_license)
        generated_keys.append(key)
    
    db.session.commit()
    
    return jsonify({
        'count': len(generated_keys),
        'keys': generated_keys,
        'expires_at': expiration_date.isoformat()
    }), 201
```

### 2. Update Database Schema

Update the database schema to support pattern-based signatures and statistics:

```sql
-- Add signature_count to definition_update table
ALTER TABLE definition_update ADD COLUMN signature_count INTEGER DEFAULT 0;

-- Create scan_statistics table
CREATE TABLE scan_statistics (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(64) NOT NULL,
    scan_date TIMESTAMP DEFAULT now(),
    scanned_files INTEGER NOT NULL,
    threats_found INTEGER NOT NULL,
    duration_seconds INTEGER NOT NULL
);

-- Create user_activity table
CREATE TABLE user_activity (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(64) NOT NULL,
    activity_type VARCHAR(32) NOT NULL,
    activity_date TIMESTAMP DEFAULT now(),
    details JSONB
);
```

Implement these changes in a migration script:

```python
# server/scripts/migrations/001_update_schema.py
import psycopg2
import os

def run_migration():
    conn = psycopg2.connect(os.getenv('DATABASE_URL'))
    cursor = conn.cursor()
    
    try:
        # Add signature_count to definition_update
        cursor.execute("""
        ALTER TABLE definition_update 
        ADD COLUMN IF NOT EXISTS signature_count INTEGER DEFAULT 0;
        """)
        
        # Create scan_statistics table
        cursor.execute("""
        CREATE TABLE IF NOT EXISTS scan_statistics (
            id SERIAL PRIMARY KEY,
            device_id VARCHAR(64) NOT NULL,
            scan_date TIMESTAMP DEFAULT now(),
            scanned_files INTEGER NOT NULL,
            threats_found INTEGER NOT NULL,
            duration_seconds INTEGER NOT NULL
        );
        """)
        
        # Create user_activity table
        cursor.execute("""
        CREATE TABLE IF NOT EXISTS user_activity (
            id SERIAL PRIMARY KEY,
            device_id VARCHAR(64) NOT NULL,
            activity_type VARCHAR(32) NOT NULL,
            activity_date TIMESTAMP DEFAULT now(),
            details JSONB
        );
        """)
        
        conn.commit()
        print("Migration completed successfully")
    except Exception as e:
        conn.rollback()
        print(f"Migration failed: {e}")
    finally:
        cursor.close()
        conn.close()

if __name__ == "__main__":
    run_migration()
```

### 3. Admin Dashboard Implementation

#### 3.1 Setup TypeScript/Tailwind Project

Create a new TypeScript project in the `server/web` directory:

```bash
# Initialize TypeScript project
npm init -y
npm install --save-dev typescript ts-node @types/node
npm install --save react react-dom next
npm install --save-dev tailwindcss postcss autoprefixer
npm install --save axios chart.js react-chartjs-2

# Initialize Tailwind CSS
npx tailwindcss init -p
```

Configure Tailwind CSS in `tailwind.config.js`:

```javascript
module.exports = {
  content: [
    './pages/**/*.{js,ts,jsx,tsx}',
    './components/**/*.{js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        pastelGreen: '#A8D5BA',
        pastelPink: '#FFD1DC',
      },
    },
  },
  plugins: [],
}
```

#### 3.2 Create Dashboard Pages

Implement the following pages:

1. **Login Page** (`pages/login.tsx`)
2. **Dashboard Overview** (`pages/index.tsx`)
3. **License Management** (`pages/licenses.tsx`)
4. **Virus Definitions** (`pages/definitions.tsx`)
5. **Statistics** (`pages/statistics.tsx`)

Example of the License Management page:

```typescript
// pages/licenses.tsx
import { useState, useEffect } from 'react';
import axios from 'axios';
import { useRouter } from 'next/router';

interface License {
  id: number;
  key: string;
  created_at: string;
  expires_at: string;
  device_id: string | null;
}

export default function Licenses() {
  const [licenses, setLicenses] = useState<License[]>([]);
  const [loading, setLoading] = useState(true);
  const [count, setCount] = useState(10);
  const [expirationDays, setExpirationDays] = useState(365);
  const [prefix, setPrefix] = useState('');
  const router = useRouter();
  
  useEffect(() => {
    // Check if user is logged in
    const token = localStorage.getItem('token');
    if (!token) {
      router.push('/login');
      return;
    }
    
    // Fetch licenses
    fetchLicenses();
  }, []);
  
  const fetchLicenses = async () => {
    try {
      const token = localStorage.getItem('token');
      const response = await axios.get('http://localhost:5000/licenses', {
        headers: { Authorization: `Bearer ${token}` }
      });
      setLicenses(response.data);
      setLoading(false);
    } catch (error) {
      console.error('Error fetching licenses:', error);
      setLoading(false);
    }
  };
  
  const generateLicenses = async () => {
    try {
      const token = localStorage.getItem('token');
      const response = await axios.post('http://localhost:5000/generate-licenses', {
        count,
        expiration_days: expirationDays,
        prefix
      }, {
        headers: { Authorization: `Bearer ${token}` }
      });
      
      alert(`Generated ${response.data.count} licenses`);
      fetchLicenses();
    } catch (error) {
      console.error('Error generating licenses:', error);
    }
  };
  
  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-6">License Management</h1>
      
      <div className="bg-white rounded-lg shadow p-6 mb-8">
        <h2 className="text-xl font-semibold mb-4">Generate Licenses</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Count</label>
            <input 
              type="number" 
              value={count} 
              onChange={(e) => setCount(parseInt(e.target.value))} 
              className="w-full p-2 border rounded"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Expiration (days)</label>
            <input 
              type="number" 
              value={expirationDays} 
              onChange={(e) => setExpirationDays(parseInt(e.target.value))} 
              className="w-full p-2 border rounded"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Prefix</label>
            <input 
              type="text" 
              value={prefix} 
              onChange={(e) => setPrefix(e.target.value)} 
              className="w-full p-2 border rounded"
            />
          </div>
        </div>
        <button 
          onClick={generateLicenses} 
          className="mt-4 bg-pastelGreen hover:bg-opacity-80 text-white font-bold py-2 px-4 rounded"
        >
          Generate Licenses
        </button>
      </div>
      
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">License Key</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Expires</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {loading ? (
              <tr>
                <td colSpan={4} className="px-6 py-4 text-center">Loading...</td>
              </tr>
            ) : licenses.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-6 py-4 text-center">No licenses found</td>
              </tr>
            ) : (
              licenses.map((license) => (
                <tr key={license.id}>
                  <td className="px-6 py-4 whitespace-nowrap">{license.key}</td>
                  <td className="px-6 py-4 whitespace-nowrap">{new Date(license.created_at).toLocaleDateString()}</td>
                  <td className="px-6 py-4 whitespace-nowrap">{new Date(license.expires_at).toLocaleDateString()}</td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {license.device_id ? (
                      <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">
                        Activated
                      </span>
                    ) : (
                      <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-gray-100 text-gray-800">
                        Available
                      </span>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
```

#### 3.3 API Integration

Create an API service to interact with the Flask backend:

```typescript
// services/api.ts
import axios from 'axios';

const API_URL = 'http://localhost:5000';

const api = axios.create({
  baseURL: API_URL,
});

// Add request interceptor to include auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

export const login = async (username: string, password: string) => {
  const response = await api.post('/login', { username, password });
  return response.data;
};

export const getLicenses = async () => {
  const response = await api.get('/licenses');
  return response.data;
};

export const generateLicenses = async (count: number, expirationDays: number, prefix: string) => {
  const response = await api.post('/generate-licenses', { count, expiration_days: expirationDays, prefix });
  return response.data;
};

export const getDefinitions = async () => {
  const response = await api.get('/definitions');
  return response.data;
};

export const uploadDefinition = async (file: File, version: string, signatureCount: number) => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('version', version);
  formData.append('signature_count', signatureCount.toString());
  
  const response = await api.post('/upload-definitions', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  
  return response.data;
};

export const getStatistics = async () => {
  const response = await api.get('/statistics');
  return response.data;
};

export default api;
```

### 4. Integration with Flask

Update the Flask app to serve the Next.js dashboard:

```python
# server/app/app.py

# Add route to serve the dashboard
@app.route('/admin', defaults={'path': ''})
@app.route('/admin/<path:path>')
def serve_admin(path):
    if path and os.path.exists(os.path.join(app.static_folder, 'admin', path)):
        return send_from_directory(os.path.join(app.static_folder, 'admin'), path)
    return send_from_directory(os.path.join(app.static_folder, 'admin'), 'index.html')
```

### 5. Deployment Configuration

Update the Dockerfile to build and include the admin dashboard:

```dockerfile
# server/Dockerfile
FROM node:16 AS web-builder
WORKDIR /app
COPY server/web/package*.json ./
RUN npm install
COPY server/web/ ./
RUN npm run build

FROM python:3.9-slim
WORKDIR /app

# Install dependencies
COPY server/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY server/app/ ./app/
COPY server/db/ ./db/
COPY server/scripts/ ./scripts/

# Copy built web dashboard
COPY --from=web-builder /app/out ./app/static/admin/

# Create necessary directories
RUN mkdir -p uploads definitions

# Expose port
EXPOSE 5000

# Set environment variables
ENV FLASK_APP=app/app.py
ENV FLASK_ENV=production

# Run the application
CMD ["flask", "run", "--host=0.0.0.0", "--port=5000"]
```

Update the docker-compose.yml file:

```yaml
# server/docker-compose.yml
version: '3'

services:
  web:
    build: .
    ports:
      - "5000:5000"
    environment:
      - DATABASE_URL=postgresql://zarivirus:password@db:5432/zarivirus
      - JWT_SECRET_KEY=your-secret-key-here
    volumes:
      - ./uploads:/app/uploads
      - ./definitions:/app/definitions
    depends_on:
      - db

  db:
    image: postgres:13
    environment:
      - POSTGRES_USER=zarivirus
      - POSTGRES_PASSWORD=password
      - POSTGRES_DB=zarivirus
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  migration:
    build: .
    command: python scripts/init_db.py
    environment:
      - DATABASE_URL=postgresql://zarivirus:password@db:5432/zarivirus
    depends_on:
      - db

volumes:
  postgres_data:
```

## Testing

1. Create unit tests for API endpoints
2. Test the admin dashboard with different screen sizes
3. Validate license generation and verification
4. Test definition upload and distribution

## Next Steps

1. Implement the admin dashboard UI
2. Complete the API endpoints
3. Update the database schema
4. Test the integration between client and server
5. Deploy the server using Docker

This implementation will provide a complete server-side solution for the ZariVirusKiller project, including license management, virus definition distribution, and an admin dashboard for managing the system.