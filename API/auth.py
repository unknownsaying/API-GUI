from flask import jsonify, request
from flask_jwt_extended import (
    JWTManager, create_access_token, create_refresh_token,
    jwt_required, get_jwt_identity, get_jwt,
    verify_jwt_in_request, decode_token
)
from datetime import datetime, timedelta
import pytz
from models import User, LoginHistory
from database import db

jwt = JWTManager()

def init_auth(app):
    app.config['JWT_SECRET_KEY'] = app.config.get('JWT_SECRET_KEY', 'super-secret-key-change-in-production')
    app.config['JWT_ACCESS_TOKEN_EXPIRES'] = timedelta(hours=1)
    app.config['JWT_REFRESH_TOKEN_EXPIRES'] = timedelta(days=30)
    app.config['JWT_TOKEN_LOCATION'] = ['headers', 'cookies']
    app.config['JWT_COOKIE_SECURE'] = not app.config.get('DEBUG', False)
    app.config['JWT_COOKIE_CSRF_PROTECT'] = True
    
    jwt.init_app(app)
    
    # Register callbacks
    @jwt.user_identity_loader
    def user_identity_lookup(user):
        return user.uuid
    
    @jwt.user_lookup_loader
    def user_lookup_callback(_jwt_header, jwt_data):
        identity = jwt_data["sub"]
        return User.query.filter_by(uuid=identity).one_or_none()
    
    @jwt.expired_token_loader
    def expired_token_callback(jwt_header, jwt_payload):
        return jsonify({
            'success': False,
            'message': 'Token has expired',
            'error': 'token_expired'
        }), 401
    
    @jwt.invalid_token_loader
    def invalid_token_callback(error):
        return jsonify({
            'success': False,
            'message': 'Invalid token',
            'error': 'invalid_token'
        }), 422
    
    @jwt.unauthorized_loader
    def missing_token_callback(error):
        return jsonify({
            'success': False,
            'message': 'Missing authentication token',
            'error': 'authorization_required'
        }), 401
    
    @jwt.revoked_token_loader
    def revoked_token_callback(jwt_header, jwt_payload):
        return jsonify({
            'success': False,
            'message': 'Token has been revoked',
            'error': 'token_revoked'
        }), 401
    
    return jwt

def log_login_attempt(user, success=True, failure_reason=None):
    """Log login attempt to database"""
    try:
        login_record = LoginHistory(
            user_id=user.id if user else None,
            ip_address=request.remote_addr,
            user_agent=request.user_agent.string,
            login_time=datetime.utcnow(),
            status='success' if success else 'failed'
        )
        db.session.add(login_record)
        db.session.commit()
    except Exception as e:
        app.logger.error(f"Failed to log login attempt: {e}")

class AuthManager:
    @staticmethod
    def authenticate(username, password, ip_address=None):
        """Authenticate user and return tokens"""
        user = User.query.filter_by(username=username).first()
        
        if not user or not user.check_password(password):
            log_login_attempt(user, success=False, failure_reason="invalid_credentials")
            return None, "Invalid username or password"
        
        if not user.is_active:
            log_login_attempt(user, success=False, failure_reason="account_inactive")
            return None, "Account is inactive"
        
        # Update last login
        user.last_login = datetime.utcnow()
        db.session.commit()
        
        # Create tokens
        additional_claims = {
            'user_id': user.id,
            'username': user.username,
            'email': user.email,
            'is_admin': user.is_admin
        }
        
        access_token = create_access_token(
            identity=user,
            additional_claims=additional_claims,
            expires_delta=timedelta(hours=1)
        )
        
        refresh_token = create_refresh_token(
            identity=user,
            additional_claims=additional_claims
        )
        
        # Log successful login
        log_login_attempt(user, success=True)
        
        return {
            'access_token': access_token,
            'refresh_token': refresh_token,
            'token_type': 'bearer',
            'expires_in': 3600,
            'user': user.to_dict()
        }, None
    
    @staticmethod
    def refresh_token(refresh_token):
        """Refresh access token"""
        try:
            # Verify refresh token
            verify_jwt_in_request(refresh=True)
            
            current_user = get_jwt_identity()
            user = User.query.filter_by(uuid=current_user).first()
            
            if not user or not user.is_active:
                return None, "Invalid user"
            
            # Create new access token
            additional_claims = {
                'user_id': user.id,
                'username': user.username,
                'email': user.email,
                'is_admin': user.is_admin
            }
            
            new_access_token = create_access_token(
                identity=user,
                additional_claims=additional_claims
            )
            
            return {
                'access_token': new_access_token,
                'token_type': 'bearer',
                'expires_in': 3600
            }, None
            
        except Exception as e:
            return None, str(e)
    
    @staticmethod
    def revoke_token(token):
        """Revoke a token"""
        # In production, store revoked tokens in Redis or database
        pass