#import streamlit as st
#import pandas as pd
#import numpy as np
#st.title("简单数据可视化 / Simple Data Visualization")
# 假数据 / Fake data
#data = pd.DataFrame(
#    np.random.randn(20, 3),
#    columns=["A", "B", "C"]
#)

from datetime import datetime, timedelta
import os
import pandas as pd
from FinMind.data import DataLoader

api = DataLoader()
token = os.environ.get("FINMIND_TOKEN")
if not token:
    raise RuntimeError("Please set FINMIND_TOKEN environment variable before running.")
api.login_by_token(api_token=token)

start_date = datetime(2023, 9, 22)
end_date = datetime(2023, 9, 30)

with open("stock_id.txt", "r", encoding="utf-8") as f:
    stock_ids = [line.strip() for line in f if line.strip()]

all_data = {stock_id: [] for stock_id in stock_ids}

current_date = start_date
while current_date <= end_date:
    date_str = current_date.strftime("%Y-%m-%d")
    for stock_id in stock_ids:
        df = api.taiwan_stock_kbar(
            stock_id=stock_id,
            date=date_str
        )
        print(f"{stock_id} {date_str} head:")
        print(df.head())
        if not df.empty:
            all_data[stock_id].append(df)
    current_date += timedelta(days=1)

for stock_id, df_list in all_data.items():
    if not df_list:
        continue
    full_df = pd.concat(df_list, ignore_index=True)
    output_name = f"output_{stock_id}_{start_date.strftime('%Y-%m-%d')}_{end_date.strftime('%Y-%m-%d')}.csv"
    full_df.to_csv(output_name, index=False, encoding="utf-8-sig")
