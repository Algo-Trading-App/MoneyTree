import numpy as np
import pandas as pd
from scipy.special import erf
from scipy.stats import norm
from Library.DataClass import *
import math


# obtain data for each asset and return the historical data and weight of each asset along with the portfolio's
# current value (investment)
def portfolio_get_data(portf: Portfolio):
    historical_data = pd.DataFrame()
    weights = []
    investment = 0

    # Empty Portfolio
    if len(portf.assets) == 0:
        return 0

    for asset in portf.assets:
        # get data and reformat
        asset_data = pd.read_csv(asset.price_history_file).drop(columns=['Open', 'Low', 'High', 'Close', 'Volume'])
        asset_data = asset_data.rename(columns={"Adj Close": asset.name})
        asset_data = asset_data.set_index('Date')

        # join new asset data to overall data table
        if historical_data.empty:
            historical_data = asset_data
        elif len(historical_data) >= len(asset_data):
            historical_data = historical_data.join(asset_data)
        else:
            historical_data = asset_data.join(historical_data)

        # store current value of each asset and calulate total investment
        current_asset_price = asset_data.iloc[-1][
                                  0] * asset.shares_owned  # if csv is ordered in ascending order of dates
        weights.append(current_asset_price)
        investment = investment + current_asset_price  # TODO case when 0 shares owned

    # calculate weights of each asset in portfolio
    weights = weights / investment
    weights = np.array(weights).round(3)

    return historical_data, weights, round(investment, 2)

# confidence level .9,.95,.99 where horizon is number of days Value at risk which to be calculated for
def value_at_risk(portf: Portfolio, conf_level: float, horizon: int):
    historical_data, weights, investment = portfolio_get_data(portf)

    # portfolio estimated parameters calculations
    returns = historical_data.pct_change()
    portf_mean = returns.mean().dot(weights)
    portf_std = np.sqrt(weights.T.dot(returns.cov()).dot(weights))

    # parametric calculations
    mean_investment = (1+portf_mean)*investment
    std_investment = investment * portf_std
    cut = norm.ppf((1-conf_level), mean_investment, std_investment)

    # Value at risk
    vr = round((investment - cut)*np.sqrt(horizon),2)
    # returns Value at Risk as a value and as a percentage of total investment
    return vr,round(vr/investment,2)*100




################ For genetic algorithm #################################

# This function returns a pandas dataframe of all the historical prices of all the
# assets in the universe. price_indicator is a string that indicates what column in
# the orginal .csv file we want to use for calculating returns. i.e 'open','close',
# 'adj. close', etc.
def gen_universe_hist_data(universe:[Asset], price_indicator:str):
    historical_data = pd.DataFrame()

    # Empty chromosomes
    if len(universe) == 0:
        print("error: empty list")
        return 0

    for asset in universe:
        # get data and reformat
        asset_data = pd.read_csv(asset.price_history_file)
        col = list(asset_data.columns)
        col.remove(price_indicator)
        col.remove('Date')
        asset_data = asset_data.drop(columns=col)
        asset_data = asset_data.rename(columns={price_indicator: asset.ticker})
        asset_data = asset_data.set_index('Date')
        asset_data = asset_data.dropna()

        # join new asset data to overall data table
        if historical_data.empty:
            historical_data = asset_data
        elif len(historical_data) <= len(asset_data):
            historical_data = historical_data.join(asset_data)
        else:
            historical_data = asset_data.join(historical_data)

    return historical_data


# returns value at risk for genetic algorithm
# weight array and universe must be 1 to 1
# confidence level .9,.95,.99
# reruns dataframe is a joint table of returns for all assets in the universe
def gen_cond_var(weights: [float], returns: pd.DataFrame, conf_level: float):

    # VaR Historical totlat returns
    #indx = round((1-conf_level)*len(data.index)) # Percentile calculation
    #return data.iloc[indx]

    ###  CVaR parametric  ###
    w = np.array(weights)
    mean = returns.mean().dot(weights)
    std = np.sqrt(w.T.dot(returns.cov()).dot(weights))
    # parametric calculations
    # there's a (1-conf_level)% probability that we loss at least
    # var percent of our total portfolio value in a day
    var = norm.ppf((1 - conf_level), mean, std)

    # equation for expected value given var of normal distribution with known mean and std
    cvar = .5*mean*erf((var-mean)/(math.sqrt(2)*std))- std*math.exp(-(var-mean)**2/(2*std**2))/math.sqrt(2*math.pi) +.5*mean
    return cvar


