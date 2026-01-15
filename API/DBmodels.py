from datetime import datetime
from database import db
from werkzeug.security import generate_password_hash, check_password_hash
import uuid

class User(db.Model):
    __tablename__ = 'users'
    
    id = db.Column(db.Integer, primary_key=True)
    uuid = db.Column(db.String(36), unique=True, default=lambda: str(uuid.uuid4()))
    username = db.Column(db.String(80), unique=True, nullable=False, index=True)
    email = db.Column(db.String(120), unique=True, nullable=False, index=True)
    password_hash = db.Column(db.String(256), nullable=False)
    is_active = db.Column(db.Boolean, default=True)
    is_admin = db.Column(db.Boolean, default=False)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    last_login = db.Column(db.DateTime)
    
    # Relationships
    sayings = db.relationship('Saying', backref='user', lazy=True, cascade='all, delete-orphan')
    login_history = db.relationship('LoginHistory', backref='user', lazy=True, cascade='all, delete-orphan')
    backups = db.relationship('Backup', backref='user', lazy=True, cascade='all, delete-orphan')
    
    def set_password(self, password):
        self.password_hash = generate_password_hash(password)
    
    def check_password(self, password):
        return check_password_hash(self.password_hash, password)
    
    def to_dict(self):
        return {
            'id': self.id,
            'uuid': self.uuid,
            'username': self.username,
            'email': self.email,
            'is_active': self.is_active,
            'is_admin': self.is_admin,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'last_login': self.last_login.isoformat() if self.last_login else None
        }

class Saying(db.Model):
    __tablename__ = 'sayings'
    
    id = db.Column(db.Integer, primary_key=True)
    uuid = db.Column(db.String(36), unique=True, default=lambda: str(uuid.uuid4()))
    content = db.Column(db.Text, nullable=False, index=True)
    author = db.Column(db.String(200), default="Unknown", index=True)
    category = db.Column(db.String(100), default="General", index=True)
    tags = db.Column(db.JSON)  # List of tags
    language = db.Column(db.String(10), default="en")
    source = db.Column(db.String(200))
    rating = db.Column(db.Float, default=0.0)
    view_count = db.Column(db.Integer, default=0)
    is_public = db.Column(db.Boolean, default=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Foreign keys
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=False)
    
    # Indexes
    __table_args__ = (
        db.Index('idx_sayings_search', 'content', 'author', 'category'),
        db.Index('idx_sayings_user', 'user_id', 'created_at'),
    )
    
    def to_dict(self):
        return {
            'id': self.id,
            'uuid': self.uuid,
            'content': self.content,
            'author': self.author,
            'category': self.category,
            'tags': self.tags or [],
            'language': self.language,
            'source': self.source,
            'rating': self.rating,
            'view_count': self.view_count,
            'is_public': self.is_public,
            'user_id': self.user_id,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None
        }

class LoginHistory(db.Model):
    __tablename__ = 'login_history'
    
    id = db.Column(db.Integer, primary_key=True)
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=False)
    ip_address = db.Column(db.String(45))
    user_agent = db.Column(db.Text)
    login_time = db.Column(db.DateTime, default=datetime.utcnow)
    logout_time = db.Column(db.DateTime)
    status = db.Column(db.String(20))  # success, failed, locked

class Backup(db.Model):
    __tablename__ = 'backups'
    
    id = db.Column(db.Integer, primary_key=True)
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=False)
    filename = db.Column(db.String(255), nullable=False)
    file_path = db.Column(db.String(500))
    file_size = db.Column(db.Integer)
    backup_type = db.Column(db.String(20))  # full, incremental
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    status = db.Column(db.String(20), default='completed')  # pending, processing, completed, failed

class UsageStatistics(db.Model):
    __tablename__ = 'usage_statistics'
    
    id = db.Column(db.Integer, primary_key=True)
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=False)
    date = db.Column(db.Date, default=datetime.utcnow().date)
    sayings_created = db.Column(db.Integer, default=0)
    sayings_updated = db.Column(db.Integer, default=0)
    sayings_deleted = db.Column(db.Integer, default=0)
    api_calls = db.Column(db.Integer, default=0)
    login_count = db.Column(db.Integer, default=0)
    total_view_count = db.Column(db.Integer, default=0)