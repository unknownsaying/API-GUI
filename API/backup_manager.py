import os
import json
import zipfile
import shutil
from datetime import datetime
from pathlib import Path
import boto3
from google.cloud import storage
import psycopg2
from sqlalchemy import create_engine
import pandas as pd

class BackupManager:
    def __init__(self, app):
        self.app = app
        self.backup_dir = Path(app.config.get('BACKUP_DIR', './backups'))
        self.backup_dir.mkdir(exist_ok=True)
        
        # Cloud storage clients
        self.s3_client = None
        self.gcs_client = None
        
        if app.config.get('AWS_ACCESS_KEY'):
            self.s3_client = boto3.client(
                's3',
                aws_access_key_id=app.config['AWS_ACCESS_KEY'],
                aws_secret_access_key=app.config['AWS_SECRET_KEY']
            )
        
        if app.config.get('GOOGLE_APPLICATION_CREDENTIALS'):
            self.gcs_client = storage.Client()
    
    def create_backup(self, user_id, backup_type='full'):
        """Create a backup for user"""
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        backup_name = f"backup_{user_id}_{timestamp}_{backup_type}"
        backup_path = self.backup_dir / backup_name
        
        try:
            # Create backup directory
            backup_path.mkdir()
            
            # 1. Backup database data
            self._backup_database(user_id, backup_path)
            
            # 2. Backup files (if any)
            self._backup_files(user_id, backup_path)
            
            # 3. Create manifest
            self._create_manifest(user_id, backup_path, backup_type)
            
            # 4. Create zip archive
            zip_path = self.backup_dir / f"{backup_name}.zip"
            self._create_zip(backup_path, zip_path)
            
            # 5. Cleanup
            shutil.rmtree(backup_path)
            
            # 6. Upload to cloud (optional)
            if self.s3_client and self.app.config.get('S3_BACKUP_BUCKET'):
                self._upload_to_s3(zip_path)
            
            if self.gcs_client and self.app.config.get('GCS_BACKUP_BUCKET'):
                self._upload_to_gcs(zip_path)
            
            return True, {
                'backup_name': backup_name,
                'file_path': str(zip_path),
                'file_size': zip_path.stat().st_size,
                'created_at': datetime.now().isoformat()
            }
            
        except Exception as e:
            return False, f"Backup failed: {str(e)}"
    
    def _backup_database(self, user_id, backup_path):
        """Export user data from database"""
        # Export sayings
        sayings_data = self._export_user_sayings(user_id)
        
        # Export user settings
        settings_data = self._export_user_settings(user_id)
        
        # Save to JSON files
        with open(backup_path / 'sayings.json', 'w', encoding='utf-8') as f:
            json.dump(sayings_data, f, indent=2, ensure_ascii=False)
        
        with open(backup_path / 'settings.json', 'w', encoding='utf-8') as f:
            json.dump(settings_data, f, indent=2, ensure_ascii=False)
    
    def restore_backup(self, user_id, backup_file):
        """Restore from backup file"""
        try:
            # Extract backup
            extract_path = self.backup_dir / 'temp_restore'
            extract_path.mkdir(exist_ok=True)
            
            with zipfile.ZipFile(backup_file, 'r') as zip_ref:
                zip_ref.extractall(extract_path)
            
            # Read manifest
            manifest_path = extract_path / 'manifest.json'
            if not manifest_path.exists():
                raise ValueError("Invalid backup: manifest not found")
            
            with open(manifest_path, 'r', encoding='utf-8') as f:
                manifest = json.load(f)
            
            # Restore database data
            self._restore_database(user_id, extract_path)
            
            # Restore files
            self._restore_files(user_id, extract_path)
            
            # Cleanup
            shutil.rmtree(extract_path)
            
            return True, "Backup restored successfully"
            
        except Exception as e:
            return False, f"Restore failed: {str(e)}"
    
    def list_backups(self, user_id):
        """List available backups for user"""
        backups = []
        
        # List local backups
        for backup_file in self.backup_dir.glob(f"*_{user_id}_*.zip"):
            stats = backup_file.stat()
            backups.append({
                'name': backup_file.name,
                'path': str(backup_file),
                'size': stats.st_size,
                'created': datetime.fromtimestamp(stats.st_ctime).isoformat(),
                'type': 'local'
            })
        
        # List cloud backups
        if self.s3_client:
            s3_backups = self._list_s3_backups(user_id)
            backups.extend(s3_backups)
        
        if self.gcs_client:
            gcs_backups = self._list_gcs_backups(user_id)
            backups.extend(gcs_backups)
        
        return sorted(backups, key=lambda x: x['created'], reverse=True)