# This function returns the Renyi or Shannon entropy given weights and joint table of returns
# for all assets in the universe (data).
# uses square root rule for determining number of subintervals
def gen_entropy(weights: [float], returns: pd.DataFrame, type: str):
    # total returns
    returns_weighted = (returns.dropna()) * weights
    data = returns_weighted.apply(np.sum, axis=1).sort_values()

    #entropy conditions
    n = data.shape[0]
    k = int(round(np.sqrt(n))) # square root choice
    min = round(data.iloc[0],4)
    bin_width = (data.iloc[-1] - data.iloc[0])/k
    summation = 0

    if type is 'Renyi':
        for i in range(1,k):
            series = data.apply(lambda x: True if (min + bin_width * i) >= x > (min + bin_width * (i - 1)) else False)
            items_in_bin = len(series[series == True].index)
            summation = summation + bin_width*((items_in_bin/(n*bin_width))**2) # renyi formula

        entropy = math.log(summation,2) # renyi formula
        return entropy

    elif type is 'Shannon':
        for i in range(0, k+1):
            series = data.apply(lambda x: True if (min + bin_width * (i+1)) > x >= (min + bin_width * i) else False)
            items_in_bin = len(series[series == True].index)
            if (items_in_bin > 0): #can ignore cases when items_in_bin = 0
                summation = summation + items_in_bin*math.log(items_in_bin / (n * bin_width),2) # shannon formula

        entropy = (1/n)*summation # shannon formula
        return entropy

    else:
        return 'error'

# returns the probability that our portfolio daily return is 0% or greater.
# uses normal parametric approximation or historical data
def gen_pos_returns(weights: [float], returns: pd.DataFrame, type: str):
    if type is 'parametric':
        w = np.array(weights)
        mean = returns.mean().dot(weights)
        std = np.sqrt(w.T.dot(returns.cov()).dot(weights))

        # parametric calculations
        return 1 - norm.cdf(0, mean, std)

    elif type is 'historical':
        returns_weighted = (returns.dropna()) * weights
        total_returns = returns_weighted.apply(np.sum,axis=1).sort_values()
        array = np.asarray(total_returns)
        idx = (np.abs(array)).argmin() # index of closest return = 0%
        prob = 1 - (idx)/len(array)  # 1 - probability return is less than 0%
        return prob

    else:
        return 'error'


# returns fitness score for a particular individual given its
# weights(chromosome) and universe for which does weights are 1-1 with.
def gen_fitness_value(weights: [float], universe_data: pd.DataFrame):

    # portfolio returns
    returns = universe_data.pct_change()

    # CVaR
    VaR = gen_cond_var(weights,returns,.95)

    # entropy
    n = returns.nunique().sum() # number of unique returns all the universe
    p_x = 1/((returns.max().max() - returns.min().min())+1) # discrete probability of a uniform distribution for universe
    # discrete shannon entropy is maximized when distribution is uniform, i.e maximum entropy in the universe
    # discrete renyi entropy is maximized when distribution is uniform, i.e maximum entropy in the universe
    max_entropy_shannon = -n*p_x*math.log(p_x,2) # discrete shannon equation for uniform distribution
    max_entropy_renyi = math.log(n*(p_x**2),2) # discrete renyi entropy equation for uniform distribution
    entropy = gen_entropy(weights,returns,'Renyi')

    # returns
    prob_pos_returns = gen_pos_returns(weights,returns, 'historical')

    # all objectives are unitless and are percentages from 0 to 1. Therefore they have about equal weight in
    # the fitness function. theoretical max fitness score = 0 + 1 + 1 = 2
    fitness_score = VaR + entropy/max_entropy_renyi + prob_pos_returns
    return round(fitness_score,5)