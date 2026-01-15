from datetime import datetime, timedelta
from sqlalchemy import func, and_
from models import db, UsageStatistics, Saying, User
import pandas as pd
import matplotlib.pyplot as plt
import io
import base64

class AnalyticsManager:
    @staticmethod
    def track_api_call(user_id, endpoint):
        """Track API call for analytics"""
        today = datetime.utcnow().date()
        
        stats = UsageStatistics.query.filter_by(
            user_id=user_id,
            date=today
        ).first()
        
        if not stats:
            stats = UsageStatistics(user_id=user_id, date=today)
            db.session.add(stats)
        
        stats.api_calls += 1
        db.session.commit()
    
    @staticmethod
    def track_saying_creation(user_id):
        """Track saying creation"""
        today = datetime.utcnow().date()
        
        stats = UsageStatistics.query.filter_by(
            user_id=user_id,
            date=today
        ).first()
        
        if not stats:
            stats = UsageStatistics(user_id=user_id, date=today)
            db.session.add(stats)
        
        stats.sayings_created += 1
        db.session.commit()
    
    @staticmethod
    def get_user_stats(user_id, start_date=None, end_date=None):
        """Get user statistics"""
        if not start_date:
            start_date = datetime.utcnow() - timedelta(days=30)
        if not end_date:
            end_date = datetime.utcnow()
        
        query = UsageStatistics.query.filter_by(user_id=user_id)
        query = query.filter(UsageStatistics.date.between(start_date.date(), end_date.date()))
        
        stats = query.order_by(UsageStatistics.date).all()
        
        # Prepare data for charts
        dates = [stat.date.isoformat() for stat in stats]
        sayings_created = [stat.sayings_created for stat in stats]
        api_calls = [stat.api_calls for stat in stats]
        
        # Calculate totals
        total_sayings = sum(sayings_created)
        total_api_calls = sum(api_calls)
        avg_sayings_per_day = total_sayings / len(stats) if stats else 0
        
        return {
            'period': {
                'start': start_date.isoformat(),
                'end': end_date.isoformat()
            },
            'totals': {
                'sayings_created': total_sayings,
                'api_calls': total_api_calls,
                'avg_sayings_per_day': round(avg_sayings_per_day, 2)
            },
            'daily_data': {
                'dates': dates,
                'sayings_created': sayings_created,
                'api_calls': api_calls
            }
        }
    
    @staticmethod
    def generate_chart(data, chart_type='line'):
        """Generate chart image"""
        plt.figure(figsize=(10, 6))
        
        if chart_type == 'line':
            plt.plot(data['dates'], data['sayings_created'], 
                    marker='o', label='Sayings Created')
            plt.plot(data['dates'], data['api_calls'], 
                    marker='s', label='API Calls', linestyle='--')
        elif chart_type == 'bar':
            x = range(len(data['dates']))
            width = 0.35
            plt.bar([i - width/2 for i in x], data['sayings_created'], 
                   width, label='Sayings Created')
            plt.bar([i + width/2 for i in x], data['api_calls'], 
                   width, label='API Calls')
        
        plt.xlabel('Date')
        plt.ylabel('Count')
        plt.title('User Activity Report')
        plt.legend()
        plt.xticks(rotation=45)
        plt.tight_layout()
        
        # Convert plot to base64 image
        buffer = io.BytesIO()
        plt.savefig(buffer, format='png', dpi=100)
        buffer.seek(0)
        image_base64 = base64.b64encode(buffer.getvalue()).decode('utf-8')
        plt.close()
        
        return image_base64
    
    @staticmethod
    def get_category_distribution(user_id):
        """Get saying distribution by category"""
        distribution = db.session.query(
            Saying.category,
            func.count(Saying.id).label('count')
        ).filter_by(
            user_id=user_id
        ).group_by(
            Saying.category
        ).order_by(
            func.count(Saying.id).desc()
        ).all()
        
        return {
            'categories': [cat for cat, _ in distribution],
            'counts': [count for _, count in distribution]
        }
    
    @staticmethod
    def generate_report(user_id, report_type='weekly'):
        """Generate comprehensive report"""
        if report_type == 'weekly':
            end_date = datetime.utcnow()
            start_date = end_date - timedelta(days=7)
        elif report_type == 'monthly':
            end_date = datetime.utcnow()
            start_date = end_date - timedelta(days=30)
        else:  # yearly
            end_date = datetime.utcnow()
            start_date = end_date - timedelta(days=365)
        
        # Get user stats
        user_stats = AnalyticsManager.get_user_stats(user_id, start_date, end_date)
        
        # Get category distribution
        category_stats = AnalyticsManager.get_category_distribution(user_id)
        
        # Generate chart
        chart_image = AnalyticsManager.generate_chart(user_stats['daily_data'])
        
        # Calculate engagement score
        engagement_score = AnalyticsManager._calculate_engagement_score(user_stats)
        
        report = {
            'report_type': report_type,
            'period': user_stats['period'],
            'summary': {
                'engagement_score': engagement_score,
                'productivity_level': AnalyticsManager._get_productivity_level(engagement_score),
                'recommendations': AnalyticsManager._generate_recommendations(user_stats)
            },
            'statistics': user_stats['totals'],
            'category_distribution': category_stats,
            'chart_image': chart_image,
            'generated_at': datetime.utcnow().isoformat()
        }
        
        return report