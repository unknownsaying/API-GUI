import pandas as pd
import json
from sqlalchemy.exc import SQLAlchemyError
from models import db, Saying, User
from datetime import datetime
import csv
import io

class BatchProcessor:
    @staticmethod
    def import_csv(file_content, user_id, chunk_size=100):
        """Import sayings from CSV file"""
        try:
            # Read CSV
            df = pd.read_csv(io.StringIO(file_content.decode('utf-8')))
            
            # Validate required columns
            required_columns = ['content']
            if not all(col in df.columns for col in required_columns):
                return False, "CSV must contain 'content' column"
            
            # Process in chunks
            success_count = 0
            error_count = 0
            errors = []
            
            for i in range(0, len(df), chunk_size):
                chunk = df.iloc[i:i+chunk_size]
                sayings_batch = []
                
                for _, row in chunk.iterrows():
                    try:
                        saying = Saying(
                            content=row['content'],
                            author=row.get('author', 'Unknown'),
                            category=row.get('category', 'General'),
                            tags=json.loads(row['tags']) if 'tags' in row and pd.notna(row['tags']) else None,
                            language=row.get('language', 'en'),
                            source=row.get('source', ''),
                            user_id=user_id
                        )
                        sayings_batch.append(saying)
                    except Exception as e:
                        error_count += 1
                        errors.append(f"Row {_}: {str(e)}")
                
                # Bulk insert
                try:
                    db.session.bulk_save_objects(sayings_batch)
                    db.session.commit()
                    success_count += len(sayings_batch)
                except SQLAlchemyError as e:
                    db.session.rollback()
                    error_count += len(sayings_batch)
                    errors.append(f"Database error: {str(e)}")
            
            return True, {
                'success_count': success_count,
                'error_count': error_count,
                'total': len(df),
                'errors': errors[:10]  # Return first 10 errors
            }
            
        except Exception as e:
            return False, f"Import failed: {str(e)}"
    
    @staticmethod
    def import_json(file_content, user_id):
        """Import sayings from JSON file"""
        try:
            data = json.loads(file_content.decode('utf-8'))
            
            if not isinstance(data, list):
                return False, "JSON must contain an array of sayings"
            
            success_count = 0
            errors = []
            
            for item in data:
                try:
                    saying = Saying(
                        content=item['content'],
                        author=item.get('author', 'Unknown'),
                        category=item.get('category', 'General'),
                        tags=item.get('tags'),
                        language=item.get('language', 'en'),
                        source=item.get('source', ''),
                        user_id=user_id
                    )
                    db.session.add(saying)
                    success_count += 1
                except KeyError as e:
                    errors.append(f"Missing required field: {str(e)}")
                except Exception as e:
                    errors.append(f"Error: {str(e)}")
            
            db.session.commit()
            
            return True, {
                'success_count': success_count,
                'error_count': len(errors),
                'errors': errors[:10]
            }
            
        except json.JSONDecodeError as e:
            return False, f"Invalid JSON: {str(e)}"
        except Exception as e:
            return False, f"Import failed: {str(e)}"
    
    @staticmethod
    def export_csv(user_id, filters=None):
        """Export sayings to CSV format"""
        try:
            query = Saying.query.filter_by(user_id=user_id)
            
            # Apply filters
            if filters:
                if 'category' in filters:
                    query = query.filter_by(category=filters['category'])
                if 'author' in filters:
                    query = query.filter_by(author=filters['author'])
                if 'is_public' in filters:
                    query = query.filter_by(is_public=filters['is_public'])
            
            sayings = query.all()
            
            # Create CSV content
            output = io.StringIO()
            writer = csv.writer(output)
            
            # Write header
            writer.writerow(['id', 'content', 'author', 'category', 'tags', 
                           'language', 'source', 'rating', 'view_count', 
                           'is_public', 'created_at', 'updated_at'])
            
            # Write data
            for saying in sayings:
                writer.writerow([
                    saying.id,
                    saying.content,
                    saying.author,
                    saying.category,
                    json.dumps(saying.tags) if saying.tags else '',
                    saying.language,
                    saying.source,
                    saying.rating,
                    saying.view_count,
                    saying.is_public,
                    saying.created_at.isoformat() if saying.created_at else '',
                    saying.updated_at.isoformat() if saying.updated_at else ''
                ])
            
            return True, output.getvalue()
            
        except Exception as e:
            return False, f"Export failed: {str(e)}"
    
    @staticmethod
    def export_json(user_id, filters=None):
        """Export sayings to JSON format"""
        try:
            query = Saying.query.filter_by(user_id=user_id)
            
            # Apply filters
            if filters:
                if 'category' in filters:
                    query = query.filter_by(category=filters['category'])
                if 'author' in filters:
                    query = query.filter_by(author=filters['author'])
            
            sayings = query.all()
            
            # Convert to dictionary list
            data = [saying.to_dict() for saying in sayings]
            
            return True, json.dumps(data, indent=2, ensure_ascii=False)
            
        except Exception as e:
            return False, f"Export failed: {str(e)}"