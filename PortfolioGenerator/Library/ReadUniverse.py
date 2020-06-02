import pandas as pd
import time
import quandl
import os
from Library.DataClass import *

# Set up Quandl variables
API_KEY = 'b5wYcieS5ZYxGvJNN7yW'

# Function to get data from quandl
def get_quandl_data(ticker: str, start_date: str, end_date: str, path: str):
    try:
        df = quandl.get('EOD/'+ticker,start_date=start_date,end_date=end_date,api_key=API_KEY)

        # If data is fetched successfully, save to local cache
        if not df.empty:
            df.to_csv(path)
        return df
    except quandl.errors.quandl_error.NotFoundError:
        print("unknown ticker: {}".format(ticker))
    except:
        print('unknown error for ticker: {}'.format(ticker))

# This function will generate a universe of stocks from a list saved locally.
# If the local cache of data is missing data on a stock, it will request the data
# from quandl.
def ReadUniverse(start_date: str, end_date: str):
    path = os.path.abspath('./Library/universe.csv')
    universe_set = pd.read_csv(path)

    asset_list = []
    count = 0

    # For each asset in the universe, check if it's data exists locally.
    for i in range(len(universe_set)):
        ticker = universe_set['Symbol'][i]
        name = universe_set['Name'][i]
        path = os.path.abspath('./Stock_Data')+'/'+ticker+'.csv'

        # If data exists locally and is up to date, add it to universe
        if os.path.exists(path):
            df = pd.read_csv(path,index_col=0)
            data_start_date = time.strptime(df.index.values[0], "%Y-%m-%d")
            data_last_date = time.strptime(df.index.values[-1], "%Y-%m-%d")
            user_start_date = time.strptime(start_date, "%Y-%m-%d")
            user_end_date = time.strptime(end_date, "%Y-%m-%d")
            # If all data needed is stored locally
            if (data_start_date <= user_start_date and data_last_date >= user_end_date):
                df = df[start_date:end_date]
                if len(df) < 10:
                    continue
                asset = Asset(ticker,name,'stock',path,df['Close'].values[-1],df[['Adj_High','Adj_Low','Adj_Close']])
                asset_list.append(asset)
                count += 1
            # If local data is missing, get correct start and end and request from quandl
            else:
                if (data_start_date <= user_start_date):
                    req_start = df.index.values[0]
                else:
                    req_start = start_date
                
                if (data_last_date >= user_end_date):
                    req_end = df.index.values[-1]
                else:
                    req_end = end_date

                df = get_quandl_data(ticker,req_start,req_end,path)
                if (df.empty):
                    continue
                df = df[start_date:end_date]
                if len(df) < 10:
                    continue
                asset = Asset(ticker,name,'stock',path,df['Close'].values[-1],df[['Adj_High','Adj_Low','Adj_Close']])
                asset_list.append(asset)
                count += 1

         # If data doesn't exist locally or is not up to date, try and get data from quandl.
        else:
            df = get_quandl_data(ticker,start_date,end_date,path)
            if (df.empty or len(df) < 10):
                continue
            asset = Asset(ticker,name,'stock',path,df['Close'].values[-1],df[['Adj_High','Adj_Low','Adj_Close']])
            asset_list.append(asset)
            count += 1

    return Universe(count,asset_list,start_date,end_date)