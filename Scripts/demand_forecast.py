# 🚀 محرك التنبؤ المعتمد على Prophet للأنظمة الدوائية
import sys
import pandas as pd
from prophet import Prophet
import json

def forecast_demand(json_data):
    try:
        # 1. تجهيز البيانات (تحويل JSON إلى DataFrame)
        data = json.loads(json_data)
        df = pd.DataFrame(data)
        df['ds'] = pd.to_datetime(df['date'])
        df['y'] = df['quantity']
        
        # 2. بناء الموديل (إضافة الموسمية الأسبوعية والسنوية)
        model = Prophet(yearly_seasonality=True, weekly_seasonality=True, daily_seasonality=False)
        model.fit(df[['ds', 'y']])
        
        # 3. التنبؤ للـ 30 يوماً القادمة
        future = model.make_future_dataframe(periods=30)
        forecast = model.predict(future)
        
        # 4. استخراج متوسط الطلب اليومي المتوقع للشهر القادم
        next_month_demand = forecast.tail(30)['yhat'].sum()
        
        return round(next_month_demand, 2)
    except Exception as e:
        return 0

if __name__ == "__main__":
    # استلام البيانات من C# عبر Standard Input
    input_data = sys.stdin.read()
    result = forecast_demand(input_data)
    print(result)