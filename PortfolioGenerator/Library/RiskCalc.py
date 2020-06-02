from Library.DataClass import *
import numpy as np
import pandas as pd
import math
import ta

################ General Risk Calculations #################################

# This function returns historical value at risk
# returns: a single column dataframe holding a list of returns
def VAR(returns: pd.Series, horizon: int):
    # confidence level for calculating VaR
    # .95 is standard but .99 and .975 can also be used
    conf_level = .95

    if(returns.empty):
        print("empty data for calculating var")
        raise
    elif(horizon <= 0):
        print("horizion for VAR must be > 0 ")
        raise

    # portf_return needs to be sorted in ascending order for VaR Calc
    returns = returns.sort_values()

    # VaR Historical total returns
    tail = 1-conf_level  # the percentile of returns we are cutting off from our distribution
    indx = round((tail*len(returns.index)))  # indx is the index in our portfolio returns where we cut off the tail
    return returns.iloc[indx]*np.sqrt(horizon)  # returns value of our cutoff point aka stop loss

# This function returns the Shannon entropy
# uses square root rule for determining number of subintervals
# returns: a single column dataframe holding a list of returns
# cutoff_value: the cutoff point of our distribution used to reduce riskm  g
def Entropy(returns: pd.Series, cutoff_value: float):
    if (returns.empty):
        print("empty data for calculating Entropy")
        raise

    # sort returns in ascending order to ease calculations
    returns = returns.sort_values()

    # need to cut off tail of our returns distribution based on our cutoff value before calculating entropy
    cutoff_indx = 0
    for x in range(0,returns.shape[0]):
        if returns[x] <= cutoff_value:
            cutoff_indx = cutoff_indx+1
        else:
            break
    returns = returns.iloc[cutoff_indx:]

    # entropy conditions
    n = returns.shape[0] # number of returns in distribution
    num_bin = int(round(np.sqrt(n))) # square root choice for how many bins to split all returns into
    bin_width = (returns.iloc[-1] - returns.iloc[0]) / num_bin
    min = round(returns.iloc[0],4)
    summation = 0

    # entropy calculation
    for i in range(0, num_bin+1):
        # mark all returns in current bin
        series = returns.apply(lambda x: True if (min + bin_width * (i+1)) > x >= (min + bin_width * i) else False)
        # count all returns marked
        items_in_bin = len(series[series == True].index)

        if (items_in_bin > 0): #to ignore cases when items_in_bin = 0
            summation = summation + items_in_bin*math.log(items_in_bin / (n * bin_width),2) # shannon formula

    entropy = (1/n)*summation # shannon formula
    return entropy

# This function returns historical probability that our portfolio daily returns are positive.
def PosReturns(returns: pd.Series):
    if (returns.empty):
        print("empty data for calculating PosReturns")
        raise

    returns = returns.sort_values()
    array = np.asarray(returns)
    idx = (np.abs(array)).argmin() # index of closest return = 0%
    prob = 1 - (idx)/len(array)  # 1 - probability return is less than 0% = probability returns are greater than 0%
    return prob

################ Portfolio Risk Calculations #################################

# obtain data for each asset and return the historical data and weight of each asset along with the portfolio's
# current value (investment)
def portfolio_get_data(portf: Portfolio, start_date: str, end_date: str, price_indicator: str):
    historical_data = pd.DataFrame()
    weights = []

    # Empty Portfolio
    if len(portf.assets) == 0:
        return 0

    for asset in portf.assets:
        # get data from csv and reformat dataframe
        asset_data = pd.read_csv(asset.PriceHistoryFile)
        col = list(asset_data.columns)
        col.remove(price_indicator)
        col.remove('Date')
        asset_data = asset_data.drop(columns=col)
        asset_data = asset_data.rename(columns={price_indicator: asset.Ticker})
        asset_data = asset_data.set_index('Date')
        asset_data = asset_data.dropna()
        asset_data = asset_data[start_date:end_date]

        # join new asset data to overall data table
        if historical_data.empty and not asset_data.empty:
            historical_data = asset_data
        else:
            historical_data = asset_data.join(historical_data)

        # store current value of each asset and calulate total investment
        weights.append(portf.AssetAlloc[asset.Ticker])

    return historical_data, weights

