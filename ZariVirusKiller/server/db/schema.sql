-- Schema PostgreSQL

CREATE TABLE license_key (
    id SERIAL PRIMARY KEY,
    key VARCHAR(128) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT now(),
    expires_at TIMESTAMP NOT NULL,
    device_id VARCHAR(64)
);

CREATE TABLE definition_update (
    id SERIAL PRIMARY KEY,
    version VARCHAR(16) NOT NULL,
    path VARCHAR(256) NOT NULL,
    uploaded_at TIMESTAMP DEFAULT now()
);
