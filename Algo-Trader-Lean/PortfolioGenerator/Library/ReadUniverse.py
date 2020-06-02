import pandas as pd
import quandl
import os
from Library.DataClass import *

def ReadUniverse():
    path = os.path.abspath('./Library/universe.csv')
    universe_set = pd.read_csv(path)

    API_KEY = 'AfNGX6bsX6tWB-NayPFh'
    start = '2010-01-01'
    end = '2020-01-01'

    asset_list = []
    count = 0

    for i in range(len(universe_set)):
        ticker = universe_set['Symbol'][i]
        name = universe_set['Name'][i]
        path = os.path.abspath('./Stock_Data')+'/'+ticker+'.csv'
        if os.path.exists(path):
            df = pd.read_csv(path)
            asset = Asset(ticker,name,'stock',path,df['Adj. Close'].values[-1])
            asset_list.append(asset)
            count += 1
            continue
        try:
            df = quandl.get('WIKI/'+ticker,start_date=start,end_date=end,api_key=API_KEY)
            df.to_csv(path)
            asset = Asset(ticker,name,'stock',path,df['Adj. Close'].values[-1])
            asset_list.append(asset)
            count += 1
        except quandl.errors.quandl_error.NotFoundError:
            print("unknown ticker: {}".format(ticker))
        except:
            print('unknown error')

    return Universe(count,asset_list)