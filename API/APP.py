from flask import Flask, jsonify, request, abort
from flask_cors import CORS
from datetime import datetime
import json
import os

app = Flask(__name__)
CORS(app)  # 允许跨域请求

# 简单的内存数据存储（生产环境应使用数据库）
sayings_data = []
next_id = 1
DATA_FILE = "sayings_data.json"

def load_data():
    """从文件加载数据"""
    global sayings_data, next_id
    try:
        if os.path.exists(DATA_FILE):
            with open(DATA_FILE, 'r', encoding='utf-8') as f:
                data = json.load(f)
                sayings_data = data.get('sayings', [])
                next_id = data.get('next_id', 1) if sayings_data else 1
    except Exception as e:
        print(f"Error loading data: {e}")
        sayings_data = []
        next_id = 1

def save_data():
    """保存数据到文件"""
    try:
        data = {
            'sayings': sayings_data,
            'next_id': next_id
        }
        with open(DATA_FILE, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
    except Exception as e:
        print(f"Error saving data: {e}")

# 初始化时加载数据
load_data()

class Saying:
    def __init__(self, content, author="Unknown", category="General"):
        global next_id
        self.id = next_id
        self.content = content
        self.author = author
        self.category = category
        self.created_date = datetime.now().isoformat()
        self.last_modified = self.created_date
        next_id += 1
    
    def to_dict(self):
        return {
            'id': self.id,
            'content': self.content,
            'author': self.author,
            'category': self.category,
            'created_date': self.created_date,
            'last_modified': self.last_modified
        }

@app.route('/')
def index():
    return jsonify({
        'message': 'Unknown Saying API',
        'version': '1.0.0',
        'endpoints': {
            'GET /api/sayings': 'Get all sayings',
            'GET /api/sayings/<id>': 'Get specific saying',
            'POST /api/sayings': 'Create new saying',
            'PUT /api/sayings/<id>': 'Update saying',
            'DELETE /api/sayings/<id>': 'Delete saying'
        }
    })

@app.route('/api/sayings', methods=['GET'])
def get_all_sayings():
    """获取所有说法"""
    return jsonify({
        'success': True,
        'count': len(sayings_data),
        'data': [s.to_dict() if hasattr(s, 'to_dict') else s for s in sayings_data]
    })

@app.route('/api/sayings/<int:saying_id>', methods=['GET'])
def get_saying(saying_id):
    """根据ID获取说法"""
    saying = next((s for s in sayings_data if (s.id if hasattr(s, 'id') else s['id']) == saying_id), None)
    
    if not saying:
        abort(404, description=f"Saying with ID {saying_id} not found")
    
    if hasattr(saying, 'to_dict'):
        return jsonify({'success': True, 'data': saying.to_dict()})
    else:
        return jsonify({'success': True, 'data': saying})

@app.route('/api/sayings', methods=['POST'])
def create_saying():
    """创建新说法"""
    data = request.get_json()
    
    if not data or 'content' not in data:
        abort(400, description="Content is required")
    
    content = data.get('content', '').strip()
    if not content:
        abort(400, description="Content cannot be empty")
    
    author = data.get('author', 'Unknown').strip() or 'Unknown'
    category = data.get('category', 'General').strip() or 'General'
    
    new_saying = Saying(content, author, category)
    sayings_data.append(new_saying)
    save_data()  # 保存到文件
    
    return jsonify({
        'success': True,
        'message': 'Saying created successfully',
        'data': new_saying.to_dict()
    }), 201

@app.route('/api/sayings/<int:saying_id>', methods=['PUT'])
def update_saying(saying_id):
    """更新说法"""
    saying = next((s for s in sayings_data if (s.id if hasattr(s, 'id') else s['id']) == saying_id), None)
    
    if not saying:
        abort(404, description=f"Saying with ID {saying_id} not found")
    
    data = request.get_json()
    
    if not data:
        abort(400, description="No data provided")
    
    if 'content' in data:
        content = data['content'].strip()
        if not content:
            abort(400, description="Content cannot be empty")
        saying.content = content
    
    if 'author' in data:
        saying.author = data['author'].strip() or 'Unknown'
    
    if 'category' in data:
        saying.category = data['category'].strip() or 'General'
    
    saying.last_modified = datetime.now().isoformat()
    save_data()  # 保存到文件
    
    if hasattr(saying, 'to_dict'):
        return jsonify({
            'success': True,
            'message': 'Saying updated successfully',
            'data': saying.to_dict()
        })
    else:
        return jsonify({
            'success': True,
            'message': 'Saying updated successfully',
            'data': saying
        })

@app.route('/api/sayings/<int:saying_id>', methods=['DELETE'])
def delete_saying(saying_id):
    """删除说法"""
    global sayings_data
    initial_count = len(sayings_data)
    
    sayings_data = [s for s in sayings_data if (s.id if hasattr(s, 'id') else s['id']) != saying_id]
    
    if len(sayings_data) == initial_count:
        abort(404, description=f"Saying with ID {saying_id} not found")
    
    save_data()  # 保存到文件
    
    return jsonify({
        'success': True,
        'message': f'Saying with ID {saying_id} deleted successfully'
    })

@app.route('/api/sayings/search', methods=['GET'])
def search_sayings():
    """搜索说法"""
    query = request.args.get('q', '').lower()
    category = request.args.get('category', '').lower()
    author = request.args.get('author', '').lower()
    
    filtered_sayings = sayings_data
    
    if query:
        filtered_sayings = [s for s in filtered_sayings if query in s.content.lower()]
    
    if category:
        filtered_sayings = [s for s in filtered_sayings if category == s.category.lower()]
    
    if author:
        filtered_sayings = [s for s in filtered_sayings if author in s.author.lower()]
    
    result_data = []
    for s in filtered_sayings:
        if hasattr(s, 'to_dict'):
            result_data.append(s.to_dict())
        else:
            result_data.append(s)
    
    return jsonify({
        'success': True,
        'count': len(result_data),
        'data': result_data
    })

@app.errorhandler(400)
def bad_request_error(error):
    return jsonify({
        'success': False,
        'error': 'Bad Request',
        'message': str(error.description)
    }), 400

@app.errorhandler(404)
def not_found_error(error):
    return jsonify({
        'success': False,
        'error': 'Not Found',
        'message': str(error.description)
    }), 404

@app.errorhandler(500)
def internal_error(error):
    return jsonify({
        'success': False,
        'error': 'Internal Server Error',
        'message': 'An internal error occurred'
    }), 500

if __name__ == '__main__':
    # 初始化一些示例数据
    if not sayings_data:
        sayings_data = [
            Saying("The journey of a thousand miles begins with one step", "Lao Tzu", "Philosophy"),
            Saying("To be, or not to be, that is the question", "William Shakespeare", "Literature"),
            Saying("I think, therefore I am", "René Descartes", "Philosophy"),
            Saying("Knowledge is power", "Francis Bacon", "Education")
        ]
        save_data()
    
    print("Starting Unknown Saying API Server...")
    print("API URL: http://localhost:5000")
    print("API Documentation: http://localhost:5000/")
    print("Available endpoints:")
    print("  GET  /api/sayings           - Get all sayings")
    print("  GET  /api/sayings/<id>      - Get specific saying")
    print("  POST /api/sayings           - Create new saying")
    print("  PUT  /api/sayings/<id>      - Update saying")
    print("  DELETE /api/sayings/<id>    - Delete saying")
    print("  GET  /api/sayings/search    - Search sayings")
    
    app.run(host='0.0.0.0', port=5000, debug=True)