# confidence level .9,.95,.99 where horizon is number of days Value at risk which to be calculated for
def portfolio_var(portf: Portfolio,start_date: str, end_date: str, horizon: int):
    historical_data, weights = portfolio_get_data(portf,start_date,end_date,"Adj_Close")
    returns = historical_data.pct_change()
    returns = returns.dropna(how='all')  # drop all dates that have no data of any asset
    returns = returns * weights
    total_returns = returns.apply(np.sum, axis=1)
    return VAR(total_returns,horizon)

def bollinger_series_data(universe:[Asset], asset_shares: dict, price_indicator:str, start_date: str, end_date: str):
    historical_data = pd.Series()

    for asset in universe:
        # get data from csv and reformat dataframe
        asset_data = pd.read_csv(asset.PriceHistoryFile, index_col=0)
        asset_data = asset_data[start_date:end_date]
        col = list(asset_data.columns)
        col.remove(price_indicator)
        asset_data = asset_data.drop(columns=col)
        asset_data = asset_data.rename(columns={price_indicator: asset.Ticker})
        asset_data = asset_data.multiply(math.trunc(asset_shares[asset.Ticker]))
        # join new asset data to overall data table
        if historical_data.empty:
            historical_data = asset_data
        else:
            historical_data = historical_data.join(asset_data)

    return historical_data.sum(axis =1)

################ Genetic Algorithm Functions#################################

# This function returns a pandas dataframe of all the historical prices of all the
# assets in the universe joined in one table
# universe: list of assets in universe
# price_indicator: indicates what column in the orginal .csv file to keep for calculating returns.
# i.e 'open','close', or 'adj. close', etc.
def gen_universe_hist_data(universe:[Asset], price_indicator: str):
    historical_data = pd.DataFrame()

    for asset in universe:
        col = list(asset.AssetData.columns)
        col.remove(price_indicator)
        asset_data = asset.AssetData.drop(columns=col)
        asset_data = asset_data.rename(columns={price_indicator: asset.Ticker})
        # join new asset data to overall data table
        if historical_data.empty:
            historical_data = asset_data
        else:
            historical_data = historical_data.join(asset_data)
    return historical_data

# This function returns a pandas dataframe of all the historical prices of for assets
# in the chromosome joined in one table
# universe: list of assets in universe
def gen_chromosome_hist_data(chromosome: [int], universe_assets: [str], universe_data: pd.DataFrame):
    # reduces universe dataframe to a portfolio of assets indicated by the chromosome
    portf_asset_indicies = np.where(np.array(chromosome) == 1)
    assets_in_portf = np.array(universe_assets)[portf_asset_indicies]
    chromosome_hist_data = universe_data[assets_in_portf]
    return chromosome_hist_data

# this function returns a VAR fitness score for a particular individual given a chromosome
# and universe that chromosome is from. We seek to minimize this.
def gen_VAR(chromosome: [int], universe_assets: [str], universe_data: pd.DataFrame):
    horizion = 21  # number of trading days per month

    # the total portfolio returns is the average of asset returns, in an equally weighted portfolio
    chrom_data = gen_chromosome_hist_data(chromosome,universe_assets, universe_data)
    chrom_returns = chrom_data.pct_change().dropna(how='all')  # drop all dates that have no data of any asset
    chrom_total_returns = chrom_returns.apply(np.sum, axis=1) / chrom_returns.count(axis='columns')  # average = sum/#assets
    return VAR(chrom_total_returns, horizion)

