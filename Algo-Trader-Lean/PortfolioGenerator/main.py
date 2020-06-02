#imports
from Library.RiskCalc import *
from Library.ReadUniverse import *
from Library.FilterUniverse import *
from Library.GeneratePortfolio import generate_portfolio
from Library.MonteCarloSim import *
import os

#####  sample universe for testing  ######
AKM = Asset(ticker='AKAM', name='Akamai Technologies Inc', asset_type='stock', price_history_file='C:\\Users\\Francisco\\Documents\\AlgoTradingCode\\portfolio_gen\\Stock_Data/AKAM.csv',last_price=0)
CSRA = Asset(ticker='CSRA', name='CSRA Inc.', asset_type='stock', price_history_file='C:\\Users\\Francisco\\Documents\\AlgoTradingCode\\portfolio_gen\\Stock_Data/CSRA.csv',last_price=0)
MCHP = Asset(ticker='MCHP', name='Microchip Technology', asset_type='stock', price_history_file='C:\\Users\\Francisco\\Documents\\AlgoTradingCode\\portfolio_gen\\Stock_Data/MCHP.csv',last_price=0)
NKTR = Asset(ticker='NKTR', name='Nektar Therapeutics', asset_type='stock', price_history_file='C:\\Users\\Francisco\\Documents\\AlgoTradingCode\\portfolio_gen\\Stock_Data/NKTR.csv',last_price=0)
data = gen_universe_hist_data([AKM,CSRA,MCHP,NKTR],"Adj. Close")

#####  for testing genetic algorithm fitness function  #####
#n= np.random.dirichlet(np.ones(4),size=20)
#for i in n:
#   print(gen_fitness_value(i, data))

####   for testing monte carlo simulations  #####
#period = 10  # in days
#n_inc = 10    # granularity higher number is more smooth
#n_sims = 5   # number of simulations
#df = monte_carlo(period,n_inc,n_sims,'GBM', data, ['NKTR', 'CSRA'])
#print(df)

####  for testing genetic algorithm  ####
#universe = ReadUniverse()
#print(universe.count)
#filtered_universe = filter_universe(universe)
#print(filtered_universe.universe_set)
#portf = generate_portfolio(filtered_universe, 10000, 1)
#print(portf)