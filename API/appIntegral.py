from flask import Flask, request, jsonify
from flask_cors import CORS
from datetime import datetime
import os
from database import db
from models import User, Saying
from auth import init_auth
from flask_jwt_extended import jwt_required, create_access_token, get_jwt_identity
import hashlib

app = Flask(__name__)
CORS(app)

# 配置数据库
app.config['SQLALCHEMY_DATABASE_URI'] = os.getenv('DATABASE_URL', 'sqlite:///sayings.db')
app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False

# 初始化数据库
db.init_app(app)

# 初始化JWT
jwt = init_auth(app)

# 创建数据库表
with app.app_context():
    db.create_all()

# 用户注册
@app.route('/api/auth/register', methods=['POST'])
def register():
    data = request.get_json()
    username = data.get('username')
    password = data.get('password')

    if not username or not password:
        return jsonify({'success': False, 'message': 'Username and password are required'}), 400

    if User.query.filter_by(username=username).first():
        return jsonify({'success': False, 'message': 'Username already exists'}), 400

    # 创建用户（注意：实际生产环境中应使用加密密码，这里使用简单的哈希）
    password_hash = hashlib.sha256(password.encode()).hexdigest()
    new_user = User(username=username, password_hash=password_hash)
    db.session.add(new_user)
    db.session.commit()

    return jsonify({'success': True, 'message': 'User created successfully'}), 201

# 用户登录
@app.route('/api/auth/login', methods=['POST'])
def login():
    data = request.get_json()
    username = data.get('username')
    password = data.get('password')

    user = User.query.filter_by(username=username).first()
    if not user:
        return jsonify({'success': False, 'message': 'Invalid credentials'}), 401

    password_hash = hashlib.sha256(password.encode()).hexdigest()
    if user.password_hash != password_hash:
        return jsonify({'success': False, 'message': 'Invalid credentials'}), 401

    # 创建访问令牌
    access_token = create_access_token(identity=user.id)
    return jsonify({'success': True, 'access_token': access_token, 'user_id': user.id})

# 获取所有说法（需要登录）
@app.route('/api/sayings', methods=['GET'])
@jwt_required()
def get_all_sayings():
    current_user_id = get_jwt_identity()
    sayings = Saying.query.filter_by(user_id=current_user_id).all()
    return jsonify({
        'success': True,
        'count': len(sayings),
        'data': [{
            'id': s.id,
            'content': s.content,
            'author': s.author,
            'category': s.category,
            'created_date': s.created_date.isoformat() if s.created_date else None,
            'last_modified': s.last_modified.isoformat() if s.last_modified else None
        } for s in sayings]
    })

# 获取单个说法（需要登录）
@app.route('/api/sayings/<int:saying_id>', methods=['GET'])
@jwt_required()
def get_saying(saying_id):
    current_user_id = get_jwt_identity()
    saying = Saying.query.filter_by(id=saying_id, user_id=current_user_id).first()
    if not saying:
        return jsonify({'success': False, 'message': 'Saying not found'}), 404

    return jsonify({
        'success': True,
        'data': {
            'id': saying.id,
            'content': saying.content,
            'author': saying.author,
            'category': saying.category,
            'created_date': saying.created_date.isoformat() if saying.created_date else None,
            'last_modified': saying.last_modified.isoformat() if saying.last_modified else None
        }
    })

# 创建说法（需要登录）
@app.route('/api/sayings', methods=['POST'])
@jwt_required()
def create_saying():
    current_user_id = get_jwt_identity()
    data = request.get_json()

    if not data or 'content' not in data:
        return jsonify({'success': False, 'message': 'Content is required'}), 400

    content = data.get('content', '').strip()
    if not content:
        return jsonify({'success': False, 'message': 'Content cannot be empty'}), 400

    author = data.get('author', 'Unknown').strip() or 'Unknown'
    category = data.get('category', 'General').strip() or 'General'

    new_saying = Saying(
        content=content,
        author=author,
        category=category,
        user_id=current_user_id
    )
    db.session.add(new_saying)
    db.session.commit()

    return jsonify({
        'success': True,
        'message': 'Saying created successfully',
        'data': {
            'id': new_saying.id,
            'content': new_saying.content,
            'author': new_saying.author,
            'category': new_saying.category,
            'created_date': new_saying.created_date.isoformat() if new_saying.created_date else None,
            'last_modified': new_saying.last_modified.isoformat() if new_saying.last_modified else None
        }
    }), 201

# 更新说法（需要登录）
@app.route('/api/sayings/<int:saying_id>', methods=['PUT'])
@jwt_required()
def update_saying(saying_id):
    current_user_id = get_jwt_identity()
    saying = Saying.query.filter_by(id=saying_id, user_id=current_user_id).first()
    if not saying:
        return jsonify({'success': False, 'message': 'Saying not found'}), 404

    data = request.get_json()
    if not data:
        return jsonify({'success': False, 'message': 'No data provided'}), 400

    if 'content' in data:
        content = data['content'].strip()
        if not content:
            return jsonify({'success': False, 'message': 'Content cannot be empty'}), 400
        saying.content = content

    if 'author' in data:
        saying.author = data['author'].strip() or 'Unknown'

    if 'category' in data:
        saying.category = data['category'].strip() or 'General'

    saying.last_modified = datetime.utcnow()
    db.session.commit()

    return jsonify({
        'success': True,
        'message': 'Saying updated successfully',
        'data': {
            'id': saying.id,
            'content': saying.content,
            'author': saying.author,
            'category': saying.category,
            'created_date': saying.created_date.isoformat() if saying.created_date else None,
            'last_modified': saying.last_modified.isoformat() if saying.last_modified else None
        }
    })

# 删除说法（需要登录）
@app.route('/api/sayings/<int:saying_id>', methods=['DELETE'])
@jwt_required()
def delete_saying(saying_id):
    current_user_id = get_jwt_identity()
    saying = Saying.query.filter_by(id=saying_id, user_id=current_user_id).first()
    if not saying:
        return jsonify({'success': False, 'message': 'Saying not found'}), 404

    db.session.delete(saying)
    db.session.commit()

    return jsonify({
        'success': True,
        'message': f'Saying with ID {saying_id} deleted successfully'
    })

# 搜索说法（需要登录）
@app.route('/api/sayings/search', methods=['GET'])
@jwt_required()
def search_sayings():
    current_user_id = get_jwt_identity()
    query = request.args.get('q', '').lower()
    category = request.args.get('category', '').lower()
    author = request.args.get('author', '').lower()

    # 构建查询
    sayings_query = Saying.query.filter_by(user_id=current_user_id)

    if query:
        sayings_query = sayings_query.filter(Saying.content.ilike(f'%{query}%'))

    if category:
        sayings_query = sayings_query.filter(Saying.category.ilike(f'%{category}%'))

    if author:
        sayings_query = sayings_query.filter(Saying.author.ilike(f'%{author}%'))

    sayings = sayings_query.all()

    return jsonify({
        'success': True,
        'count': len(sayings),
        'data': [{
            'id': s.id,
            'content': s.content,
            'author': s.author,
            'category': s.category,
            'created_date': s.created_date.isoformat() if s.created_date else None,
            'last_modified': s.last_modified.isoformat() if s.last_modified else None
        } for s in sayings]
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)