# this function returns a Entropy fitness score for a particular individual given a chromosome
# and universe that chromosome is from. We seek to maximize this.
def gen_Entropy(chromosome: [int], universe_assets: [str], universe_data: pd.DataFrame):
    horizion = 21  # number of trading days per month
    # the total portfolio returns is the average of asset returns, in an equally weighted portfolio
    chrom_data = gen_chromosome_hist_data(chromosome,universe_assets, universe_data)
    chrom_returns = chrom_data.pct_change().dropna(how='all')  # drop all dates that have no data of any asset
    chrom_total_returns = chrom_returns.apply(np.sum, axis=1) / chrom_returns.count(axis='columns')  # average = sum/#assets
    if chrom_total_returns.empty:
        return 0
    cutoff = VAR(chrom_total_returns, horizion)
    return Entropy(chrom_total_returns,cutoff)

# this function returns a PosReturns fitness score for a particular individual given a chromosome
# and universe that chromosome is from. We seek to maximize this.
def gen_PosReturns(chromosome: [int], universe_assets: [str], universe_data: pd.DataFrame):
    # the total portfolio returns is the average of asset returns, in an equally weighted portfolio
    chrom_data = gen_chromosome_hist_data(chromosome,universe_assets, universe_data)
    chrom_returns = chrom_data.pct_change().dropna(how='all')  # drop all dates that have no data of any asset
    chrom_total_returns = chrom_returns.apply(np.sum, axis=1) / chrom_returns.count(axis='columns')  # average = sum/#assets
    return PosReturns(chrom_total_returns)


################ Filtering and Bollinger Calculations ####################

# Function to calculate historical volatility for an asset
def calc_volatility(df: pd.DataFrame):

    # Set the number of days to calculate volatility over to be one less than lenght.
    num_days = len(df) - 1
    
    # Create two lists of prices representing current and previous day for each day.
    prices = df['Adj_Close'][-num_days:].values
    prev_prices = df['Adj_Close'][-(num_days+1):-1].values

    # Calculate logarithmic returns for each day
    log_ret = np.log(prices/prev_prices)
    # Calculate standard deviation.
    std = np.std(log_ret)
    # Calculate volatility
    volatility = std * np.sqrt(num_days)

    return volatility

# Function to calculate adx values for an asset
def calc_adx(df: pd.DataFrame, num_days: int = 14):

    if len(df) <= num_days:
        return None
    else:
        num_days = len(df)//2

    adx = ta.trend.ADXIndicator(high=df['Adj_High'],low=df['Adj_Low'],close=df['Adj_Close'],n=num_days)

    return (adx.adx().values[-1],adx.adx_neg().values[-1],adx.adx_pos().values[-1])

# Function to calculate aroon values for an asset
def calc_aroon(df: pd.DataFrame, num_days: int = 25):

    if len(df) <= num_days:
        return None
    else:
        num_days = len(df)

    aroon = ta.trend.AroonIndicator(close=df['Adj_Close'],n=num_days)

    return (aroon.aroon_down().values[-1],aroon.aroon_up().values[-1])

# Function to return bollinger band vales
def calc_bollinger_bands(df: pd.DataFrame, num_days: int=10, ndev: int=2):

    if len(df) < num_days:
        return None

    bollinger_bands = ta.volatility.BollingerBands(close=df, n=num_days, ndev=ndev)

    return (bollinger_bands.bollinger_hband().values[-1], bollinger_bands.bollinger_lband().values[-1])

# Function to indicate upper band crossover
def calc_bollinger_upper_indicator(df: pd.DataFrame, num_days: int=10, ndev: int=1):

    if len(df) < num_days:
        return None

    bollinger_bands = ta.volatility.BollingerBands(close=df, n=num_days, ndev=ndev)

    return bollinger_bands.bollinger_hband_indicator().values[-1]

# Function to indicate lower band indicator
def calc_bollinger_lower_indicator(df: pd.DataFrame, num_days: int=10, ndev: int=1):

    if len(df) < num_days:
        return None

    bollinger_bands = ta.volatility.BollingerBands(close=df, n=num_days, ndev=ndev)

    return bollinger_bands.bollinger_lband_indicator().values[-1]

# Funtion for calculating total value of a portfolio based of alloc %
def calc_total_value(universe: Universe, weights: [float]):
    total = 0
    for i in range(universe.count):
        total += universe.UniverseSet[i].LastPrice * weights[i]
    return total
