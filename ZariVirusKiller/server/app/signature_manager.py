import os
import json
import datetime
from flask import current_app
from app import db
from models import VirusSignature, DefinitionUpdate

class SignatureManager:
    """
    Manages virus signatures and definition updates
    """
    
    @staticmethod
    def add_hash_signature(name, hash_value, severity="medium", description=None):
        """
        Adds a hash-based signature to the database
        """
        try:
            # Check if signature already exists
            existing = VirusSignature.query.filter_by(hash_value=hash_value).first()
            if existing:
                return False, "Signature with this hash already exists"
            
            # Create new signature
            signature = VirusSignature(
                name=name,
                hash_value=hash_value,
                signature_type="hash",
                severity=severity,
                description=description or f"Hash-based signature for {name}",
                created_at=datetime.datetime.utcnow()
            )
            
            db.session.add(signature)
            db.session.commit()
            
            # Update definitions file
            SignatureManager.generate_definitions_file()
            
            return True, "Signature added successfully"
        except Exception as e:
            db.session.rollback()
            return False, str(e)
    
    @staticmethod
    def add_pattern_signature(name, patterns, logic="all", severity="medium", description=None):
        """
        Adds a pattern-based signature to the database
        """
        try:
            # Validate patterns
            if not patterns or not isinstance(patterns, list):
                return False, "Invalid patterns format"
            
            # Create signature ID
            signature_id = f"ZARI-{VirusSignature.query.count() + 1:04d}"
            
            # Create new signature
            signature = VirusSignature(
                name=name,
                signature_id=signature_id,
                signature_type="pattern",
                severity=severity,
                description=description or f"Pattern-based signature for {name}",
                created_at=datetime.datetime.utcnow(),
                pattern_data=json.dumps({
                    "patterns": patterns,
                    "logic": logic
                })
            )
            
            db.session.add(signature)
            db.session.commit()
            
            # Update definitions file
            SignatureManager.generate_pattern_definitions_file()
            
            return True, "Pattern signature added successfully"
        except Exception as e:
            db.session.rollback()
            return False, str(e)
    
    @staticmethod
    def generate_definitions_file():
        """
        Generates the hash-based definitions file
        """
        try:
            # Get all hash-based signatures
            signatures = VirusSignature.query.filter_by(signature_type="hash").all()
            
            # Create definitions dictionary
            definitions = {}
            for sig in signatures:
                definitions[sig.name] = sig.hash_value
            
            # Create definitions directory if it doesn't exist
            definitions_dir = current_app.config['DEFINITIONS_FOLDER']
            os.makedirs(definitions_dir, exist_ok=True)
            
            # Write definitions to file
            definitions_file = os.path.join(definitions_dir, "signatures.json")
            with open(definitions_file, 'w') as f:
                json.dump(definitions, f, indent=2)
            
            # Create definition update record
            version = datetime.datetime.utcnow().strftime("%Y%m%d%H%M")
            update = DefinitionUpdate(
                version=version,
                path=definitions_file,
                signature_count=len(signatures)
            )
            
            db.session.add(update)
            db.session.commit()
            
            return True, "Definitions file generated successfully"
        except Exception as e:
            db.session.rollback()
            return False, str(e)
    
    @staticmethod
    def generate_pattern_definitions_file():
        """
        Generates the pattern-based definitions file
        """
        try:
            # Get all pattern-based signatures
            signatures = VirusSignature.query.filter_by(signature_type="pattern").all()
            
            # Create signature container
            signature_container = {
                "signatures": []
            }
            
            # Add each signature
            for sig in signatures:
                pattern_data = json.loads(sig.pattern_data)
                signature_container["signatures"].append({
                    "id": sig.signature_id,
                    "name": sig.name,
                    "severity": sig.severity,
                    "patterns": pattern_data["patterns"],
                    "logic": pattern_data["logic"]
                })
            
            # Create definitions directory if it doesn't exist
            definitions_dir = current_app.config['DEFINITIONS_FOLDER']
            os.makedirs(definitions_dir, exist_ok=True)
            
            # Write definitions to file
            patterns_file = os.path.join(definitions_dir, "patterns.json")
            with open(patterns_file, 'w') as f:
                json.dump(signature_container, f, indent=2)
            
            # Create definition update record
            version = datetime.datetime.utcnow().strftime("%Y%m%d%H%M")
            update = DefinitionUpdate(
                version=version,
                path=patterns_file,
                signature_count=len(signatures),
                update_type="pattern"
            )
            
            db.session.add(update)
            db.session.commit()
            
            return True, "Pattern definitions file generated successfully"
        except Exception as e:
            db.session.rollback()
            return False, str(e)
    
    @staticmethod
    def get_latest_definitions():
        """
        Gets the latest definitions information
        """
        try:
            # Get latest hash-based definitions
            hash_update = DefinitionUpdate.query.filter_by(update_type="hash").order_by(DefinitionUpdate.id.desc()).first()
            
            # Get latest pattern-based definitions
            pattern_update = DefinitionUpdate.query.filter_by(update_type="pattern").order_by(DefinitionUpdate.id.desc()).first()
            
            return {
                "hash_definitions": {
                    "version": hash_update.version if hash_update else None,
                    "signature_count": hash_update.signature_count if hash_update else 0,
                    "path": hash_update.path if hash_update else None
                },
                "pattern_definitions": {
                    "version": pattern_update.version if pattern_update else None,
                    "signature_count": pattern_update.signature_count if pattern_update else 0,
                    "path": pattern_update.path if pattern_update else None
                }
            }
        except Exception as e:
            return {"error": str(e)}