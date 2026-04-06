# 🚀 محرك التنبؤ المعتمد على Prophet للأنظمة الدوائية
import sys
import pandas as pd
import numpy as np
from prophet import Prophet
import json

def forecast_demand(json_data):
    try:
        data = json.loads(json_data)
        if len(data) < 2:
            return json.dumps({"forecast": 0, "accuracy": 0})

        df = pd.DataFrame(data)
        df['ds'] = pd.to_datetime(df['date'])
        df['y'] = df['quantity']
        
        # بناء الموديل
        model = Prophet(yearly_seasonality=True, weekly_seasonality=True, daily_seasonality=False)
        model.fit(df[['ds', 'y']])
        
        # التنبؤ للـ 30 يوماً القادمة
        future = model.make_future_dataframe(periods=30)
        forecast = model.predict(future)
        
        # استخراج الطلب المتوقع للشهر القادم
        next_month_demand = forecast.tail(30)['yhat'].sum()
        # تجنب القيم السالبة
        if next_month_demand < 0:
            next_month_demand = 0

        # حساب دقة التنبؤ التقريبية (In-sample MAPE)
        train_forecast = forecast.head(len(df))
        merged = pd.merge(df, train_forecast[['ds', 'yhat']], on='ds', how='inner')
        # حساب MAPE مع تجنب القسمة على صفر
        y_true = np.maximum(merged['y'], 1e-5) 
        mape = np.mean(np.abs((y_true - merged['yhat']) / y_true)) * 100
        
        # الدقة = 100 - نسبة الخطأ (بحد أدنى 0 و أقصى 99)
        accuracy = max(0, min(99.9, 100 - mape))

        result = {
            "forecast": round(next_month_demand, 2),
            "accuracy": round(accuracy, 2)
        }
        return json.dumps(result)
    except Exception as e:
        return json.dumps({"forecast": 0, "accuracy": 0, "error": str(e)})

if __name__ == "__main__":
    input_data = sys.stdin.read()
    if not input_data or input_data.strip() == "":
        print(json.dumps({"forecast": 0, "accuracy": 0}))
    else:
        result = forecast_demand(input_data)
        print(result)